using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Sales.Configuration;
using MassTransit;
using Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sales.Models;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Sales
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        class NewBookSalesInfoConsumer : IConsumer<NewBookSalesInfo>
        {
            private readonly ILogger<NewBookSalesInfoConsumer> _logger;
            private BookContext _bookContext;
            public NewBookSalesInfoConsumer(BookContext bookContext, ILogger<NewBookSalesInfoConsumer> logger)
            {
                _logger = logger;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<NewBookSalesInfo> context)
            {
                var bookID = context.Message.ID;
                var bookPrice = context.Message.price;
                Book book = new Book(bookID, bookPrice);
                _bookContext.Add(book);
                _bookContext.SaveChanges();
                _logger.LogInformation($"New book registered: {bookID}, price={bookPrice}");
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var endpointConfiguration = Configuration.GetSection("Endpoint").Get<EndpointConfiguration>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<NewBookSalesInfoConsumer>();
                x.AddConsumer<SalesConfirmationConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint(endpointConfiguration.NewBookConsumer, ep =>
                    {
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<NewBookSalesInfoConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(endpointConfiguration.ConfirmationConsumer, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<SalesConfirmationConsumer>(context);
                    });

                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("SalesBookList"));
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        class SalesConfirmationConsumer : IConsumer<SalesConfirmation>
        {
            private readonly ILogger<SalesConfirmationConsumer> _logger;
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public SalesConfirmationConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint, ILogger<SalesConfirmationConsumer> logger)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<SalesConfirmation> context)
            {
                double bookPrice = context.Message.BookPrice;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(context.Message.BookID));

                if (book == null)
                {
                    _logger.LogError($"Requested book with BookID={context.Message.BookID}, price={bookPrice} was not found for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else if (bookPrice != book.Price)
                {
                    _logger.LogError($"Wrong price provided for book with BookID={context.Message.BookID}, price={bookPrice} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    _logger.LogInformation($"Correct information for book with BookID={context.Message.BookID}, price={bookPrice} for request={context.Message.CorrelationId}.");6
                    await _publishEndpoint.Publish<SalesConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
