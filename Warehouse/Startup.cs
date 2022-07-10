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
using Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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
            var mongoDbConfiguration = Configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

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

            services.AddHealthChecks()
                .AddMongoDb(mongoDbConfiguration.Connection)
                .AddRabbitMQ(rabbitConnectionString: rabbitConfiguration.ConnStr);
            services.AddSingleton(new MongoClient(mongoDbConfiguration.Connection));
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

                endpoints.MapHealthChecks("/health");
            });
        }
        class WarehouseDeliveryRequestConsumer : IConsumer<WarehouseDeliveryStart>
        {
            private readonly ILogger<WarehouseDeliveryRequestConsumer> _logger;
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseDeliveryRequestConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<WarehouseDeliveryRequestConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<WarehouseDeliveryStart> context)
            {
                var bookID = context.Message.BookID;
                var bookQuantity = context.Message.BookQuantity;

                var collectionBooks = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Book>(_mongoConf.CollectionName.Books);

                var collectionReservations = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Reservation>(_mongoConf.CollectionName.Reservations);

                Book book = collectionBooks.Find(b => b.ID.Equals(bookID)).SingleOrDefault();
                bool valid = true;
                if (book == null)
                {
                    _logger.LogError($"No book for reservation with ID={context.Message.CorrelationId} found. BookID={bookID}, quantity={bookQuantity}.");
                    valid = false;
                }

                Reservation reservation = collectionReservations.Find(r => r.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();
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
                    collectionReservations.ReplaceOne(r => r.ID.Equals(context.Message.CorrelationId.ToString()), reservation);
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
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public WarehouseConfirmationConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<WarehouseConfirmationConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<WarehouseConfirmation> context)
            {
                var bookID = context.Message.BookID;
                var bookQuantity = context.Message.BookQuantity;
                var bookName = context.Message.BookName;

                var collectionBooks = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Book>(_mongoConf.CollectionName.Books);

                var collectionReservations = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Reservation>(_mongoConf.CollectionName.Reservations);

                Book book = collectionBooks.Find(b => b.ID.Equals(bookID)).SingleOrDefault();
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
                    collectionReservations.InsertOne(new Reservation(context.Message.CorrelationId.ToString(), book.ID, context.Message.BookQuantity, false, false));
                    book.Quantity -= bookQuantity;
                    collectionBooks.ReplaceOne(b => b.ID.Equals(bookID), book);
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
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public OrderCancelConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<OrderCancelConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<OrderCancel> context)
            {
                var collectionBooks = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Book>(_mongoConf.CollectionName.Books);

                var collectionReservations = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Reservation>(_mongoConf.CollectionName.Reservations);

                Reservation reservation = collectionReservations.Find(r => r.ID.Equals(context.Message.CorrelationId.ToString())).SingleOrDefault();

                if (reservation != null)
                {
                    _logger.LogInformation($"Cancelled reservation for {context.Message.CorrelationId}.");
                    reservation.IsCancelled = true;
                    collectionReservations.ReplaceOne(r => r.ID.Equals(context.Message.CorrelationId.ToString()), reservation);

                    Book book = collectionBooks.Find(b => b.ID.Equals(reservation.BookID)).SingleOrDefault();
                    if (book != null)
                    {
                        book.Quantity += reservation.Quantity;
                        collectionBooks.ReplaceOne(b => b.ID.Equals(reservation.BookID), book);
                    }
                }
                else
                {
                    _logger.LogError($"Cannot cancel reservation for {context.Message.CorrelationId}, because there is no such a reservation");
                }
            }
        }
    }
}
