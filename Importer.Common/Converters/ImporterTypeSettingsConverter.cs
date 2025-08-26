// ImporterTypeSettingsConverter.cs
using Importer.Common.Factories;
using Importer.Common.Interfaces;
using Importer.Common.Models.TypeSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Importer.Common.Converters
{
    public class ImporterTypeSettingsConverter : JsonConverter<ImporterTypeSettings>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, ImporterTypeSettings value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override ImporterTypeSettings ReadJson(JsonReader reader, Type objectType, ImporterTypeSettings existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);
            var type = jObject["Type"]?.Value<string>();

            ImporterTypeSettings settings = type switch
            {
                "FileMonitor" => new FileMonitorSettings(),
                "SchedulerService" => new SchedulerServiceSettings(),
                _ => throw new JsonSerializationException($"Unknown TypeSettings type: {type}")
            };

            // Manually populate the properties
            using (var subReader = jObject.CreateReader())
            {
                serializer.Populate(subReader, settings);
            }

            return settings;
        }
    }
}