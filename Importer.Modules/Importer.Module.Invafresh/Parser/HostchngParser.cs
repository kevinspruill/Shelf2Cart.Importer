﻿using Importer.Module.Invafresh.Enums;
using Importer.Module.Invafresh.Models;
using Importer.Module.Invafresh.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Importer.Common.Helpers;
using System.Threading;
using System.Diagnostics;
using Importer.Common.Models;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;
using Importer.Common.Interfaces;
using Importer.Common.Modifiers;

namespace Importer.Module.Invafresh.Parser
{
    public class HostchngParser
    {
        private readonly char _fieldDelimiter = (char)253;
        private readonly Dictionary<string, CommandCode> _commandCodeMap;
        private bool _legacyNutritionEnabled;
        private ICustomerProcess _customerProcess { get; set; }
        InvafreshSettingsLoader Settings = new InvafreshSettingsLoader();


        public List<PluItemRecord> PLURecords { get; private set; } = new List<PluItemRecord>();
        public List<PluItemRecord> DeletedPLURecords { get; private set; } = new List<PluItemRecord>();
        public List<IngredientItemRecord> IngredientRecords { get; private set; } = new List<IngredientItemRecord>();
        public List<NutritionItemRecord> NutritionRecords { get; private set; } = new List<NutritionItemRecord>();
        public List<LegacyNutritionItemRecord> LegacyNutritionRecords { get; private set; } = new List<LegacyNutritionItemRecord>();
        private tblProducts ProductTemplate { get; set; } = new tblProducts();
        public HostchngParser(tblProducts productTemplate, ICustomerProcess customerProcess = null)
        {
            ProductTemplate = productTemplate;
            _customerProcess = customerProcess ?? new BaseProcess();
            _commandCodeMap = InitializeCommandCodeMap();
            _legacyNutritionEnabled = Settings.UseLegacyNutritionFormat;
        }
        public bool Flush { get; set; }
        private Dictionary<string, CommandCode> InitializeCommandCodeMap()
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

            // Create and start a Stopwatch
            Stopwatch parsetimer = new Stopwatch();
            parsetimer.Start();

            using (var reader = new StreamReader(filePath, Encoding.Default))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var record = ParseLine(line);
                    if (record != null)
                    {
                        if (_customerProcess != null && _customerProcess.Name != "Importer Base Processor")
                        {
                            // Apply any data conditioning, based on the customer process
                            record = _customerProcess.DataFileCondtioning(record);
                        }

                        records.Add(record);
                        if (record is BatchHeaderRecord batchHeader)
                        {
                            Logger.Trace($"Batch Header: Name={batchHeader.BatchName}, Number={batchHeader.BatchNumber}, CreationDate={batchHeader.BatchCreationDate}, CreationTime={batchHeader.BatchCreationTime}, ItemsInBatch={batchHeader.NumberOfItemsInBatch}, DateToApplyBatch={batchHeader.DateToApplyBatch}, TimeToApplyBatch={batchHeader.TimeToApplyBatch}, BatchType={batchHeader.BatchType}, StoreNumber={batchHeader.StoreNumber}, QueueFlag={batchHeader.QueueFlag}");
                        }
                        else if (record is PluItemRecord pluItem)
                        {
                            // if command code is SPID, This is a PLU delete record
                            if (pluItem.CommandCode == CommandCode.SPID)
                            {
                                if (!Flush)
                                {
                                    DeletedPLURecords.Add(pluItem);
                                    Logger.Trace($"Deleted PLU Record: DepartmentNumber={pluItem.DepartmentNumber}, PluNumber={pluItem.PluNumber}");

                                }
                            }
                            // if command code is SPFE, This is a delete all record
                            else if (record.CommandCode == CommandCode.SPFE)
                            {
                                Logger.Trace($"Delete All PLU Record: CommandCode={pluItem.CommandCode}");
                                Flush = true;
                            }
                            // Else this is an Add or Change PLU record
                            else
                            {
                                Logger.Trace($"PLU Item Record: CommandCode={pluItem.CommandCode}, DepartmentNumber={pluItem.DepartmentNumber}, PluNumber={pluItem.PluNumber}, UpcCode={pluItem.UpcCode}, DescriptionLine1={pluItem.DescriptionLine1}, DescriptionLine2={pluItem.DescriptionLine2}");
                                PLURecords.Add(pluItem);
                            }
                        }
                        else if (record is IngredientItemRecord ingredientItem)
                        {
                            Logger.Trace($"Ingredient Item Record: CommandCode={ingredientItem.CommandCode}, DepartmentNumber={ingredientItem.DepartmentNumber}, PluNumber={ingredientItem.PluNumber}, IngredientNumber={ingredientItem.IngredientNumber}");
                            IngredientRecords.Add(ingredientItem);
                        }
                        else if (record is NutritionItemRecord nutritionItem)
                        {
                            NutritionRecords.Add(nutritionItem);
                            Logger.Trace($"Nutrition Item Record: CommandCode={nutritionItem.CommandCode}, DepartmentNumber={nutritionItem.DepartmentNumber}, NutritionNumber={nutritionItem.NutritionNumber}");
                            
                        }
                        else if (record is LegacyNutritionItemRecord legacyNutritionItem)
                        {
                            LegacyNutritionRecords.Add(legacyNutritionItem);
                            Logger.Trace($"Legacy Nutrition Item Record: CommandCode={legacyNutritionItem.CommandCode}, DepartmentNumber={legacyNutritionItem.DepartmentNumber}, NutritionNumber={legacyNutritionItem.NutritionNumber}");
                            
                        }
                        else if (record is BaseRecord baseRecord)
                        {
                            Logger.Trace($"Base Record: CommandCode={baseRecord.CommandCode}");
                        }
                        else
                        {
                            Logger.Warn($"Unknown record type: {record.GetType()}");
                        }
                    }
                }
            }

            parsetimer.Stop();
            Logger.Trace($"Parsed {records.Count} records in {parsetimer.ElapsedMilliseconds / 1000.0} seconds.");

            Logger.Trace($"PLU Records: {PLURecords.Count}");
            Logger.Trace($"Ingredient Records: {IngredientRecords.Count}");
            if (_legacyNutritionEnabled)
                Logger.Trace($"Nutrition Records: {LegacyNutritionRecords.Count}");
            else
                Logger.Trace($"Nutrition Records: {NutritionRecords.Count}");

            ConvertPLURecordsToTblProducts();

            return records;

            
        }
        private BaseRecord ParseLine(string line)
        {
            // Split the line into fields using the field delimiter
            var fields = line.Split(_fieldDelimiter);

            // Dictionary to hold all fields
            var fieldDict = new Dictionary<string, string>();

            // check fields for legacy nutrition format, It will contain NENN, NEV, NEP fields
            if (_legacyNutritionEnabled && (fields.Any(f => f.Contains("SNIA") || f.Contains("SNIC"))))
            {
                return CreateLegacyNutritionItemRecord(CommandCode.SNIA, line);
            }

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
            switch (commandCode)
            {
                case CommandCode.SPIA:
                case CommandCode.SPIC:
                case CommandCode.SPPC:
                    return CreatePluItemRecord(commandCode, fields);
                case CommandCode.SPFE:
                    Flush = true;
                    return CreatePluDeleteRecord(commandCode, fields);
                case CommandCode.SPID:                
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
        private PluItemRecord CreatePluDeleteRecord(CommandCode commandCode, Dictionary<string, string> fields)
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

            
            if (!_legacyNutritionEnabled)
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

            return record;
        }
        private LegacyNutritionItemRecord CreateLegacyNutritionItemRecord(CommandCode commandCode, string line)
        {
            var record = new LegacyNutritionItemRecord
            {
                CommandCode = commandCode
            };

            // First extract the basic fields
            var basicFields = new Dictionary<string, string>();
            var fieldParts = line.Split(_fieldDelimiter);

            foreach (var field in fieldParts)
            {
                if (field.Length >= 3)
                {
                    var fieldId = field.Substring(0, 3);
                    var fieldValue = field.Length > 3 ? field.Substring(3) : string.Empty;
                    basicFields[fieldId] = fieldValue;
                }
            }

            // Set basic fields
            record.DepartmentNumber = FieldTagHelper.GetTagValue<int>(basicFields, "DNO", 0);
            record.NutritionNumber = FieldTagHelper.GetTagValue<int>(basicFields, "NTN", 0);
            record.LabelFormatNumber = FieldTagHelper.GetTagValue<int?>(basicFields, "LF1");
            record.ServingsPerContainer = FieldTagHelper.GetTagValue<string>(basicFields, "SPC");
            record.ServingSizeDescription = FieldTagHelper.GetTagValue<string>(basicFields, "SSZ");

            // Now parse nutrition entries in sequence
            var entries = new List<NutritionEntry>();
            NutritionEntry currentEntry = null;

            foreach (var field in fieldParts)
            {
                if (field.Length < 3) continue;

                var fieldId = field.Substring(0, 3);
                var fieldValue = field.Length > 3 ? field.Substring(3) : string.Empty;

                // Handle nutrition type entries in sequence
                if (fieldId == "NEN") // NENN field
                {
                    currentEntry = new NutritionEntry { NutritionType = fieldValue };
                    entries.Add(currentEntry);
                }
                else if (fieldId == "NEV" && currentEntry != null) // NEV field
                {
                    if (int.TryParse(fieldValue, out var val))
                        currentEntry.Value = val;
                }
                else if (fieldId == "NEP" && currentEntry != null) // NEP field
                {
                    if (int.TryParse(fieldValue, out var pct))
                        currentEntry.PercentageValue = pct;
                }
            }

            record.NutritionEntries = entries;
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
        public List<tblProducts> ConvertPLURecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in PLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                products.Add(product);
            }
            return products;
        }
        public List<tblProducts> ConvertPLUDeleteRecordsToTblProducts()
        {
            var products = new List<tblProducts>();
            foreach (var pluItem in DeletedPLURecords)
            {
                var product = ConvertPLURecordToTblproducts(pluItem);
                products.Add(product);
            }
            return products;
        }
        private tblProducts ConvertPLURecordToTblproducts(PluItemRecord pluItem)
        {
            var product = ProductTemplate.Clone();

            // Get matching INO from IngredientRecords
            var ingredientRecord = IngredientRecords.FirstOrDefault(i => i.DepartmentNumber == pluItem.DepartmentNumber && i.IngredientNumber == pluItem.IngredientNumber);

            // Get matching NTN from NutritionRecords
            var nutritionRecord = NutritionRecords.FirstOrDefault(n => n.DepartmentNumber == pluItem.DepartmentNumber && n.NutritionNumber == pluItem.NutritionNumber);

            // Get matching NTN from LegacyNutritionRecords
            var legacyNutritionRecord = LegacyNutritionRecords.FirstOrDefault(n => n.DepartmentNumber == pluItem.DepartmentNumber && n.NutritionNumber == pluItem.NutritionNumber);

            // Convert the PLU item record to a tblProducts object
            product.Dept = pluItem.DepartmentNumber.ToString();
            product.PLU = pluItem.PluNumber.ToString();
            product.Description1 = pluItem.DescriptionLine1;
            product.Description2 = pluItem.DescriptionLine2;
            product.Description3 = pluItem.DescriptionLine3;
            product.Description4 = pluItem.DescriptionLine4;
            product.Price = pluItem.UnitPrice.ToPrice().ToString();
            product.NetWt = pluItem.FixedWeightAmount.ToFixedWeight().ToString();
            product.ShelfLife = pluItem.ShelfLife.ToString();
            product.IngredientNum = pluItem.IngredientNumber.ToString();
            product.NutrifactNum = pluItem.NutritionNumber.ToString();
            product.Scaleable = Converters.UMEtoScalable(pluItem.UnitOfMeasure);
            product.Tare = pluItem.WrappedTareWeight.HasValue ? pluItem.WrappedTareWeight.ToTare().ToString() : string.Empty;
            product.Description10 = pluItem.UpcCode;
            product.Description4 = pluItem.ByCountQuantity.ToString();
            product.SalePrice = pluItem.DiscountPrice.HasValue ? pluItem.DiscountPrice.ToPrice().ToString() : string.Empty;
            product.Description8 = string.IsNullOrEmpty(pluItem.GradeNumber) ? string.Empty : pluItem.GradeNumber;


            // Use CustomMapLoader to override mapping fields from PluItemRecord to tblProducts
            var mappedFields = CustomMapLoader.CustomMap;
            foreach (var field in mappedFields)
            {

                var propertyWithAttribute = typeof(tblProducts).GetProperties()
                    .FirstOrDefault(prop =>
                    {
                        var attr = prop.GetCustomAttributes(typeof(ImportDBFieldAttribute), false)
                            .Cast<ImportDBFieldAttribute>()
                            .FirstOrDefault();
                        return attr != null && attr.Name == field.Key;
                    });

                if (propertyWithAttribute != null)
                {
                    var pluItemProperty = typeof(PluItemRecord).GetProperties()
                    .FirstOrDefault(prop =>
                    {
                        var attr = prop.GetCustomAttributes(typeof(FieldTagAttribute), false)
                            .Cast<FieldTagAttribute>()
                            .FirstOrDefault();
                        return attr != null && attr.Tag == field.Value;
                    });

                    if (pluItemProperty != null)
                    {
                        // Get the value from pluItem and set it to the product
                        var value = pluItemProperty.GetValue(pluItem);
                        propertyWithAttribute.SetValue(product, value);
                    }
                }
            }

            if (ingredientRecord != null)
            {
                // Add ingredient information to tblProducts
                product.Ingredients = ingredientRecord.IngredientText;
            }

            if (nutritionRecord != null)
            {
                // Add nutrition information to tblProducts
                product.NFDesc = nutritionRecord.ServingsPerContainer;
                product.NFServingSize = nutritionRecord.ServingSizeDescription;

                // Vitamins and Minerals (Percentages)
                // TODO: Add more nutrition fields here
            }

            if (legacyNutritionRecord != null)
            {
                product.NFDesc = legacyNutritionRecord.ServingsPerContainer;
                product.NFServingSize = legacyNutritionRecord.ServingSizeDescription;

                // Vitamins and Minerals (Percentages)
                product.NFVitA = GetLegacyNutrtionValue(legacyNutritionRecord, "N001-P"); // Vitamin A Percent
                product.NFVitC = GetLegacyNutrtionValue(legacyNutritionRecord, "N002-P"); // Vitamin C Percent
                product.NF3 = GetLegacyNutrtionValue(legacyNutritionRecord, "N003-P"); // Thiamine Percent
                product.NF4 = GetLegacyNutrtionValue(legacyNutritionRecord, "N004-P"); // Riboflavin Percent
                product.NF5 = GetLegacyNutrtionValue(legacyNutritionRecord, "N005-P"); // Niacin Percent
                product.NFCalcium = GetLegacyNutrtionValue(legacyNutritionRecord, "N006-P"); // Calcium Percent
                product.NFCalciummcg = GetLegacyNutrtionValue(legacyNutritionRecord, "N006-V"); // Calcium Value
                product.NFIron = GetLegacyNutrtionValue(legacyNutritionRecord, "N007-P"); // Iron Percent
                product.NFIronmcg = GetLegacyNutrtionValue(legacyNutritionRecord, "N007-V"); // Iron Value
                product.NFVitD = GetLegacyNutrtionValue(legacyNutritionRecord, "N008-P"); // Vitamin D Percent
                product.NFVitDmcg = GetLegacyNutrtionValue(legacyNutritionRecord, "N008-V"); // Vitamin D Value
                product.NF9 = GetLegacyNutrtionValue(legacyNutritionRecord, "N010-P"); // Vitamin B6 Percent
                product.NF6 = GetLegacyNutrtionValue(legacyNutritionRecord, "N011-P"); // Folic Acid Percent
                product.NF10 = GetLegacyNutrtionValue(legacyNutritionRecord, "N012-P"); // Vitamin B12 Percent

                // Calories and Macronutrients
                product.NFCalories = GetLegacyNutrtionValue(legacyNutritionRecord, "N100-V"); // Calories Value
                product.NFCaloriesFromFat = GetLegacyNutrtionValue(legacyNutritionRecord, "N101-V"); // Calories From Fat Value

                // Fats
                product.NFTotalFat = GetLegacyNutrtionValue(legacyNutritionRecord, "N102-P"); // Total Fat Percent
                product.NFTotalFatG = GetLegacyNutrtionValue(legacyNutritionRecord, "N102-V"); // Total Fat Value
                product.NF1 = GetLegacyNutrtionValue(legacyNutritionRecord, "N103-P"); // Saturated Fat Percent
                product.NFSatFatG = GetLegacyNutrtionValue(legacyNutritionRecord, "N103-V"); // Saturated Fat Value
                product.NF8 = GetLegacyNutrtionValue(legacyNutritionRecord, "N113-V"); // Trans Fatty Acid Value

                // Cholesterol
                product.NFCholesterol = GetLegacyNutrtionValue(legacyNutritionRecord, "N104-P"); // Cholesterol Percent
                product.NFCholesterolMG = GetLegacyNutrtionValue(legacyNutritionRecord, "N104-V"); // Cholesterol Value

                // Carbohydrates
                product.NFTotCarbo = GetLegacyNutrtionValue(legacyNutritionRecord, "N105-P"); // Total Carbohydrates Percent
                product.NFTotCarboG = GetLegacyNutrtionValue(legacyNutritionRecord, "N105-V"); // Total Carbohydrates Value
                product.NFDietFiber = GetLegacyNutrtionValue(legacyNutritionRecord, "N106-P"); // Dietary Fiber Percent
                product.NF2 = GetLegacyNutrtionValue(legacyNutritionRecord, "N106-V"); // Dietary Fiber Value
                product.NFSugars = GetLegacyNutrtionValue(legacyNutritionRecord, "N108-V"); // Sugars Value
                product.NF7 = GetLegacyNutrtionValue(legacyNutritionRecord, "N120-V"); // Sugar Alcohol Value
                product.NFSugarsAdded = GetLegacyNutrtionValue(legacyNutritionRecord, "N127-P"); // Added Sugars Percent
                product.NFSugarsAddedG = GetLegacyNutrtionValue(legacyNutritionRecord, "N127-V"); // Added Sugars Value

                // Sodium and Potassium
                product.NFSodium = GetLegacyNutrtionValue(legacyNutritionRecord, "N107-P"); // Sodium Percent
                product.NFSodiumMG = GetLegacyNutrtionValue(legacyNutritionRecord, "N107-V"); // Sodium Value
                product.NFPotassium = GetLegacyNutrtionValue(legacyNutritionRecord, "N110-P"); // Potassium Percent
                product.NFPotassiummcg = GetLegacyNutrtionValue(legacyNutritionRecord, "N110-V"); // Potassium Value

                // Protein
                product.NFProtein = GetLegacyNutrtionValue(legacyNutritionRecord, "N109-V"); // Protein Value
            }

            // TODO: Add Legacy Nutrition Custom Remapping here (Only Implement if needed)

            return product;
        }
        private string GetLegacyNutrtionValue(LegacyNutritionItemRecord legacyNutritionRecord, string NEN)
        {
            // get the Value and PercentageValue of the first 4 characters NEN
            var LegacyNF = legacyNutritionRecord.NutritionEntries.FirstOrDefault(n => n.NutritionType == NEN.Substring(0, 4));

            // if the NEN ends with V, return the Value
            if (NEN.EndsWith("V"))
            {
                return LegacyNF?.Value.ApplyImpliedDecimals(1).ToString();
            }
            // if the NEN ends with P, return the PercentageValue
            else if (NEN.EndsWith("P"))
            {
                return LegacyNF?.PercentageValue.ToString();
            }
            return string.Empty;
        }
    }
}
