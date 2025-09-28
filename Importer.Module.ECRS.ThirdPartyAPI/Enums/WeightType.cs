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
    public enum WeightType
    {
        [EnumMember(Value = null)]
        Unknown = 0,

        [EnumMember(Value = "By Pound")]
        ByPound,

        [EnumMember(Value = "By Ounce")]
        ByOunce,

        [EnumMember(Value = "By Kilogram")]
        ByKilogram,

        [EnumMember(Value = "By Gram")]
        ByGram,

        [EnumMember(Value = "Fixed Weight")]
        FixedWeight,

        [EnumMember(Value = "By Each")]
        ByEach
    }

}
