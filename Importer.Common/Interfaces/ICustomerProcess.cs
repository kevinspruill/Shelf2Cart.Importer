using Importer.Common.Models;

namespace Importer.Common.Interfaces
{
    public interface ICustomerProcess
    {
        string Name { get; }
        bool ForceUpdate { get; set; }
        void PreQueryRoutine();
        void DataFileCondtioning<T>(T ImportData = null) where T : class;
        tblProducts PreProductProcess(tblProducts product);
        void PreProductProcess();
        tblProducts ProductProcessor(tblProducts product);
        void PostProductProcess();
        void PostQueryRoutine();
        
    }
}