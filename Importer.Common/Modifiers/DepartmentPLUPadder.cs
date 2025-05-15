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
            DatabaseHelper DatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);
            _deptPadding = DatabaseHelper.LoadDepartmentPadding();
        }

        public long PadPLU(string department, long originalPLU, int pluLengthLimit=0)
        {

            if (_deptPadding.TryGetValue(department, out int paddingValue))
            {
                // If the PLU length limit is set, pad the PLU to the limit
                if (originalPLU.ToString().Length > pluLengthLimit && pluLengthLimit != 0)
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
