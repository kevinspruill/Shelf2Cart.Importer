using Importer.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Modifiers
{
    public class DepartmentPLUPadder
    {
        private readonly Dictionary<string, int> _deptPadding;

        public DepartmentPLUPadder()
        {
            _deptPadding = DatabaseHelper.LoadDepartmentPadding();
        }

        public long PadPLU(string department, long originalPLU, int pluLengthLimit=0)
        {
            // make sure department is always 2 characters, pad with 0 if necessary
            department = department.PadLeft(2, '0');

            // if originalPLU starts with "3838100" remove it and check digit
            var pluString = originalPLU.ToString();
            if (pluString.StartsWith("3838100"))
            {
                if (pluString.StartsWith("3838100000"))
                {
                    Console.WriteLine("PLU is too long, cannot pad. PLU: " + pluString);
                }

                // remove the first 7 characters
                pluString = pluString.Substring(7);

                

                // remove the last digit
                pluString = pluString.Substring(0, pluString.Length - 1);

                originalPLU = long.Parse(pluString);
            }

            if (_deptPadding.TryGetValue(department, out int paddingValue))
            {
                // If the PLU length limit is set, pad the PLU to the limit
                if (originalPLU.ToString().Length > pluLengthLimit && pluLengthLimit != 0)
                {
                    return originalPLU;
                }
                // If the first two characters of the PLU are the same as the padding value, return the original PLU
                else if (originalPLU.ToString().PadLeft(2, '0').Substring(0, 2) == paddingValue.ToString().Substring(0, 2))
                {
                    return originalPLU;
                }
                else
                {
                    return paddingValue + originalPLU;
                }                  
            }
            return originalPLU; // If no padding is defined for the department, return the original PLU
        }

        public long UnpadPLU(string department, long paddedPLU)
        {
            if (_deptPadding.TryGetValue(department, out int paddingValue))
            {
                return paddedPLU - paddingValue;
            }
            return paddedPLU; // If no padding is defined for the department, return the padded PLU
        }


    }
}
