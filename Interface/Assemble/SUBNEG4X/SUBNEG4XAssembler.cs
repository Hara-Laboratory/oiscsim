using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class SUBNEG4XAssembler : AssemblerBase
    {
        bool IsSixteenArch = false;
        public SUBNEG4XAssembler(bool isSixteen = false)
        {
            this.IsSixteenArch = isSixteen;
        }

        public static AssemblerBase Instansinate()
        {
            return new SUBNEG4XAssembler();
        }

        public static AssemblerBase InstansinateBySixteen()
        {
            return new SUBNEG4XAssembler(true);
        }

        public override bool Assemble(AssemblyCode code, out Execute.ExecuteSetupData res, List<AssembleError> errorList)
        {
            res = new Execute.ExecuteSetupData(1);

            //Search section marked as startup
            MessageManager.ShowLine($"Determining entry point...", enumMessageLevel.DetailProgressLog);
            Section startupSection = null;
            foreach (var sect in code.Sections)
            {
                if ((sect.Attributes & Section.enumAttribute.Startup) == 0)
                    continue;

                if (startupSection != null)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assembler",
                        Detail = $"More than 2 sections are marked as startup sections. \"{ startupSection.Name }\" and \"{ sect.Name }\" are so.",
                        Position = sect.AssemblePosition
                    });
                    return false;
                }

                startupSection = sect;
            }

            //Assign addresses
            MessageManager.ShowLine($"Assigning memory address...", enumMessageLevel.DetailProgressLog);
            uint ramWordSize;
            if (!AssignAddresses(code, startupSection, errorList, out ramWordSize))
                return false;

            //Allocate memory region on slot.0
            MessageManager.ShowLine($"Allocating memory regions...", enumMessageLevel.DetailProgressLog);
            res.MemoryContents[0].ExpandCapacity((int)ramWordSize);

            //Garantee minimum of memory size
            if (ramWordSize < code.MinMemorySize)
            {
                res.MemoryContents[0].ExpandCapacityForStack(code.MinMemorySize - (int)ramWordSize);
            }

            //Generate contents
            MessageManager.ShowLine($"Generating memory initial contents...", enumMessageLevel.DetailProgressLog);
            if (!GenerateMemoryContents(code, res, errorList))
                return false;

            res.StartupAddress = 0;
            res.IsSixteenBitArch = IsSixteenArch;

            return true;
        }

        private bool AssignAddresses(AssemblyCode code, Section startupSection, List<AssembleError> errorList, out uint ramBottomWordAddress)
        {
            ramBottomWordAddress = 0;

            //Assign address of instructions
            {
                if (startupSection != null)
                { //From section marked as startup
                    AssignAddressForSectionInstruction(startupSection, ref ramBottomWordAddress);
                }
                for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
                {
                    Section sect = code.Sections[sectIdx];

                    if (sect == startupSection)
                        continue;

                    AssignAddressForSectionInstruction(sect, ref ramBottomWordAddress);
                }
            }

            //Assign address of variables
            {
                //Read-only variables
                foreach (var e in code.VariableAnalyzeResult.Readonlys)
                {
                    AddressSymbolInfo placeInfo = new Interface.Assemble.AddressSymbolInfo()
                    {
                        MemoryNumber = 0,
                        Address = new Misc.AddressRange()
                        {
                            From = ramBottomWordAddress,
                            To = ramBottomWordAddress
                        }
                    };
                    ramBottomWordAddress += 1;

                    e.PlacedInfo = placeInfo;
                }

                //Read-write variables
                foreach (var e in code.VariableAnalyzeResult.Readwrites)
                {
                    AddressSymbolInfo placeInfo = new Interface.Assemble.AddressSymbolInfo()
                    {
                        MemoryNumber = 0,
                        Address = new Misc.AddressRange()
                        {
                            From = ramBottomWordAddress,
                            To = ramBottomWordAddress
                        }
                    };
                    ramBottomWordAddress += 1;

                    e.PlacedInfo = placeInfo;
                }
            }

            return true;
        }

        private void AssignAddressForSectionInstruction(Section sect, ref uint ramBottomWordAddress)
        {
            for (int instrIdx = 0; instrIdx < sect.AllInstructions.Length; instrIdx++)
            {
                Instruction instr = sect.AllInstructions[instrIdx];

                instr.PlacedInfo.MemoryNumber = 0;
                instr.PlacedInfo.Address = new Misc.AddressRange()
                {
                    From = ramBottomWordAddress,
                    To = ramBottomWordAddress + 3
                };

                for (int oprIdx = 0; oprIdx < instr.Operands.Length; oprIdx++)
                {
                    instr.Operands[oprIdx].PlacedInfo.MemoryNumber = 0;
                    instr.Operands[oprIdx].PlacedInfo.Address = new Misc.AddressRange()
                    {
                        From = (uint)(ramBottomWordAddress + (uint)oprIdx),
                        To = (uint)(ramBottomWordAddress + (uint)oprIdx)
                    };
                }

                ramBottomWordAddress += 4;
            }
        }

        private bool GenerateMemoryContents(AssemblyCode code, Execute.ExecuteSetupData setupData, List<AssembleError> errorList)
        {
            //Generate memory contents of instructions
            {
                for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
                {
                    Section sect = code.Sections[sectIdx];

                    for (int instrIdx = 0; instrIdx < sect.AllInstructions.Length; instrIdx++)
                    {
                        Instruction instr = sect.AllInstructions[instrIdx];

                        if (!GenerateMemoryContentForInstruction(instr, setupData, errorList))
                            return false;
                    }
                }
            }


            bool failed = false;
            int[] notifier = new int[2] { 0, 1 };
            System.Threading.Tasks.Task genThread = new System.Threading.Tasks.Task(() =>
            {
                notifier[1] = code.VariableAnalyzeResult.Readonlys.Count + code.VariableAnalyzeResult.Readwrites.Count;
                //Assign address of variables
                {
                    //Read-only variables
                    for (int i = 0; i < code.VariableAnalyzeResult.Readonlys.Count; i++)
                    {
                        if (!GenerateMemoryContentForVariable(code.VariableAnalyzeResult.Readonlys[i], setupData, errorList))
                        {
                            failed = true;
                            return;
                        }
                        notifier[0] = i;
                    }

                    //Read-write variables
                    for (int i = 0; i < code.VariableAnalyzeResult.Readwrites.Count; i++)
                    {
                        if (!GenerateMemoryContentForVariable(code.VariableAnalyzeResult.Readwrites[i], setupData, errorList))
                        {
                            failed = true;
                            return;
                        }
                        notifier[0] = i + code.VariableAnalyzeResult.Readonlys.Count;
                    }
                }
            });

            genThread.Start();
            MessageManager.ShowLine("");
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            while (!genThread.Wait(333))
            {
                Console.Write($"\r        Processing { notifier[0] } / { notifier[1] }...");
            }
            if (failed)
                return false;

            return true;

        }

        private bool GenerateMemoryContentForInstruction(Instruction instr, Execute.ExecuteSetupData setupData, List<AssembleError> errorList)
        {
            //Opcode
            byte opcode = 0;
            switch (instr.Nimonic.ToUpper())
            {
                case "SNG4":
                    opcode = 0;
                    break;
                case "SNG4X":
                    opcode = 1;
                    break;
                default:
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "SUBNEG4X Assemble",
                        Detail = $"Invalid mnemonic '{ instr.Nimonic }'",
                        Position = instr.AssemblePosition
                    });
                    return false;
            }

            //Determine operands
            uint operandA, operandB, operandC, operandJ;
            if (instr.Operands.Length == 4)
            { //Full description
                operandA = instr.Operands[0].Immediate.GetValue();
                operandB = instr.Operands[1].Immediate.GetValue();
                operandC = instr.Operands[2].Immediate.GetValue();
                operandJ = instr.Operands[3].Immediate.GetValue();
            }
            else if (instr.Operands.Length == 3)
            { //Skipped jump target
                operandA = instr.Operands[0].Immediate.GetValue();
                operandB = instr.Operands[1].Immediate.GetValue();
                operandC = instr.Operands[2].Immediate.GetValue();
                operandJ = instr.PlacedInfo.Address.To + 1;
            }
            else
            { //Invalid ?
                errorList.Add(new Interface.Assemble.AssembleError()
                {
                    Title = "SUBNEG4X Assemble",
                    Detail = $"Invalid operand format",
                    Position = instr.AssemblePosition
                });
                return false;
            }
            uint opcodeOnValue = IsSixteenArch ? 0x00008000 : 0x80000000;
            operandJ = (operandJ & 0x7FFFFFFF) | (opcode != 0 ? opcodeOnValue : 0x00000000);

            //Genarate
            setupData.MemoryContents[instr.PlacedInfo.MemoryNumber][(int)instr.PlacedInfo.Address.From + 0] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
            {
                InitialValue = operandA,
                DebugInfoCount = 1,
                DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                {
                    Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                    {
                        Usage = Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.Instruction,
                        IsDebugMarked = (instr.DebugText.Length > 0),
                        DebugDisplayText = instr.DebugText,
                        DebugInfo = instr.ToString().Replace("\r\n", " ").Replace("\t", "  ") + $" ({ instr.AssemblePosition.GenerateLocationText() })"
                    }
                }
            };
            setupData.MemoryContents[instr.PlacedInfo.MemoryNumber][(int)instr.PlacedInfo.Address.From + 1] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
            {
                InitialValue = operandB,
                DebugInfoCount = 1,
                DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                {
                    Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                    {
                        Usage = Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.FollowHead,
                        IsDebugMarked = false
                    }
                }
            };
            setupData.MemoryContents[instr.PlacedInfo.MemoryNumber][(int)instr.PlacedInfo.Address.From + 2] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
            {
                InitialValue = operandC,
                DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                {
                    Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                    {
                        Usage = Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.FollowHead,
                        IsDebugMarked = false
                    }
                }
            };
            setupData.MemoryContents[instr.PlacedInfo.MemoryNumber][(int)instr.PlacedInfo.Address.From + 3] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
            {
                InitialValue = operandJ,
                DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                {
                    Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                    {
                        Usage = Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.FollowHead,
                        IsDebugMarked = false
                    }
                }
            };
            return true;
        }

        private bool GenerateMemoryContentForVariable(VariableAnalyzeInfo.ElementBase varblElem, Execute.ExecuteSetupData setupData, List<AssembleError> errorList)
        {
            //Genarate

            if (varblElem.GroupedVariables.Count == 1 && varblElem.GroupedVariables[0].InitialValues.Length > 1)
            { //Array type
                Variable varbl = varblElem.GroupedVariables[0];

                setupData.MemoryContents[varblElem.PlacedInfo.MemoryNumber][(int)varblElem.PlacedInfo.Address.From] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
                {
                    InitialValue = varblElem.GetInitialValue(),
                    DebugInfoCount = 1,
                    DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                    {
                        Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                        {
                            Usage = (varblElem == varbl.AnalyzeResults[0]) ? Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.VariableArray :
                                                                     Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.FollowHead,
                            IsDebugMarked = false,
                            DebugInfo = AssemblyCode.GetBlockPathPrefix(varbl.DefinedBlock) + varbl.Name + "[" + Array.IndexOf(varbl.AnalyzeResults, varblElem) + "]"
                        }
                    }
                };
            }
            else
            { //Single type
                string debugInfo = "";
                foreach (var e in varblElem.GroupedVariables)
                {
                    if (e.InitialValues.Length == 1)
                        debugInfo += AssemblyCode.GetBlockPathPrefix(e.DefinedBlock) + e.Name + " ";
                    else
                        debugInfo += AssemblyCode.GetBlockPathPrefix(e.DefinedBlock) + e.Name + "[" + Array.IndexOf(e.AnalyzeResults, varblElem) + "]" + " ";
                }

                setupData.MemoryContents[varblElem.PlacedInfo.MemoryNumber][(int)varblElem.PlacedInfo.Address.From] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
                {
                    InitialValue = varblElem.GetInitialValue(),
                    DebugInfoCount = 1,
                    DebugInfos = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElements()
                    {
                        Element0 = new Execute.ExecuteSetupData.MemoryContent.DebugInfoElement()
                        {
                            Usage = Execute.ExecuteSetupData.MemoryContent.WordElement.enumUsage.Variable,
                            IsDebugMarked = false,
                            DebugInfo = debugInfo
                        }
                    }
                };
            }
            return true;
        }
    }
}
