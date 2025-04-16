using Importer.Common.Models;
using System.Collections.Generic;

namespace Importer.Module.Generic.Interfaces
{
    public interface IParser
    {
        List<Dictionary<string, string>> DeletedPLURecords { get; }
        List<Dictionary<string, string>> PLURecords { get; }

        List<tblProducts> ConvertPLUDeleteRecordsToTblProducts();
        List<tblProducts> ConvertPLURecordsToTblProducts();
        void ParseFile(string filePath);
    }
}