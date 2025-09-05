using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleG.CustomerProcess.Models
{
    public class MajorDept
    {
        public int MajorDeptId { get; set; }         // PK
        public string MajorDeptName { get; set; }
    }

    public class Category
    {
        public int CategoryId { get; set; }          // PK
        public int MajorDeptId { get; set; }         // FK -> MajorDept
        public string CategoryName { get; set; }
    }

    public class SubCategory
    {
        public int SubCategoryId { get; set; }       // PK
        public int CategoryId { get; set; }          // FK -> Category
        public string SubCategoryName { get; set; }
    }

    public class Item
    {
        public long ItemId { get; set; }              // PK
        public int SubCategoryId { get; set; }       // FK -> SubCategory
        public string ItemNumber { get; set; }      // e.g. your old "Item_ID"
        public string ItemName { get; set; }
        public string ItemDept { get; set; }
    }

}
