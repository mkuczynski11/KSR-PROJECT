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
    }
}
