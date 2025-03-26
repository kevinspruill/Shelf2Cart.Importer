using Importer.Common.ImporterTypes;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public static class InstanceLoader
    {
        private const string SETTINGS_FOLDER = "Settings";
        private const string SETTINGS_FILE = "ImporterInstances.json";

        public static List<ImporterInstance> LoadInstances()
        {
            string settingsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            string jsonFilePath = Path.Combine(settingsFolderPath, SETTINGS_FILE);
            List < ImporterInstance > instances = new List<ImporterInstance>();

            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                instances = JsonConvert.DeserializeObject<List<ImporterInstance>>(json);
            }
            else
            {
                Logger.Error($"JSON file not found. Expected path: {jsonFilePath}");
                return new List<ImporterInstance>();
            }

            return instances;
        }

        public static IImporterModule GetImporterModule(string moduleName)
        {
            var dlls = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            foreach (var dll in dlls)
            {
                var assembly = Assembly.LoadFrom(dll);
                var types = assembly.GetTypes()
                    .Where(t => typeof(IImporterModule).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (var type in types)
                {
                    // if the the Name property is "Invafresh" create an instance of the type
                    var instance = Activator.CreateInstance(type) as IImporterModule;
                    if (instance != null && instance.Name == moduleName)
                    {
                        Logger.Debug($"Found Configured module: {instance.Name}");
                        return instance;
                    }
                }
            }

            return null;
        }

        public static ICustomerProcess GetCustomerProcess(string Customer)
        {
            var dlls = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            foreach (var dll in dlls)
            {
                var assembly = Assembly.LoadFrom(dll);
                var types = assembly.GetTypes()
                    .Where(t => typeof(ICustomerProcess).IsAssignableFrom(t) && !t.IsAbstract);
                foreach (var type in types)
                {
                    // if the the Name property is "Invafresh" create an instance of the type
                    var instance = Activator.CreateInstance(type) as ICustomerProcess;
                    if (instance != null && instance.Name == Customer)
                    {
                        Logger.Debug($"Found Configured module: {instance.Name}");
                        return instance;
                    }
                }
            }

            return null;
        }

    }
}
