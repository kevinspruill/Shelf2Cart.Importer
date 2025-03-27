using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Module.Invafresh.Models;
using System;

namespace Wakefern.CustomerProcess
{
    public class WakefernProcess : ICustomerProcess
    {
        public string Name => "Wakefern";

        public bool ForceUpdate { get; set; } = false;

        public T DataFileCondtioning<T>(T ImportData = null) where T : class
        {
            if (ImportData is IngredientItemRecord ingredientItem)
            {
                Logger.Trace($"Cleaning Ingredient Text: IngredientNumber={ingredientItem.IngredientNumber}");
                ingredientItem.IngredientText = CleanIngredients(ingredientItem.IngredientText);
                return ingredientItem as T;
            }
            return ImportData;
        }

        private string CleanIngredients(string ingredientText)
        {
            var cleanedIngredients = ingredientText;

            cleanedIngredients = cleanedIngredients.Replace("(", " (").Replace(")", ") ").Replace("[", " [").Replace("]", "] ")
                .Replace(":", ": ").Replace(" ,", ",").Replace(",", ", ").Replace(",  ", ", ").Replace(".", ". ").Replace(".  ", ". ")
                .Replace(" .", ".");

            //Clean odd characters from Linux machines and/or encoding issues
            cleanedIngredients = cleanedIngredients.Replace(((char)131).ToString(), string.Empty).Replace(((char)167).ToString(), string.Empty)
                .Replace(((char)192).ToString(), string.Empty).Replace(((char)194).ToString(), string.Empty).Replace(((char)195).ToString(), string.Empty)
                .Replace(((char)197).ToString(), string.Empty).Replace(((char)14).ToString(), string.Empty).Replace(((char)0).ToString(), string.Empty);

            cleanedIngredients = string.Join(" ", cleanedIngredients.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            return cleanedIngredients;
        }

        public void PostProductProcess()
        {

        }

        public void PostQueryRoutine()
        {

        }

        public tblProducts PreProductProcess(tblProducts product)
        {
            //
            return product;
        }

        public void PreProductProcess()
        {

        }

        public void PreQueryRoutine()
        {

        }

        public tblProducts ProductProcessor(tblProducts product)
        {
            //
            return product;
        }
    }
}
