
using Importer.Common.Interfaces;
using Importer.Common.Models;
using Importer.Common.Modifiers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Importer.Common.Helpers
{
    public class ProductProcessor
    {
        DefaultValueLoader _defaultValues;
        SettingsLoader _settings;
        DepartmentPLUPadder _departmentPLUPadder;
        DescriptionCleanse _descriptionCleanse;
        ProductLabelUpdater _productLabelUpdater;
        ICustomerProcess _customProductProcessor;
        FindReplace _findReplace;

        public List<tblProducts> Products;

        public ProductProcessor(ICustomerProcess customerProcess)
        {
            Logger.Debug("Initializing ProductProcessor");

            DatabaseHelper DatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);

            _defaultValues = new DefaultValueLoader();
            _settings = new SettingsLoader();
            _departmentPLUPadder = new DepartmentPLUPadder();
            _descriptionCleanse = new DescriptionCleanse(DatabaseHelper.LoadCleanseRules());
            _productLabelUpdater = new ProductLabelUpdater();
            _findReplace = new FindReplace();
            _customProductProcessor = customerProcess;

            Products = new List<tblProducts>();

            Logger.Debug("ProductProcessor initialized");
        }

        public tblProducts CreateProductTemplate() //TODO Extract and refactor from ProductProcessor so modules can create this
        {
            Logger.Trace("Creating product template");
            tblProducts product = new tblProducts();
            product = _defaultValues.ApplyDefaultValues(product);
            Logger.Trace("Product template created");
            return product;
        }

        public async Task<tblProducts> ProcessProduct(tblProducts product)
        {
            try
            {
                Logger.Debug($"Processing product: PLU={product.PLU}");

                if (_settings.DepartmentPLU)
                {
                    Logger.Trace($"Applying department padding for PLU={product.PLU}");
                    var paddedPLU = _departmentPLUPadder.PadPLU(product.Dept, long.TryParse(product.PLU, out long parsedPLU) ? parsedPLU : 0, 5);
                    product.PLU = paddedPLU.ToString();
                }

                if (_settings.DescriptionCleanse)
                {

                    Logger.Trace("Applying description cleanse");
                    product = _descriptionCleanse.CleanseProductDescription(product);

                }

                //Logger.Trace("Applying number formatting");
                //product = NumberFormatProcessor.ApplyNumberFormatting(product);

                Logger.Trace("Applying ProperCase and FindReplace");
                foreach (var prop in typeof(tblProducts).GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        string value = (string)prop.GetValue(product);

                        if (_settings.ShouldApplyFindReplace(prop))
                        {
                            value = _findReplace.ApplyReplacements(value);
                        }

                        if (_settings.ShouldApplyProperCase(prop))
                        {
                            value = TitleCaseConverter.ConvertToTitleCase(value);
                        }
                        else if (_settings.ShouldApplyAllCaps(prop))
                        {
                            value = TitleCaseConverter.ConvertToAllCaps(value);
                        }

                        prop.SetValue(product, value);
                    }
                }

                Logger.Trace("Applying StockNum Translation");
                product = _productLabelUpdater.UpdateLabelFromStockDescription(product);

                Logger.Trace("Updating label names with extensions");
                product = _productLabelUpdater.UpdateLabelNamesWithExtensions(product);

                if (_settings.ButtonSameAsDesc)
                {
                    Logger.Trace("Updating button descriptions");
                    product.Button1 = product.Description1;
                    product.Button2 = product.Description2;
                }
                
                if (_customProductProcessor != null)
                {
                    Logger.Trace("Applying custom Importer product processing");
                    product = _customProductProcessor.ProductProcessor(product);
                }

                Logger.Debug($"Product processing completed: PLU={product.PLU}");
                return product;
            
            }
            catch (Exception ex)
            {
                Logger.Error($"An error occurred processing product: PLU={product.PLU}", ex);
                return product;
            }
            
        }
    }
}