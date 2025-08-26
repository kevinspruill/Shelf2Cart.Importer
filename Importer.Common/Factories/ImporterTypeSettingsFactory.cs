using Importer.Common.Interfaces;
using Importer.Common.Models.TypeSettings;
using Newtonsoft.Json.Linq;

namespace Importer.Common.Factories
{
    public static class ImporterTypeSettingsFactory
    {
        public static ImporterTypeSettings CreateFromJObject(JObject jObject)
        {
            var type = jObject["Type"]?.Value<string>();

            return type switch
            {
                "FileMonitor" => jObject.ToObject<FileMonitorSettings>(),
                "SchedulerService" => jObject.ToObject<SchedulerServiceSettings>(),
                _ => throw new System.ArgumentException($"Unknown TypeSettings type: {type}")
            };
        }
    }
}