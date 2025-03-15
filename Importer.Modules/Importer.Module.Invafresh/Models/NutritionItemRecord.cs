using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Models
{
    public class NutritionItemRecord : BaseRecord
    {
        public int NutritionNumber { get; set; }  // NTN - Required
        public int? LabelFormatNumber { get; set; }  // LF1
        public string ServingsPerContainer { get; set; }  // SPC
        public string ServingSizeDescription { get; set; }  // SSZ
        public Dictionary<string, Tuple<int?, int?>> NutritionValues { get; set; } = new Dictionary<string, Tuple<int?, int?>>();

        // Current nutrition data format (Appendix C)
        // Key is the nutrition field code (e.g., "CAL", "FAT")
        // Value tuple is (Value, Percentage) - both with one implied decimal place

        public NutritionItemRecord()
        {
            // Initialize with all possible nutrition values
            InitializeNutritionValues();
        }

        private void InitializeNutritionValues()
        {
            // Calories and Energy
            NutritionValues["CAL"] = new Tuple<int?, int?>(null, null); // Calories
            NutritionValues["CFF"] = new Tuple<int?, int?>(null, null); // Calories From Fat
            NutritionValues["CEN"] = new Tuple<int?, int?>(null, null); // CalEnergy
            NutritionValues["EGY"] = new Tuple<int?, int?>(null, null); // Energy
            NutritionValues["CSF"] = new Tuple<int?, int?>(null, null); // CalSatFat

            // Fats
            NutritionValues["FAT"] = new Tuple<int?, int?>(null, null); // Total fat
            NutritionValues["SAF"] = new Tuple<int?, int?>(null, null); // Saturated Fat
            NutritionValues["TFT"] = new Tuple<int?, int?>(null, null); // TransFat
            NutritionValues["MUS"] = new Tuple<int?, int?>(null, null); // MonoUnSatFat
            NutritionValues["PUS"] = new Tuple<int?, int?>(null, null); // PolyUnSatFat
            NutritionValues["STF"] = new Tuple<int?, int?>(null, null); // SatTranFat
            NutritionValues["O3F"] = new Tuple<int?, int?>(null, null); // Omega 3 Fatty
            NutritionValues["O6F"] = new Tuple<int?, int?>(null, null); // Omega 6 Fatty

            // Carbohydrates
            NutritionValues["TCA"] = new Tuple<int?, int?>(null, null); // Total Carbohydrate
            NutritionValues["FIB"] = new Tuple<int?, int?>(null, null); // Dietary fiber
            NutritionValues["SUG"] = new Tuple<int?, int?>(null, null); // Sugar
            NutritionValues["ASU"] = new Tuple<int?, int?>(null, null); // Added Sugars
            NutritionValues["OCB"] = new Tuple<int?, int?>(null, null); // Other Carbs
            NutritionValues["ISF"] = new Tuple<int?, int?>(null, null); // Insoluble Fiber
            NutritionValues["SFB"] = new Tuple<int?, int?>(null, null); // Soluble Fiber 
            NutritionValues["SAH"] = new Tuple<int?, int?>(null, null); // Sugar Alcohol
            NutritionValues["STC"] = new Tuple<int?, int?>(null, null); // Starch

            // Cholesterol and Sodium
            NutritionValues["CHO"] = new Tuple<int?, int?>(null, null); // Cholesterol
            NutritionValues["SOD"] = new Tuple<int?, int?>(null, null); // Sodium

            // Other Nutrients
            NutritionValues["PRO"] = new Tuple<int?, int?>(null, null); // Protein

            // Minerals
            NutritionValues["CAC"] = new Tuple<int?, int?>(null, null); // Calcium
            NutritionValues["IRO"] = new Tuple<int?, int?>(null, null); // Iron
            NutritionValues["POT"] = new Tuple<int?, int?>(null, null); // Potassium
            NutritionValues["CHL"] = new Tuple<int?, int?>(null, null); // Chloride
            NutritionValues["COP"] = new Tuple<int?, int?>(null, null); // Copper
            NutritionValues["CRO"] = new Tuple<int?, int?>(null, null); // Chromium
            NutritionValues["IOD"] = new Tuple<int?, int?>(null, null); // Iodine
            NutritionValues["MAG"] = new Tuple<int?, int?>(null, null); // Magnesium
            NutritionValues["MAN"] = new Tuple<int?, int?>(null, null); // Manganese
            NutritionValues["MOL"] = new Tuple<int?, int?>(null, null); // Molybdenum
            NutritionValues["PHO"] = new Tuple<int?, int?>(null, null); // Phosphorous
            NutritionValues["SEL"] = new Tuple<int?, int?>(null, null); // Selenium
            NutritionValues["ZIN"] = new Tuple<int?, int?>(null, null); // Zinc

            // Vitamins
            NutritionValues["VIA"] = new Tuple<int?, int?>(null, null); // Vitamin A
            NutritionValues["VIC"] = new Tuple<int?, int?>(null, null); // Vitamin C
            NutritionValues["VID"] = new Tuple<int?, int?>(null, null); // Vitamin D
            NutritionValues["VIK"] = new Tuple<int?, int?>(null, null); // Vitamin K
            NutritionValues["THI"] = new Tuple<int?, int?>(null, null); // Thiamine
            NutritionValues["RIB"] = new Tuple<int?, int?>(null, null); // Riboflavin
            NutritionValues["NIA"] = new Tuple<int?, int?>(null, null); // Niacin
            NutritionValues["PAN"] = new Tuple<int?, int?>(null, null); // Pantothenic Acid
            NutritionValues["VB6"] = new Tuple<int?, int?>(null, null); // Vitamin B6
            NutritionValues["FOL"] = new Tuple<int?, int?>(null, null); // Folate
            NutritionValues["FOA"] = new Tuple<int?, int?>(null, null); // Folic Acid
            NutritionValues["V12"] = new Tuple<int?, int?>(null, null); // Vitamin B12
            NutritionValues["BIO"] = new Tuple<int?, int?>(null, null); // Biotin
            NutritionValues["BCR"] = new Tuple<int?, int?>(null, null); // Beta-Carotene
        }
    }
}
