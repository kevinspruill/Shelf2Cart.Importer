using Importer.Common.Helpers;
using Importer.Core.Modes;
using Importer.Service.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Importer.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                string filePath = string.Empty;
                Logger.Info("Application starting");

                Logger.Info("Initializing Dependency Injection");
                Container.Initialize();

                // TopShelf commands that we need to look for
                string[] topshelfCommands = { "install", "uninstall", "start", "stop", "help" };
                
                // If arguments are TopShelf commands, let ServiceMode handle them
                if (args.Length > 0 && topshelfCommands.Contains(args[0].ToLower()))
                {
                    Logger.Info($"Detected TopShelf command: {args[0]}");
                    ServiceMode.RunServiceMode(args);
                    return;
                }

                // If started by Windows Service Manager, just run the service directly
                if (args.Length > 0 && (args[0].StartsWith("-") || args[0].StartsWith("/")))
                {
                    Logger.Info("Started by Windows Service Manager");
                    ServiceMode.RunServiceMode(args);
                    return;
                }

                // Normal operation continues below with file processing
                if (args.Length > 0)
                {
                    filePath = args[0];
                    if (!File.Exists(filePath))
                    {
                        Logger.Error($"{filePath} does not exist or Permission is Denied");
                        return;
                    }
                }

                SettingsLoader settingsLoader = new SettingsLoader();

                if (settingsLoader.TestingEnabled)
                {
                    Logger.Info("Running in test mode");
                    await TestingMode.RunTestMode(filePath);
                }
                else
                {
                    Logger.Info("Running in service mode");
                    ServiceMode.RunServiceMode(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal("An unhandled exception occurred in the main program", ex);
                Console.WriteLine("An error occurred. Check the log file for details.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                Logger.Info("Application shutting down");
            }
        }       

    }
}
