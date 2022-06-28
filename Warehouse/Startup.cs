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
                x.AddConsumer<OrderCancelConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("warehouse-delivery-request-event", ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.ConfigureConsumer<WarehouseDeliveryRequestConsumer>(context);
                    });
                    cfg.ReceiveEndpoint("warehouse-quantity-confirmation-request-event", ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.ConfigureConsumer<WarehouseConfirmationConsumer>(context);
                    });
                    cfg.ReceiveEndpoint("warehouse-order-cancel-event", ep =>
                    {
                        ep.ConfigureConsumer<OrderCancelConsumer>(context);
                    });
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("WarehouseBookAndReservationList"));
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
                if (book == null)
                {
                    Console.WriteLine($"No book for reservation with ID={context.Message.CorrelationId} found. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }

                Reservation reservation = _bookContext.ReservationItems.SingleOrDefault(r => r.ID.Equals(context.Message.CorrelationId.ToString()));
                if (reservation == null)
                {
                    Console.WriteLine($"No reservation with ID={context.Message.CorrelationId} found. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (reservation.IsRedeemed)
                {
                    Console.WriteLine($"Reservation with ID={context.Message.CorrelationId} is redeemed. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (reservation.IsCancelled)
                {
                    Console.WriteLine($"Reservation with ID={context.Message.CorrelationId} is cancelled. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }

                if (valid)
                {
                    Console.WriteLine($"Warehouse is able to reddem reservation with ID={context.Message.CorrelationId}. BookID={bookID}, quantity={bookQuantity}.");
                    reservation.IsRedeemed = true;
                    _bookContext.SaveChanges();
                    await _publishEndpoint.Publish<WarehouseDeliveryStartConfirmation>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    await _publishEndpoint.Publish<WarehouseDeliveryStartRejection>(new { CorrelationId = context.Message.CorrelationId });
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
                if (book == null)
                {
                    Console.WriteLine($"No book with provided info found.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (book.Quantity < bookQuantity)
                {
                    Console.WriteLine($"Warehouse has less amount of book then requested.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (!book.Name.Equals(bookName))
                {
                    Console.WriteLine($"Requested book has wrong name.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }

                if (valid)
                {
                    _bookContext.ReservationItems.Add(new Reservation(context.Message.CorrelationId.ToString(), book.ID, context.Message.BookQuantity, false, false));
                    book.Quantity -= bookQuantity;
                    _bookContext.SaveChanges();
                    Console.WriteLine($"There is {bookQuantity} amount of BookID={bookID}, name={bookName}. Order can be made");
                    await _publishEndpoint.Publish<WarehouseConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    await _publishEndpoint.Publish<WarehouseConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
        class OrderCancelConsumer : IConsumer<OrderCancel>
        {
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public OrderCancelConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint)
            {
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<OrderCancel> context)
            {
                Reservation reservation = _bookContext.ReservationItems.SingleOrDefault(r => r.ID.Equals(context.Message.CorrelationId.ToString()));

                if (reservation != null)
                {
                    Console.WriteLine($"Cancelled reservation for {context.Message.CorrelationId}.");
                    reservation.IsCancelled = true;
                    Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(reservation.BookID));
                    if (book != null) book.Quantity += reservation.Quantity;
                    _bookContext.SaveChanges();
                }
                else
                {
                    Console.WriteLine($"Cannot cancel reservation for {context.Message.CorrelationId}, because there is no such a reservation");
                }
            }
        }
    }
}
