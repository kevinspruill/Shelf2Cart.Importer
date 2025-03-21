using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Importer.Core.Modes;
using Importer.Module.Invafresh;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

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
                    ServiceMode.RunServiceMode();
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
