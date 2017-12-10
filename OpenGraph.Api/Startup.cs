using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CacheManager.Core;
using CacheManager.Serialization.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OpenGraph.Api
{
    public class Startup
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public Startup(IConfiguration configuration)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the application container.
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        #endregion Public Properties

        #region Public Methods

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //Set mvc
            services.AddMvc();

            //Set DI (Autofac)
            var builder = new ContainerBuilder();
            builder.Populate(services);

            //Create cache dependency
            var cache = CacheFactory.Build<string>(settings =>
            {
                //Get settings
                string redisconnection = Environment.GetEnvironmentVariable("redis");
                string database = Environment.GetEnvironmentVariable("database");

                //Writeline
                Console.WriteLine($"Initializing connection. Using Redis: {!string.IsNullOrWhiteSpace(redisconnection) && !string.IsNullOrWhiteSpace(database)}. Connection: {redisconnection}. Database: {database}");

                //Set with redis enabled
                if (!string.IsNullOrWhiteSpace(redisconnection) && int.TryParse(database, out int db))
                    settings
                        .WithDictionaryHandle("url")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(2))
                        .And
                        .WithSerializer(typeof(GzJsonCacheSerializer))
                        .WithRedisConfiguration("redis", redisconnection, db)
                        .WithMaxRetries(100)
                        .WithRetryTimeout(100)
                        .WithRedisBackplane("redis")
                        .WithRedisCacheHandle("redis")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(15));

                //Set with no redis usage
                else
                    settings
                        .WithDictionaryHandle("url")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(15));
            });

            //Add cache as singleton
            builder.RegisterInstance(cache).SingleInstance();

            //Build container
            ApplicationContainer = builder.Build();

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(ApplicationContainer);
        }

        #endregion Public Methods
    }
}