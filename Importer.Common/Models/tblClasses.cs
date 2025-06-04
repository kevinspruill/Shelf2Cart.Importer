using System;

namespace Importer.Common.Models
{

    public class tblClasses
    {
        [ImportDBField("ClassNum")]
        public int ClassNum { get; set; }

        [ImportDBField("Class")]
        public string Class { get; set; } = string.Empty;

        [ImportDBField("DeptNum")]
        public int DeptNum { get; set; }

        [ImportDBField("PageNum")]
        public string PageNum { get; set; }
    }

}