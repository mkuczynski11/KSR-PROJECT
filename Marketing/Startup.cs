using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Marketing.Configuration;
using MassTransit;
using Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Marketing.Models;
using System.Linq;

namespace Marketing
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        class NewBookMarketingInfoConsumer : IConsumer<NewBookMarketingInfo>
        {
            private BookContext _bookContext;
            public NewBookMarketingInfoConsumer(BookContext bookContext)
            {
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<NewBookMarketingInfo> context)
            {
                var bookID = context.Message.ID;
                var bookDiscount = context.Message.discount;
                Book book = new Book(bookID, bookDiscount);
                _bookContext.Add(book);
                _bookContext.SaveChanges();
                Console.WriteLine($"New book registered: {bookID}, discount={bookDiscount}");
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<NewBookMarketingInfoConsumer>();
                x.AddConsumer<MarketingConfirmationConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("marketing-book-creation-event", ep =>
                    {
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<NewBookMarketingInfoConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("marketing-book-confirmation-event", ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<MarketingConfirmationConsumer>(context);
                    });

                    cfg.UseDelayedRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("MarketingBookList"));
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

        class MarketingConfirmationConsumer : IConsumer<MarketingConfirmation>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public MarketingConfirmationConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<MarketingConfirmation> context)
            {
                double bookDiscount = context.Message.BookDiscount;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(context.Message.BookID));
                if (book == null)
                {
                    Console.WriteLine($"Book with BookID{context.Message.BookID}, discount={bookDiscount} was not found for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<MarketingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });

                }
                else if (bookDiscount != book.Discount)
                {
                    Console.WriteLine($"Wrong discount for book with BookID{context.Message.BookID}, discount={bookDiscount} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<MarketingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    Console.WriteLine($"Book with BookID{context.Message.BookID}, discount={bookDiscount} information is valid.");
                    await _publishEndpoint.Publish<MarketingConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
