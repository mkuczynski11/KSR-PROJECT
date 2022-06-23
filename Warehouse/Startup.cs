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
                x.AddConsumer<WarehouseConfirmationConsumer>();
                //TODO: remove conumser
                x.AddConsumer<ShippingShipmentSentConsumer>();
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
                        ep.ConfigureConsumer<WarehouseConfirmationConsumer>(context);
                    });
                    //TODO: remove consumer
                    cfg.ReceiveEndpoint("test-test-test", ep =>
                    {
                        ep.ConfigureConsumer<ShippingShipmentSentConsumer>(context);
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
        class WarehouseDeliveryRequestConsumer : IConsumer<WarehouseDeliveryStart>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseDeliveryRequestConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<WarehouseDeliveryStart> context)
            {
                var bookID = context.Message.BookID;
                var bookQuantity = context.Message.BookQuantity;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(bookID));
                bool valid = true;
                if (book == null) valid = false;
                else if (book.quantity < bookQuantity) valid = false;

                if (valid)
                {
                    Console.WriteLine($"Book={bookID} can successfully deliver {bookQuantity} amount of it.");
                    book.quantity -= bookQuantity;
                    _bookContext.SaveChanges();
                    await _publishEndpoint.Publish<WarehouseDeliveryStartConfirmation>(new { });
                }
                else
                {
                    Console.WriteLine($"Book={bookID} is not in warehouse or cannot deliver {bookQuantity} amount of it.");
                    await _publishEndpoint.Publish<WarehouseDeliveryStartRejection>(new { });
                }
            }
        }
        class WarehouseConfirmationConsumer : IConsumer<WarehouseConfirmation>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseConfirmationConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<WarehouseConfirmation> context)
            {
                var bookID = context.Message.BookID;
                var bookQuantity = context.Message.BookQuantity;
                var bookName = context.Message.BookName;

                Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(bookID));
                bool valid = true;
                if (book == null) valid = false;
                else if (book.quantity < bookQuantity) valid = false;
                else if (!book.name.Equals(bookName)) valid = false;

                if (valid)
                {
                    Console.WriteLine($"There is {bookQuantity} amount of book={bookID}. Order can be made");
                    await _publishEndpoint.Publish<WarehouseConfirmationAccept>(new { });
                }
                else
                {
                    Console.WriteLine($"Missing {bookQuantity} amount of book={bookID}. Current amount={book.quantity}");
                    await _publishEndpoint.Publish<WarehouseConfirmationRefuse>(new { });
                }
            }
        }
        class ShippingShipmentSentConsumer : IConsumer<ShippingShipmentSent>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public ShippingShipmentSentConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            //TODO: remove since it is here for testing purposes
            public async Task Consume(ConsumeContext<ShippingShipmentSent> context)
            {
                Console.WriteLine($"LESSS GOOOO");
            }
        }
    }
}
