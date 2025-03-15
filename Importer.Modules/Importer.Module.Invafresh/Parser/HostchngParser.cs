using Importer.Module.Invafresh.Enums;
using Importer.Module.Invafresh.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Importer.Module.Invafresh.Parser
{
    public class HostchngParser
    {
        private readonly char _fieldDelimiter;
        private readonly Dictionary<string, CommandCode> _commandCodeMap;

        public HostchngParser(char fieldDelimiter = (char)253)
        {
            _fieldDelimiter = fieldDelimiter;
            _commandCodeMap = InitializeCommandCodeMap();
        }

        private Dictionary<string, CommandCode> InitializeCommandCodeMap()
        {
            return new Dictionary<string, CommandCode>
            {
                { "SPIA", CommandCode.SPIA },
                { "SPIC", CommandCode.SPIC },
                { "SPPC", CommandCode.SPPC },
                { "SPID", CommandCode.SPID },
                { "SPFE", CommandCode.SPFE },
                { "SIIA", CommandCode.SIIA },
                { "SIIC", CommandCode.SIIC },
                { "SIID", CommandCode.SIID },
                { "SNIA", CommandCode.SNIA },
                { "SNIC", CommandCode.SNIC },
                { "SNID", CommandCode.SNID }
            };
        }

        public List<BaseRecord> ParseFile(string filePath)
        {
            var records = new List<BaseRecord>();

            using (var rea0der = new StreamReader(filePath, Encoding.ASCII))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var record = ParseLine(line);
                    if (record != null)
                    {
                        records.Add(record);
                    }
                }
            }

            return records;
        }
        private BaseRecord ParseLine(string line)
        {
            // Split the line into fields using the field delimiter
            var fields = line.Split(_fieldDelimiter);

            // Dictionary to hold all fields
            var fieldDict = new Dictionary<string, string>();

            // Process each field
            foreach (var field in fields)
            {
                if (field.Length >= 3)
                {
                    var fieldId = field.Substring(0, 3);
                    var fieldValue = field.Length > 3 ? field.Substring(3) : string.Empty;
                    fieldDict[fieldId] = fieldValue;
                }
            }

            // Determine record type based on command code
            if (fieldDict.TryGetValue("CCO", out var commandCodeStr) &&
                _commandCodeMap.TryGetValue(commandCodeStr, out var commandCode))
            {
                return CreateRecordFromFields(commandCode, fieldDict);
            }
            else if (fieldDict.ContainsKey("BNA")) // This is a batch header
            {
                return CreateBatchHeaderFromFields(fieldDict);
            }

            return null;
        }

        private BaseRecord CreateRecordFromFields(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var addOrUpdateCommands = new List<CommandCode>
            {
                CommandCode.SPIA, // Send PLU Item Add
                CommandCode.SPIC, // Send PLU Item Change
                CommandCode.SPPC, // Send PLU Price Change
                CommandCode.SIIA, // Send Ingredient Item Add
                CommandCode.SIIC, // Send Ingredient Item Change
                CommandCode.SNIA, // Send Nutrition Item Add
                CommandCode.SNIC  // Send Nutrition Item Change
            };

            var deleteCommands = new List<CommandCode>
            {
                CommandCode.SPID, // Send PLU Item Delete
                CommandCode.SPFE, // Delete All Scale PLU Items
                CommandCode.SIID, // Send Ingredient Item Delete 
                CommandCode.SNID  // Send Nutrition Item Delete
            };

            switch (commandCode)
            {
                case CommandCode.SPIA:
                case CommandCode.SPIC:
                case CommandCode.SPPC:
                    return CreatePluItemRecord(commandCode, fields);
                case CommandCode.SPID:
                case CommandCode.SPFE:
                    return CreatePluDeleteRecord(commandCode, fields);
                case CommandCode.SIIA:
                case CommandCode.SIIC:
                    return CreateIngredientItemRecord(commandCode, fields);
                case CommandCode.SIID:
                    return CreateIngredientDeleteRecord(commandCode, fields);
                case CommandCode.SNIA:
                case CommandCode.SNIC:
                    return CreateNutritionItemRecord(commandCode, fields);
                case CommandCode.SNID:
                    return CreateNutritionDeleteRecord(commandCode, fields);
                default:
                    return null;
            }
        }
        private BatchHeaderRecord CreateBatchHeaderFromFields(Dictionary<string, string> fields)
        {
            var record = new BatchHeaderRecord
            {
                CommandCode = CommandCode.BATCH_HEADER
            };

            // Parse batch header fields
            if (fields.TryGetValue("BNA", out var bna)) record.BatchName = bna;
            if (fields.TryGetValue("BNO", out var bno) && int.TryParse(bno, out var bnoVal)) record.BatchNumber = bnoVal;

            // Parse dates
            if (fields.TryGetValue("BDA", out var bda) && DateTime.TryParse(bda, out var bdaVal))
                record.BatchCreationDate = bdaVal;

            if (fields.TryGetValue("BTI", out var bti)) record.BatchCreationTime = bti;

            if (fields.TryGetValue("ICO", out var ico) && int.TryParse(ico, out var icoVal))
                record.NumberOfItemsInBatch = icoVal;

            // Parse effective date and time
            if (fields.TryGetValue("EFD", out var efd) && DateTime.TryParse(efd, out var efdVal))
                record.DateToApplyBatch = efdVal;

            if (fields.TryGetValue("EFT", out var eft)) record.TimeToApplyBatch = eft;

            // Parse batch type
            if (fields.TryGetValue("BTY", out var bty) && bty.Length == 1)
            {
                switch (bty)
                {
                    case "S": record.BatchType = BatchType.S; break;
                    case "H": record.BatchType = BatchType.H; break;
                    case "O": record.BatchType = BatchType.O; break;
                    case "E": record.BatchType = BatchType.E; break;
                }
            }

            // Parse store number
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            // Parse queue flag
            if (fields.TryGetValue("FLG", out var flg) && int.TryParse(flg, out var flgVal) && flgVal >= 0 && flgVal <= 2)
                record.QueueFlag = flgVal;

            return record;
        }
        private PluItemRecord CreatePluItemRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new PluItemRecord
            {
                CommandCode = commandCode
            };

            // Parse required fields
            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("PNO", out var pno) && int.TryParse(pno, out var pnoVal))
                record.PluNumber = pnoVal;

            if (fields.TryGetValue("UPC", out var upc))
                record.UpcCode = upc;

            // Parse description fields
            if (fields.TryGetValue("DN1", out var dn1))
                record.DescriptionLine1 = dn1;

            if (fields.TryGetValue("DS1", out var ds1) && int.TryParse(ds1, out var ds1Val))
                record.DescriptionSize1 = ds1Val;

            if (fields.TryGetValue("DN2", out var dn2))
                record.DescriptionLine2 = dn2;

            if (fields.TryGetValue("DS2", out var ds2) && int.TryParse(ds2, out var ds2Val))
                record.DescriptionSize2 = ds2Val;

            if (fields.TryGetValue("DN3", out var dn3))
                record.DescriptionLine3 = dn3;

            if (fields.TryGetValue("DS3", out var ds3) && int.TryParse(ds3, out var ds3Val))
                record.DescriptionSize3 = ds3Val;

            if (fields.TryGetValue("DN4", out var dn4))
                record.DescriptionLine4 = dn4;

            if (fields.TryGetValue("DS4", out var ds4) && int.TryParse(ds4, out var ds4Val))
                record.DescriptionSize4 = ds4Val;

            // Parse price and weight fields
            if (fields.TryGetValue("UPR", out var upr) && int.TryParse(upr, out var uprVal))
                record.UnitPrice = uprVal;

            if (fields.TryGetValue("FWT", out var fwt) && int.TryParse(fwt, out var fwtVal))
                record.FixedWeightAmount = fwtVal;

            // Parse unit of measure
            if (fields.TryGetValue("UME", out var ume) && Enum.TryParse(ume, out UnitOfMeasure umeVal))
                record.UnitOfMeasure = umeVal;

            // Parse tare fields
            if (fields.TryGetValue("BCO", out var bco) && int.TryParse(bco, out var bcoVal))
                record.ByCountQuantity = bcoVal;

            if (fields.TryGetValue("WTA", out var wta) && int.TryParse(wta, out var wtaVal))
                record.WrappedTareWeight = wtaVal;

            if (fields.TryGetValue("UTA", out var uta) && int.TryParse(uta, out var utaVal))
                record.UnwrappedTareWeight = utaVal;

            // Parse shelf life fields
            if (fields.TryGetValue("SLI", out var sli) && int.TryParse(sli, out var sliVal))
                record.ShelfLife = sliVal;

            if (fields.TryGetValue("SLT", out var slt) && int.TryParse(slt, out var sltVal) && Enum.IsDefined(typeof(ShelfLifeType), sltVal))
                record.ShelfLifeType = (ShelfLifeType)sltVal;

            if (fields.TryGetValue("EBY", out var eby) && int.TryParse(eby, out var ebyVal))
                record.UseBy = ebyVal;

            // Parse class fields
            if (fields.TryGetValue("CCL", out var ccl) && int.TryParse(ccl, out var cclVal))
                record.CommodityClass = cclVal;

            // Parse logo and graphic fields
            if (fields.TryGetValue("LNU", out var lnu))
                record.LogoNumbers = lnu;

            if (fields.TryGetValue("GNO", out var gno))
                record.GraphicNumbers = gno;

            // Parse related item numbers
            if (fields.TryGetValue("INO", out var ino) && int.TryParse(ino, out var inoVal))
                record.IngredientNumber = inoVal;

            if (fields.TryGetValue("NTN", out var ntn) && int.TryParse(ntn, out var ntnVal))
                record.NutritionNumber = ntnVal;

            if (fields.TryGetValue("ALG", out var alg) && int.TryParse(alg, out var algVal))
                record.AllergenNumber = algVal;

            if (fields.TryGetValue("U1N", out var u1n) && int.TryParse(u1n, out var u1nVal))
                record.UserDefinedText1Number = u1nVal;

            if (fields.TryGetValue("U2N", out var u2n) && int.TryParse(u2n, out var u2nVal))
                record.UserDefinedText2Number = u2nVal;

            // Parse flags
            if (fields.TryGetValue("FTA", out var fta))
                record.ForcedTare = fta;

            // Parse label format fields
            if (fields.TryGetValue("LF1", out var lf1) && int.TryParse(lf1, out var lf1Val))
                record.LabelFormatNumberOne = lf1Val;

            if (fields.TryGetValue("LF2", out var lf2) && int.TryParse(lf2, out var lf2Val))
                record.LabelFormatNumberTwo = lf2Val;

            // Parse frequent shopper fields
            if (fields.TryGetValue("FR1", out var fr1) && int.TryParse(fr1, out var fr1Val))
                record.DiscountPrice = fr1Val;

            if (fields.TryGetValue("FDT", out var fdt) && int.TryParse(fdt, out var fdtVal) &&
                Enum.IsDefined(typeof(FrequentShopperDiscountType), fdtVal))
                record.DiscountMethod = (FrequentShopperDiscountType)fdtVal;

            if (fields.TryGetValue("FSM", out var fsm) && int.TryParse(fsm, out var fsmVal))
                record.ByCountQuantityFrequentShopper = fsmVal;

            if (fields.TryGetValue("FSX", out var fsx) && int.TryParse(fsx, out var fsxVal))
                record.ByCountItemExceptionPrice = fsxVal;

            if (fields.TryGetValue("PTA", out var pta) && int.TryParse(pta, out var ptaVal))
                record.PercentageTareWeight = ptaVal;

            if (fields.TryGetValue("FSL", out var fsl))
                record.ForceShelfLifeEntry = fsl;

            if (fields.TryGetValue("FUB", out var fub))
                record.ForceUseByEntry = fub;

            // Parse COOL (Country of Origin Labeling) fields
            if (fields.TryGetValue("CNO", out var cno) && int.TryParse(cno, out var cnoVal))
                record.CoolTextNumber = cnoVal;

            if (fields.TryGetValue("CCN", out var ccn) && int.TryParse(ccn, out var ccnVal))
                record.CoolClassNumber = ccnVal;

            if (fields.TryGetValue("CSN", out var csn) && int.TryParse(csn, out var csnVal))
                record.ShortListNumber = csnVal;

            if (fields.TryGetValue("SQN", out var sqn) && int.TryParse(sqn, out var sqnVal))
                record.SequenceNumber = sqnVal;

            if (fields.TryGetValue("CUN", out var cun) && int.TryParse(cun, out var cunVal))
                record.MostRecentlyUsedCoolNumber = cunVal;

            if (fields.TryGetValue("CRN", out var crn))
                record.CoolTrackingNumber = crn;

            if (fields.TryGetValue("CFX", out var cfx))
                record.CoolPromptFlag = cfx;

            if (fields.TryGetValue("CXN", out var cxn) && int.TryParse(cxn, out var cxnVal))
                record.CoolPreTextNumber = cxnVal;

            return record;
        }
        private BaseRecord CreatePluDeleteRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new PluItemRecord
            {
                CommandCode = commandCode
            };

            // For delete records, we only need the key fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("PNO", out var pno) && int.TryParse(pno, out var pnoVal))
                record.PluNumber = pnoVal;

            if (fields.TryGetValue("UPC", out var upc))
                record.UpcCode = upc;

            return record;
        }
        private IngredientItemRecord CreateIngredientItemRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new IngredientItemRecord
            {
                CommandCode = commandCode
            };

            // Parse required fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("PNO", out var pno) && int.TryParse(pno, out var pnoVal))
                record.PluNumber = pnoVal;

            if (fields.TryGetValue("INO", out var ino) && int.TryParse(ino, out var inoVal))
                record.IngredientNumber = inoVal;

            if (fields.TryGetValue("ITE", out var ite))
                record.IngredientText = ite;

            // Parse optional fields
            if (fields.TryGetValue("IS1", out var is1) && int.TryParse(is1, out var is1Val))
                record.IngredientTextFontSize = is1Val;

            if (fields.TryGetValue("LNK", out var lnk))
                record.LinkedIngredientNumbers = lnk;

            if (fields.TryGetValue("MOD", out var mod))
                record.ModifiedFlag = mod;

            return record;
        }
        private BaseRecord CreateIngredientDeleteRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new IngredientItemRecord
            {
                CommandCode = commandCode
            };

            // For delete records, we only need the key fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("INO", out var ino) && int.TryParse(ino, out var inoVal))
                record.IngredientNumber = inoVal;

            return record;
        }
        private BaseRecord CreateNutritionItemRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new NutritionItemRecord
            {
                CommandCode = commandCode
            };

            // Parse required fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("NTN", out var ntn) && int.TryParse(ntn, out var ntnVal))
                record.NutritionNumber = ntnVal;

            // Parse optional fields
            if (fields.TryGetValue("LF1", out var lf1) && int.TryParse(lf1, out var lf1Val))
                record.LabelFormatNumber = lf1Val;

            if (fields.TryGetValue("SPC", out var spc))
                record.ServingsPerContainer = spc;

            if (fields.TryGetValue("SSZ", out var ssz))
                record.ServingSizeDescription = ssz;

            // Parse nutrition values
            // Check if we're using the current nutrition format
            bool isCurrentFormat = false;
            foreach (var key in fields.Keys)
            {
                if (key.Length == 3 && record.NutritionValues.ContainsKey(key))
                {
                    isCurrentFormat = true;
                    break;
                }
            }

            if (isCurrentFormat)
            {
                // Process current nutrition format (with comma-separated values)
                foreach (var pair in fields)
                {
                    if (record.NutritionValues.ContainsKey(pair.Key))
                    {
                        var valueParts = pair.Value.Split(',');
                        if (valueParts.Length == 2)
                        {
                            int? value = null;
                            int? percentage = null;

                            if (int.TryParse(valueParts[0], out var val))
                                value = val;

                            if (int.TryParse(valueParts[1], out var pct))
                                percentage = pct;

                            record.NutritionValues[pair.Key] = new Tuple<int?, int?>(value, percentage);
                        }
                    }
                }
            }
            else
            {
                // Try to detect if it's legacy format, if so, we should create a LegacyNutritionItemRecord instead
                bool isLegacyFormat = false;
                foreach (var key in fields.Keys)
                {
                    if (key == "NENN" || key == "NEV" || key == "NEP")
                    {
                        isLegacyFormat = true;
                        break;
                    }
                }

                if (isLegacyFormat)
                {
                    return CreateLegacyNutritionItemRecord(commandCode, fields);
                }
            }

            return record;
        }
        private LegacyNutritionItemRecord CreateLegacyNutritionItemRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new LegacyNutritionItemRecord
            {
                CommandCode = commandCode
            };

            // Parse required fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("NTN", out var ntn) && int.TryParse(ntn, out var ntnVal))
                record.NutritionNumber = ntnVal;

            // Parse optional fields
            if (fields.TryGetValue("LF1", out var lf1) && int.TryParse(lf1, out var lf1Val))
                record.LabelFormatNumber = lf1Val;

            if (fields.TryGetValue("SPC", out var spc))
                record.ServingsPerContainer = spc;

            if (fields.TryGetValue("SSZ", out var ssz))
                record.ServingSizeDescription = ssz;

            // Group the NENN, NEV, NEP fields and create nutrition entries
            // First, collect all entries by their index (presumed to be sequential)
            Dictionary<int, NutritionEntry> entries = new Dictionary<int, NutritionEntry>();

            foreach (var pair in fields)
            {
                if (pair.Key.StartsWith("NENN"))
                {
                    // Extract index and create the entry if it doesn't exist
                    if (int.TryParse(pair.Key.Substring(4), out var index))
                    {
                        if (!entries.ContainsKey(index))
                            entries[index] = new NutritionEntry();

                        entries[index].NutritionType = pair.Value;
                    }
                }
                else if (pair.Key.StartsWith("NEV"))
                {
                    if (int.TryParse(pair.Key.Substring(3), out var index) &&
                        int.TryParse(pair.Value, out var val))
                    {
                        if (!entries.ContainsKey(index))
                            entries[index] = new NutritionEntry();

                        entries[index].Value = val;
                    }
                }
                else if (pair.Key.StartsWith("NEP"))
                {
                    if (int.TryParse(pair.Key.Substring(3), out var index) &&
                        int.TryParse(pair.Value, out var val))
                    {
                        if (!entries.ContainsKey(index))
                            entries[index] = new NutritionEntry();

                        entries[index].PercentageValue = val;
                    }
                }
            }

            // Add all entries to the record
            foreach (var entry in entries.Values)
            {
                record.NutritionEntries.Add(entry);
            }

            return record;
        }
        private BaseRecord CreateNutritionDeleteRecord(CommandCode commandCode, Dictionary<string, string> fields)
        {
            var record = new NutritionItemRecord
            {
                CommandCode = commandCode
            };

            // For delete records, we only need the key fields
            if (fields.TryGetValue("SNO", out var sno) && int.TryParse(sno, out var snoVal))
                record.StoreNumber = snoVal;

            if (fields.TryGetValue("DNO", out var dno) && int.TryParse(dno, out var dnoVal))
                record.DepartmentNumber = dnoVal;

            if (fields.TryGetValue("NTN", out var ntn) && int.TryParse(ntn, out var ntnVal))
                record.NutritionNumber = ntnVal;

            return record;
        }
    }
}
