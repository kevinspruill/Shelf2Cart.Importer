using Importer.Common.Helpers;
using Importer.Common.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExecuteProcessDatabase
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Logger.Info("Manually triggering Process Database action");
            try
            {
                ProcessDatabase processDatabase = new ProcessDatabase();
                await processDatabase.ProcessImport();
                Logger.Info("Manual Process Database action finished");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error manually triggering Process Database action - {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
        }
    }
}
