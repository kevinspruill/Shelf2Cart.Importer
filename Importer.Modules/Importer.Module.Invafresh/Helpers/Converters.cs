using Importer.Module.Invafresh.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Helpers
{
    public static class Converters
    {
        public static bool UMEtoScalable(UnitOfMeasure? value)
        {
            switch (value)
            {
                
                case UnitOfMeasure.BC:
                case UnitOfMeasure.FP:
                case UnitOfMeasure.FW:
                    return false;
                case UnitOfMeasure.LB:
                case UnitOfMeasure.KG:
                case UnitOfMeasure.HG:
                case UnitOfMeasure.HB:
                case UnitOfMeasure.QB:
                case UnitOfMeasure.OK:
                case UnitOfMeasure.OP:
                case UnitOfMeasure.OG:
                case UnitOfMeasure.OH:
                case UnitOfMeasure.OB:
                case UnitOfMeasure.OQ:
                    return true;
                default:
                    return false;
            }
        }
    }
}
