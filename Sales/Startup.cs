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
using Sales.Models;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using HealthChecks.UI.Client;

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
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public NewBookSalesInfoConsumer(MongoClient mongoClient, ILogger<NewBookSalesInfoConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<NewBookSalesInfo> context)
            {
                var bookID = context.Message.ID;
                var bookPrice = context.Message.price;
                Book book = new Book(bookID, bookPrice);

                var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);
                collection.InsertOne(book);

                _logger.LogInformation($"New book registered: {bookID}, price={bookPrice}");
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var endpointConfiguration = Configuration.GetSection("Endpoint").Get<EndpointConfiguration>();
            var mongoDbConfiguration = Configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

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

            var mongoSettings = MongoClientSettings.FromConnectionString(mongoDbConfiguration.Connection);
            mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(3);
            mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

            services.AddHealthChecks()
                .AddMongoDb(mongodbConnectionString: mongoDbConfiguration.Connection, name: "mongoDB", failureStatus: HealthStatus.Unhealthy)
                .AddRabbitMQ(rabbitConnectionString: rabbitConfiguration.ConnStr);
            services.AddSingleton(new MongoClient(mongoSettings));
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

                endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }

        class SalesConfirmationConsumer : IConsumer<SalesConfirmation>
        {
            private readonly ILogger<SalesConfirmationConsumer> _logger;
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public SalesConfirmationConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<SalesConfirmationConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<SalesConfirmation> context)
            {
                double bookPrice = context.Message.BookPrice;

                var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Book>(_mongoConf.CollectionName.Books);

                Book book = collection.Find(o => o.ID.Equals(context.Message.BookID)).SingleOrDefault();
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
                    _logger.LogInformation($"Correct information for book with BookID={context.Message.BookID}, price={bookPrice} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<SalesConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
