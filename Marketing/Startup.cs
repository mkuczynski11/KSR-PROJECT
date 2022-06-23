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
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitConfiguration.ServerAddress), settings =>
                    {
                        settings.Username(rabbitConfiguration.Username);
                        settings.Password(rabbitConfiguration.Password);
                    });

                    cfg.ReceiveEndpoint("marketing-book-creation-event", ep =>
                    {
                        ep.ConfigureConsumer<NewBookMarketingInfoConsumer>(context);
                    });
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
    }
}
