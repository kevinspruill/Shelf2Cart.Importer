// IImporterTypeSettings.cs
using Importer.Common.Converters;
using Newtonsoft.Json;

namespace Importer.Common.Interfaces
{
    [JsonConverter(typeof(ImporterTypeSettingsConverter))]
    public abstract class ImporterTypeSettings
    {
        public virtual string Type { get; set; }
    }
}