using Importer.Core.Modes;
using Microsoft.Extensions.DependencyInjection;

namespace Importer.Service.DependencyInjection
{
    public class Container
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        public static void Initialize()
        {
            var services = new ServiceCollection();
            RegisterServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // resolve Singleton services

        }

        private static void RegisterServices(IServiceCollection services)
        {
            // Concrete providers
            services.AddSingleton<ImporterService>();

            // Provider-based services

            // Settings

            // Core services

        }
    }
}
