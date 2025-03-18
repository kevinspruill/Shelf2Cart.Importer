using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public static class ImpliedDecimalExtensions
    {
        // Int extensions
        public static decimal ToPrice(this int? value) => (decimal)(value / 100m);
        public static decimal ToFixedWeight(this int? value, bool useImpliedDecimal = false)
            => (decimal)(useImpliedDecimal ? value / 10m : value);
        public static decimal ToTare(this int? value, bool useTwoDigitPrecision = false)
            => (decimal)(useTwoDigitPrecision ? value / 100m : value / 1000m);
        public static decimal ToNutrition(this int? value) => (decimal)(value / 10m);

        // Decimal extensions
        public static int ToPriceInteger(this decimal value) => (int)(value * 100m);
        public static int ToFixedWeightInteger(this decimal value, bool useImpliedDecimal = false)
            => useImpliedDecimal ? (int)(value * 10m) : (int)value;
        public static int ToTareInteger(this decimal value, bool useTwoDigitPrecision = false)
            => useTwoDigitPrecision ? (int)(value * 100m) : (int)(value * 1000m);
        public static int ToNutritionInteger(this decimal value) => (int)(value * 10m);
    }
}
