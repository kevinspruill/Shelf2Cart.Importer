using Importer.UI.Services;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.UI.ViewModels
{
    public class ConsoleViewModel : BindableBase
    {
        PipeClientService PipeClientService;

        public ConsoleViewModel() 
        {
            ConsoleData += "Console From S2C_ImporterPipe";

            PipeClientService = new PipeClientService("S2C_ImporterPipe");

            GetDataFromPipe();
        }

        private string _consoleData;
        public string ConsoleData
        {
            get { return _consoleData; }
            set { SetProperty(ref _consoleData, value); }
        }

        public async void GetDataFromPipe()
        {
            ConsoleData += await PipeClientService.ReadFromPipeAsync();
        }

    }
}
