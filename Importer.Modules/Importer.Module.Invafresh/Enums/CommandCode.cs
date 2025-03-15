using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Enums
{
    public enum CommandCode
    {
        // Batch Header
        BATCH_HEADER,

        // PLU Item Commands
        SPIA, // Send PLU Item Add
        SPIC, // Send PLU Item Change
        SPPC, // Send PLU Price Change
        SPID, // Send PLU Item Delete
        SPFE, // Delete All Scale PLU Items

        // Ingredient Item Commands
        SIIA, // Send Ingredient Item Add
        SIIC, // Send Ingredient Item Change
        SIID, // Send Ingredient Item Delete

        // Nutrition Item Commands
        SNIA, // Send Nutrition Item Add
        SNIC, // Send Nutrition Item Change
        SNID  // Send Nutrition Item Delete
    }
}
