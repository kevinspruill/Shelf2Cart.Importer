using Importer.Common.Models;

namespace Importer.Common.Interfaces
{
    public interface ICustomerProcess
    {
        string Name { get; }
        bool ForceUpdate { get; set; }
        void PreQueryRoutine();
        tblProducts PreProductProcess(tblProducts product=null);
        void PreProductProcess();
        tblProducts ProductProcessor(tblProducts product);
        void PostProductProcess();
        void PostQueryRoutine();
        
    }
}