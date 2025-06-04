using System;

namespace Importer.Common.Models
{

    public class tblCategories
    {
        [ImportDBField("CategoryNum")]
        public int CategoryNum { get; set; }

        [ImportDBField("Category")]
        public string Category { get; set; } = string.Empty;

        [ImportDBField("ClassNum")]
        public int ClassNum { get; set; }

        [ImportDBField("PageNum")]
        public string PageNum { get; set; }
    }

}