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
using Microsoft.Extensions.Logging;

namespace Warehouse
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var endpointConfiguration = Configuration.GetSection("Endpoint").Get<EndpointConfiguration>();

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

                    cfg.ReceiveEndpoint(endpointConfiguration.DeliveryRequestConsumer, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<WarehouseDeliveryRequestConsumer>(context);
                    });
                    cfg.ReceiveEndpoint(endpointConfiguration.ConfirmationConsumer, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<WarehouseConfirmationConsumer>(context);
                    });
                    cfg.ReceiveEndpoint(endpointConfiguration.OrderCancelConsumer, ep =>
                    {
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<OrderCancelConsumer>(context);
                    });

                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("WarehouseBookAndReservationList"));
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
        class WarehouseDeliveryRequestConsumer : IConsumer<WarehouseDeliveryStart>
        {
            private readonly ILogger<WarehouseDeliveryRequestConsumer> _logger;
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseDeliveryRequestConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint, ILogger<WarehouseDeliveryRequestConsumer> logger)
            {
                _logger = logger;
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
                    _logger.LogError($"No book for reservation with ID={context.Message.CorrelationId} found. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }

                Reservation reservation = _bookContext.ReservationItems.SingleOrDefault(r => r.ID.Equals(context.Message.CorrelationId.ToString()));
                if (reservation == null)
                {
                    _logger.LogError($"No reservation with ID={context.Message.CorrelationId} found. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (reservation.IsRedeemed)
                {
                    _logger.LogError($"Reservation with ID={context.Message.CorrelationId} is redeemed. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (reservation.IsCancelled)
                {
                    _logger.LogError($"Reservation with ID={context.Message.CorrelationId} is cancelled. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }

                if (valid)
                {
                    _logger.LogInformation($"Warehouse is able to reddem reservation with ID={context.Message.CorrelationId}. BookID={bookID}, quantity={bookQuantity}.");
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
            private readonly ILogger<WarehouseConfirmationConsumer> _logger;
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseConfirmationConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint, ILogger<WarehouseConfirmationConsumer> logger)
            {
                _logger = logger;
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
                    _logger.LogError($"No book with provided info found.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (book.Quantity < bookQuantity)
                {
                    _logger.LogError($"Warehouse has less amount of book then requested.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }
                else if (!book.Name.Equals(bookName))
                {
                    _logger.LogError($"Requested book has wrong name.BookID={bookID}, name={bookName}, quantity={bookQuantity}.");
                    valid = false;
                }

                if (valid)
                {
                    _bookContext.ReservationItems.Add(new Reservation(context.Message.CorrelationId.ToString(), book.ID, context.Message.BookQuantity, false, false));
                    book.Quantity -= bookQuantity;
                    _bookContext.SaveChanges();
                    _logger.LogInformation($"There is {bookQuantity} amount of BookID={bookID}, name={bookName}. Order can be made");
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
            private readonly ILogger<OrderCancelConsumer> _logger;
            private BookContext _bookContext;
            public readonly IPublishEndpoint _publishEndpoint;
            public OrderCancelConsumer(BookContext bookContext, IPublishEndpoint publishEndpoint, ILogger<OrderCancelConsumer> logger)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _bookContext = bookContext;
            }
            public async Task Consume(ConsumeContext<OrderCancel> context)
            {
                Reservation reservation = _bookContext.ReservationItems.SingleOrDefault(r => r.ID.Equals(context.Message.CorrelationId.ToString()));

                if (reservation != null)
                {
                    _logger.LogInformation($"Cancelled reservation for {context.Message.CorrelationId}.");
                    reservation.IsCancelled = true;
                    Book book = _bookContext.BookItems.SingleOrDefault(b => b.ID.Equals(reservation.BookID));
                    if (book != null) book.Quantity += reservation.Quantity;
                    _bookContext.SaveChanges();
                }
                else
                {
                    _logger.LogError($"Cannot cancel reservation for {context.Message.CorrelationId}, because there is no such a reservation");
                }
            }
        }
    }
}
