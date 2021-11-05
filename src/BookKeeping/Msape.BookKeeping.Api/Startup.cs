using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Msape.BookKeeping.Api.Infra;
using Msape.BookKeeping.Data.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Api
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

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Msape.BookKeeping.Api", Version = "v1" });
            });
            services.AddMassTransit(configure =>
            {
                configure.UsingAzureServiceBus((context, busFactory) =>
                {
                    busFactory.Host(Configuration.GetConnectionString("ServiceBus"));
                    MTModuleInitializer.Initialize(busFactory);
                });
            });
            services.AddMassTransitHostedService();
            services.AddTransactionSender();
            services.AddDbContext<BookKeepingContext>(opts =>
            {
                opts.UseSqlServer(Configuration.GetConnectionString("BookKeepingContext"),
                    server =>
                    {
                        server.EnableRetryOnFailure(3);
                    });
                opts.EnableSensitiveDataLogging(true);
            });
            services
                .AddDistributedMemoryCache()
                .AddScoped<ISubjectCache, SubjectCache>()
                .AddReceiptNumberConverter()
                .AddOptions<TransactionIdToReceiptNumberConverterOptions>()
                    .Bind(Configuration.GetSection("ReceiptNumberGenerator"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Msape.BookKeeping.Api v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
