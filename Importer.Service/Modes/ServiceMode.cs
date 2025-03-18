using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Importer.Core.Modes
{
    public static class ServiceMode
    {
        // TopShelf Service Mode Intialization
        public static void RunServiceMode()
        {
            try
            {
                HostFactory.Run(x =>
                {
                    x.Service<ImporterService>(s =>
                    {
                        s.ConstructUsing(name => new ImporterService());
                        s.WhenStarted(async tc => await tc.Start());
                        s.WhenStopped(async tc => await tc.Stop());
                    });
                    x.RunAsLocalSystem();
                    x.SetDescription("This service will import data a Data Host into Shelf 2 Cart Merchandiser");
                    x.SetDisplayName("Shelf 2 Cart Importer Service");
                    x.SetServiceName("S2C_ImporterService");

                    x.OnException(ex =>
                    {
                        Logger.Error("An error occurred in the service", ex);
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred while setting up the service", ex);
                Console.WriteLine($"An error occurred while setting up the service: {ex.Message}");
                Console.WriteLine("Check the log file for more details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

    public class ImporterService
    {      

        public async Task Start()
        {
            // Create Scheduler with Quartz (if needed)
            // Create FileWatchers for ImportModules
            // Create API Clients for ImportModules
            // Create Named Pipes Service for UI

        }

        public async Task Stop()
        {
        }
    }
}
