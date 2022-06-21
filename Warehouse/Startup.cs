using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Warehouse.Configuration;
using System;
using System.Linq;

using MassTransit;
using Warehouse.Models;
using Microsoft.EntityFrameworkCore;
using Common;
using System.Threading.Tasks;

namespace Warehouse
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<WarehouseDeliveryRequestConsumer>();
                x.AddConsumer<BookQuantityCheckConsumer>();
                //TODO: remove conumser
                x.AddConsumer<ShippingConfirmedConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("warehouse-delivery-request-event", ep =>
                    {
                        ep.ConfigureConsumer<WarehouseDeliveryRequestConsumer>(context);
                    });
                    cfg.ReceiveEndpoint("warehouse-quantity-confirmation-request-event", ep =>
                    {
                        ep.ConfigureConsumer<BookQuantityCheckConsumer>(context);
                    });
                    //TODO: remove consumer
                    cfg.ReceiveEndpoint("test-test-test", ep =>
                    {
                        ep.ConfigureConsumer<ShippingConfirmedConsumer>(context);
                    });
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("WarehouseBookList"));
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
        class WarehouseDeliveryRequestConsumer : IConsumer<WarehouseDeliveryRequest>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseDeliveryRequestConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<WarehouseDeliveryRequest> context)
            {
                var bookID = context.Message.ID;
                var bookQuantity = context.Message.quantity;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(bookID));
                bool valid = true;
                if (book == null) valid = false;
                else if (book.quantity < bookQuantity) valid = false;

                if (valid)
                {
                    Console.WriteLine($"Book={bookID} can successfully deliver {bookQuantity} amount of it.");
                    book.quantity -= bookQuantity;
                    _bookContext.SaveChanges();
                    await _publishEndpoint.Publish<WarehouseDeliveryConfirmation>(new { });
                }
                else
                {
                    Console.WriteLine($"Book={bookID} is not in warehouse or cannot deliver {bookQuantity} amount of it.");
                    await _publishEndpoint.Publish<WarehouseDeliveryRejection>(new { });
                }
            }
        }
        class BookQuantityCheckConsumer : IConsumer<BookQuantityCheck>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public BookQuantityCheckConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<BookQuantityCheck> context)
            {
                var bookID = context.Message.ID;
                var bookQuantity = context.Message.quantity;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(bookID));
                bool valid = true;
                if (book == null) valid = false;
                else if (book.quantity < bookQuantity) valid = false;

                if (valid)
                {
                    Console.WriteLine($"There is {bookQuantity} amount of book={bookID}. Order can be made");
                    await _publishEndpoint.Publish<BookQuantityConfirmation>(new { });
                }
                else
                {
                    Console.WriteLine($"Missing {bookQuantity} amount of book={bookID}. Current amount={book.quantity}");
                    await _publishEndpoint.Publish<BookQuantityRejection>(new { });
                }
            }
        }
        class ShippingConfirmedConsumer : IConsumer<ShippingConfirmed>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public ShippingConfirmedConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            //TODO: remove since it is here for testing purposes
            public async Task Consume(ConsumeContext<ShippingConfirmed> context)
            {
                Console.WriteLine($"LESSS GOOOO");
            }
        }
    }
}
