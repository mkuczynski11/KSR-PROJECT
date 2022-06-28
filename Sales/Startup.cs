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
            private BookContext _bookContext;
            public NewBookSalesInfoConsumer(BookContext bookContext)
            {
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<NewBookSalesInfo> context)
            {
                var bookID = context.Message.ID;
                var bookPrice = context.Message.price;
                Book book = new Book(bookID, bookPrice);
                _bookContext.Add(book);
                _bookContext.SaveChanges();
                Console.WriteLine($"New book registered: {bookID}, price={bookPrice}");
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();

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

                    cfg.ReceiveEndpoint("sales-book-creation-event", ep =>
                    {
                        ep.ConfigureConsumer<NewBookSalesInfoConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("sales-book-confirmation-event", ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.ConfigureConsumer<SalesConfirmationConsumer>(context);
                    });
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("SalesBookList"));
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public SalesConfirmationConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<SalesConfirmation> context)
            {
                double bookPrice = context.Message.BookPrice;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(context.Message.BookID));

                if (book == null)
                {
                    Console.WriteLine($"Requested book with BookID={context.Message.BookID}, price={bookPrice} was not found for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else if (bookPrice != book.Price)
                {
                    Console.WriteLine($"Wrong price provided for book with BookID={context.Message.BookID}, price={bookPrice} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    Console.WriteLine($"Correct information for book with BookID={context.Message.BookID}, price={bookPrice} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
