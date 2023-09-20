using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentManager.Core;
using InvestmentManager.DataAccess.EF;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace InvestmentManager
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            this.loggerFactory = loggerFactory;

            // For NLog
            NLog.LogManager.LoadConfiguration("nlog.config");
        }

        public IConfiguration Configuration { get; }

        private readonly ILoggerFactory loggerFactory;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddSingleton<IConfiguration>(this.Configuration);

            // Configure the data access layer
            var connectionString = this.Configuration.GetConnectionString("InvestmentDatabase");

            services.RegisterEfDataAccessClasses(connectionString, loggerFactory);

            // For Application Services
            String stockIndexServiceUrl = this.Configuration["StockIndexServiceUrl"];
            services.ConfigureStockIndexServiceHttpClientWithoutProfiler(stockIndexServiceUrl);
            services.ConfigureInvestmentManagerServices(stockIndexServiceUrl);

            // Configure logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
                loggingBuilder.AddNLog();
            });

            services.AddHealthChecks()
            .AddSqlServer(connectionString, failureStatus: HealthStatus.Unhealthy, tags: new[] { "Ready" })
            .AddUrlGroup(new Uri($"{stockIndexServiceUrl}/api/StockIndexes"), "Stock Indexes Health Check", HealthStatus.Degraded, tags: new[] { "Ready" }, timeout: new TimeSpan(0, 0, 5));
        }

        // Configures the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/Home/Error");

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions()
                {
                    ResultStatusCodes =
                    {
                        [HealthStatus.Healthy] = StatusCodes.Status200OK,
                        [HealthStatus.Degraded] = StatusCodes.Status500InternalServerError,
                        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                    },
                    ResponseWriter = WriteGealthCheckReadyReposne,
                    Predicate = (check) => check.Tags.Contains("Ready"),
                    AllowCachingResponses = false
                });
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
                {
                    ResponseWriter = WriteGealthCheckLiveReposne,
                    Predicate = (check) => !check.Tags.Contains("Ready"),
                    AllowCachingResponses = false
                });
            });
        }

        private Task WriteGealthCheckLiveReposne(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("OverallStatus", report.Status.ToString()),
                new JProperty("TotalCheckDuration", report.TotalDuration.TotalSeconds.ToString("0:0.00"))
                );

            return context.Response.WriteAsync(json.ToString());
        }

        private Task WriteGealthCheckReadyReposne(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("OverallStatus", report.Status.ToString()),
                new JProperty("TotalCheckDuration", report.TotalDuration.TotalSeconds.ToString("0:0.00")),
                new JProperty("DependecyHealthChecks", new JObject(report.Entries.Select(dict =>
                    new JProperty(dict.Key, new JObject(
                        new JProperty("OverallStatus", dict.Value.Status.ToString()),
                        new JProperty("TotalCheckDuration", dict.Value.Duration.TotalSeconds.ToString("0:0.00"))
                        ))
                    )))
                );

            return context.Response.WriteAsync(json.ToString());
        }
    }
}