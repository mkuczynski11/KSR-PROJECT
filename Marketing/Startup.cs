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
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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
            private readonly ILogger<NewBookMarketingInfoConsumer> _logger;
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public NewBookMarketingInfoConsumer(MongoClient mongoClient, ILogger<NewBookMarketingInfoConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<NewBookMarketingInfo> context)
            {
                var bookID = context.Message.ID;
                var bookDiscount = context.Message.discount;
                Book book = new Book(bookID, bookDiscount);

                var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                .GetCollection<Book>(_mongoConf.CollectionName.Books);
                collection.InsertOne(book);

                _logger.LogInformation($"New book registered: {bookID}, discount={bookDiscount}");
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitConfiguration = Configuration.GetSection("RabbitMQ").Get<RabbitMQConfiguration>();
            var endpointConfiguration = Configuration.GetSection("Endpoint").Get<EndpointConfiguration>();
            var mongoDbConfiguration = Configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();

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

                    cfg.ReceiveEndpoint(endpointConfiguration.NewBookConsumer, ep =>
                    {
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<NewBookMarketingInfoConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(endpointConfiguration.ConfirmationConsumer, ep =>
                    {
                        ep.UseInMemoryOutbox();
                        ep.UseMessageRetry(r => r.Incremental(5, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200)));
                        ep.ConfigureConsumer<MarketingConfirmationConsumer>(context);
                    });

                    cfg.UseScheduledRedelivery(r => r.Interval(2, TimeSpan.FromSeconds(rabbitConfiguration.DelayedRedeliverySeconds)));
                    cfg.UseInMemoryScheduler();
                });
            });

            services.AddDbContext<BookContext>(opt => opt.UseInMemoryDatabase("MarketingBookList"));
            services.AddHealthChecks()
                .AddDbContextCheck<BookContext>()
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

        class MarketingConfirmationConsumer : IConsumer<MarketingConfirmation>
        {
            private readonly ILogger<MarketingConfirmationConsumer> _logger;
            private MongoClient _mongoClient;
            private readonly MongoDbConfiguration _mongoConf;
            public readonly IPublishEndpoint _publishEndpoint;
            public MarketingConfirmationConsumer(MongoClient mongoClient, IPublishEndpoint publishEndpoint, ILogger<MarketingConfirmationConsumer> logger, IConfiguration configuration)
            {
                _logger = logger;
                _publishEndpoint = publishEndpoint;
                _mongoClient = mongoClient;
                _mongoConf = configuration.GetSection("MongoDb").Get<MongoDbConfiguration>();
            }
            public async Task Consume(ConsumeContext<MarketingConfirmation> context)
            {
                double bookDiscount = context.Message.BookDiscount;

                var collection = _mongoClient.GetDatabase(_mongoConf.DatabaseName)
                    .GetCollection<Book>(_mongoConf.CollectionName.Books);

                Book book = collection.Find(o => o.ID.Equals(context.Message.BookID)).SingleOrDefault();
                if (book == null)
                {
                    _logger.LogError($"Book with BookID{context.Message.BookID}, discount={bookDiscount} was not found for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<MarketingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });

                }
                else if (bookDiscount != book.Discount)
                {
                    _logger.LogError($"Wrong discount for book with BookID{context.Message.BookID}, discount={bookDiscount} for request={context.Message.CorrelationId}.");
                    await _publishEndpoint.Publish<MarketingConfirmationRefuse>(new { CorrelationId = context.Message.CorrelationId });
                }
                else
                {
                    _logger.LogInformation($"Book with BookID{context.Message.BookID}, discount={bookDiscount} information is valid.");
                    await _publishEndpoint.Publish<MarketingConfirmationAccept>(new { CorrelationId = context.Message.CorrelationId });
                }
            }
        }
    }
}
