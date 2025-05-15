﻿using Importer.Common.Helpers;
using Importer.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Common.Modifiers
{
    public class ProductLabelUpdater
    {
        IEnumerable<StockDescription> _stockDescriptions;
        DefaultValueLoader _defaultValueLoader;

        public ProductLabelUpdater()
        {
            DatabaseHelper DatabaseHelper = new DatabaseHelper(DatabaseType.ImportDatabase);
            _stockDescriptions = DatabaseHelper.LoadStockDescriptions();
            _defaultValueLoader = new DefaultValueLoader();
        }

        public tblProducts UpdateLabelFromStockDescription(tblProducts product)
        {
            if (!string.IsNullOrEmpty(product.StockNum))
            {
                StockDescription stockDesc = _stockDescriptions.FirstOrDefault(sd => sd.StockNum == product.StockNum);
                if (stockDesc != null)
                {
                    product.LblName = stockDesc.LblName;
                }
                else
                {
                    Logger.Warn($"Stock Description not found for StockNum: {product.StockNum}, keeping Default LblName: {product.LblName}");
                }
            }

            product = UpdateLabelNameField(product, nameof(product.LblName));

            return product;

        }

        public tblProducts UpdateLabelNamesWithExtensions(tblProducts product)
        {
            var _tempProduct = product;

            _tempProduct = UpdateLabelNameField(_tempProduct, nameof(product.LblName));
            _tempProduct = UpdateLabelNameField(_tempProduct, nameof(product.LblName2));
            _tempProduct = UpdateLabelNameField(_tempProduct, nameof(product.LblName3));
            _tempProduct = UpdateLabelNameField(_tempProduct, nameof(product.LblName4));
            _tempProduct = UpdateLabelNameField(_tempProduct, nameof(product.LblName5));

            return _tempProduct;
        }

        public tblProducts UpdateLabelNameField(tblProducts product, string fieldName)
        {
            var propertyInfo = typeof(tblProducts).GetProperty(fieldName);
            
            if (propertyInfo == null) return product;

            string fieldValue = (string)propertyInfo.GetValue(product);

            if (!string.IsNullOrEmpty(fieldValue))
            {
                if (!IsNumeric(fieldValue))
                {
                    string importLblName = fieldValue;
                    if (!importLblName.ToUpper().EndsWith(".LBL"))
                    {
                        importLblName += ".lbl";
                    }
                    propertyInfo.SetValue(product, importLblName);
                }
                else
                {
                    product.StockNum = fieldValue;

                    var stockDesc = _stockDescriptions.FirstOrDefault(sd => sd.StockNum == fieldValue);
                    if (stockDesc != null)
                    {
                        propertyInfo.SetValue(product, stockDesc.LblName);
                    }
                    else
                    {
                        Logger.Warn($"Stock Description not found for StockNum: {product.StockNum}, keeping Default LblName: {_defaultValueLoader.DefLabelName}");
                        propertyInfo.SetValue(product, _defaultValueLoader.DefLabelName);
                    }
                }
            }

            return product;
        }

        public bool IsNumeric(string value)
        {
            return double.TryParse(value, out _);
        }
    }
}
