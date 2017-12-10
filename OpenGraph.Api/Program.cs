using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace OpenGraph.Api
{
    public class Program
    {
        #region Public Methods

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddAutofac())
                .UseStartup<Startup>()
                .Build();

        public static void Main(string[] args)
        {
            var latencyMode = System.Runtime.GCSettings.LatencyMode;
            var isServerGC = System.Runtime.GCSettings.IsServerGC;
            Console.WriteLine($"Server: {isServerGC}, Mode: {latencyMode}");
            BuildWebHost(args).Run();
        }

        #endregion Public Methods
    }
}