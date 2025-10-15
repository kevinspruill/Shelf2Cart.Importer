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

        // InstanceLoader.cs (partial)
        public static List<ImporterInstance> LoadInstances()
        {
            string settingsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SETTINGS_FOLDER);
            string jsonFilePath = Path.Combine(settingsFolderPath, SETTINGS_FILE);
            List<ImporterInstance> instances = new List<ImporterInstance>();

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
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var allDlls = Directory.GetFiles(baseDir, "Importer.Module.*.dll");

            // Prioritize the requested module first to avoid tripping on unrelated bad plugins
            var preferred = allDlls.Where(p =>
                string.Equals(Path.GetFileNameWithoutExtension(p), $"Importer.Module.{moduleName}", StringComparison.OrdinalIgnoreCase));
            var rest = allDlls.Except(preferred);

            foreach (var dll in preferred.Concat(rest))
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(dll);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load assembly '{dll}': {ex.Message}");
                    continue;
                }

                IEnumerable<Type> candidateTypes = Enumerable.Empty<Type>();
                try
                {
                    candidateTypes = assembly.GetTypes()
                        .Where(t => typeof(IImporterModule).IsAssignableFrom(t) && !t.IsAbstract);
                }
                catch (ReflectionTypeLoadException rtle)
                {
                    // Log detailed loader exceptions
                    foreach (var lex in rtle.LoaderExceptions.Where(e => e != null))
                    {
                        Logger.Error($"Type load error in '{Path.GetFileName(dll)}': {lex.Message}");
                    }
                    // Keep any successfully loaded types
                    candidateTypes = (rtle.Types ?? Array.Empty<Type>())
                        .Where(t => t != null && typeof(IImporterModule).IsAssignableFrom(t) && !t.IsAbstract);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to enumerate types in '{dll}': {ex.Message}");
                    continue;
                }

                foreach (var type in candidateTypes)
                {
                    IImporterModule instance = null;
                    try
                    {
                        instance = Activator.CreateInstance(type) as IImporterModule;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to instantiate '{type.FullName}' from '{dll}': {ex.Message}");
                        continue;
                    }

                    if (instance != null && string.Equals(instance.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info($"{instance.Name} Module Loaded");
                        return instance;
                    }
                }
            }

            Logger.Error($"Importer module '{moduleName}' not found.");
            return null;
        }

        public static ICustomerProcess GetCustomerProcess(string Customer)
        {
            var dlls = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.CustomerProcess.dll");
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
                        Logger.Info($"{instance.Name} Process Loaded");
                        return instance;
                    }
                }
            }

            return new BaseProcess();
        }
    }
}
