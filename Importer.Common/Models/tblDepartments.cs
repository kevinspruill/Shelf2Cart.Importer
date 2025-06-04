using System;

namespace Importer.Common.Models
{

    public class tblDepartments
    {
        [ImportDBField("DeptNum")]
        public int DeptNum { get; set; }

        [ImportDBField("DeptName")]
        public string DeptName { get; set; } = string.Empty;

        [ImportDBField("PageNum")]
        public string PageNum { get; set; }
    }

}