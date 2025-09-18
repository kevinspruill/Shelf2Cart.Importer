using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.ECRS.ThirdPartyAPI.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ForceShelfLifeOption
    {
        [EnumMember(Value = "Unknown")]
        Unknown = 0,

        [EnumMember(Value = "No")]
        No,

        [EnumMember(Value = "Yes")]
        Yes
    }
}
