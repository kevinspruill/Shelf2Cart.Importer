using Prism.Mvvm;
using Prism.Commands;
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Importer.UI.ViewModels
{
    public class DefaultValuesViewModel : BindableBase
    {
        private const string DefaultsFileRelativePath = "Importer.Common/Settings/defaultValues.json";
        private string _defaultsFileFullPath;

        private string _defCatNum;
        private string _defPrice;
        private string _defShelfLife;
        private string _defNetWt;
        private string _defBarcodeVal;
        private string _defBarType;
        private string _defLabelName;

        public string DefCatNum { get => _defCatNum; set => SetProperty(ref _defCatNum, value); }
        public string DefPrice { get => _defPrice; set => SetProperty(ref _defPrice, value); }
        public string DefShelfLife { get => _defShelfLife; set => SetProperty(ref _defShelfLife, value); }
        public string DefNetWt { get => _defNetWt; set => SetProperty(ref _defNetWt, value); }
        public string DefBarcodeVal { get => _defBarcodeVal; set => SetProperty(ref _defBarcodeVal, value); }
        public string DefBarType { get => _defBarType; set => SetProperty(ref _defBarType, value); }
        public string DefLabelName { get => _defLabelName; set => SetProperty(ref _defLabelName, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ReloadCommand { get; }

        public DefaultValuesViewModel()
        {
            SaveCommand = new DelegateCommand(SaveDefaults);
            ReloadCommand = new DelegateCommand(LoadDefaults);
            ResolveDefaultsPath();
            LoadDefaults();
        }

        private void ResolveDefaultsPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "Settings", "defaultValues.json"),
                Path.Combine(baseDir, DefaultsFileRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "Importer.Common", "Settings", "defaultValues.json"))
            };
            _defaultsFileFullPath = candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths.Last();
        }

        private void LoadDefaults()
        {
            if (!File.Exists(_defaultsFileFullPath)) return;
            var json = File.ReadAllText(_defaultsFileFullPath);
            var model = JsonConvert.DeserializeObject<DefaultValuesModel>(json);
            if (model == null) return;
            DefCatNum = model.DefCatNum;
            DefPrice = model.DefPrice;
            DefShelfLife = model.DefShelfLife;
            DefNetWt = model.DefNetWt;
            DefBarcodeVal = model.DefBarcodeVal;
            DefBarType = model.DefBarType;
            DefLabelName = model.DefLabelName;
        }

        private void SaveDefaults()
        {
            var model = new DefaultValuesModel
            {
                DefCatNum = DefCatNum,
                DefPrice = DefPrice,
                DefShelfLife = DefShelfLife,
                DefNetWt = DefNetWt,
                DefBarcodeVal = DefBarcodeVal,
                DefBarType = DefBarType,
                DefLabelName = DefLabelName
            };
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_defaultsFileFullPath));
            File.WriteAllText(_defaultsFileFullPath, json);
        }

        private class DefaultValuesModel
        {
            public string DefCatNum { get; set; }
            public string DefPrice { get; set; }
            public string DefShelfLife { get; set; }
            public string DefNetWt { get; set; }
            public string DefBarcodeVal { get; set; }
            public string DefBarType { get; set; }
            public string DefLabelName { get; set; }
        }
    }
}
