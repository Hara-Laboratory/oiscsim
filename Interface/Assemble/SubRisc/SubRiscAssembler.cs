using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Execute;
using Interface.Assemble.SubRisc;

namespace Interface.Assemble
{
    public class SubRiscAssembler : AssemblerBase
    {
        public static readonly RegisterMapping RegisterMapping = new RegisterMapping(new RegisterMapping.RegisterElement[]
            {
                new RegisterMapping.RegisterElement("ra", -1, 0, 0),
                new RegisterMapping.RegisterElement("sp", -1, 1, 1),
                new RegisterMapping.RegisterElement("v0", -1, 2, 2),
                new RegisterMapping.RegisterElement("v1", -1, 3, 3),
                new RegisterMapping.RegisterElement("a0", 4, 4, 4),
                new RegisterMapping.RegisterElement("a1", 5, 5, 5),
                new RegisterMapping.RegisterElement("a2", 6, 6, 6),
                new RegisterMapping.RegisterElement("a3", 7, 7, 7),
                new RegisterMapping.RegisterElement("m0", 8, 8, 8),
                new RegisterMapping.RegisterElement("m1", 9, 9, 9),
                new RegisterMapping.RegisterElement("t0", 10, 10, 10),
                new RegisterMapping.RegisterElement("t1", 11, 11, 11),
                new RegisterMapping.RegisterElement("t2", 12, 12, 12),
                new RegisterMapping.RegisterElement("t3", 13, 13, 13),
                new RegisterMapping.RegisterElement("t4", 14, 14, 14),
                new RegisterMapping.RegisterElement("t5", 15, 15, 15),
                new RegisterMapping.RegisterElement("Z", 0, 16, -1),   //1_0000
                new RegisterMapping.RegisterElement("INC", 1, 17, -1), //1_0001
                new RegisterMapping.RegisterElement("DEC", 2, 18, -1), //1_0010
                new RegisterMapping.RegisterElement("PC", -1, 20, -1), //1_0100
                new RegisterMapping.RegisterElement("WIDTH", -1, 24, -1),
                new RegisterMapping.RegisterElement("NFOUR", 3, -1, -1),
            });
        struct InstructionAnalyzeInfo
        {
            public int Type;
            public int LengthInHalfWord;
            public bool NeedToAlign;
        }
        InstructionAnalyzeInfo[][] InstructionAnalyzeInfos;

        public SubRiscAssembler()
        {
        }

        public static AssemblerBase Instansinate()
        {
            return new SubRiscAssembler();
        }

        public override bool Assemble(AssemblyCode code, out ExecuteSetupData res, List<AssembleError> errorList)
        {
            res = new Execute.ExecuteSetupData(1);
            
            //Search section marked as startup
            MessageManager.ShowLine($"Determining entry point...", enumMessageLevel.DetailProgressLog);
            Section startupSection = null;
            int startupSectionIndex = -1;
            for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
            {
                var sect = code.Sections[sectIdx];
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
                startupSectionIndex = sectIdx;
            }

            //Analyze instruction type and length
            if (!AnalyzeInstructionType(code, errorList))
                return false;

            //Assign addresses
            MessageManager.ShowLine($"Assigning memory address...", enumMessageLevel.DetailProgressLog);
            uint ramByteSize;
            uint instructionByteSize;
            if (!AssignAddresses(code, startupSectionIndex, errorList, out ramByteSize, out instructionByteSize))
                return false;

            //Allocate memory region on slot.0
            MessageManager.ShowLine($"Allocating memory regions...", enumMessageLevel.DetailProgressLog);
            res.MemoryContents[0].ExpandCapacity((int)ramByteSize / 4);

            //Garantee minimum of memory size
            if (ramByteSize / 4 < code.MinMemorySize)
            {
                res.MemoryContents[0].ExpandCapacityForStack(code.MinMemorySize - (int)ramByteSize / 4);
            }

            //Generate contents
            MessageManager.ShowLine($"Generating memory initial contents...", enumMessageLevel.DetailProgressLog);

            if (!GenerateMemoryContents(code, res, errorList))
                return false;

            res.StartupAddress = 0;
            return true;
        }

        private bool AnalyzeInstructionType(AssemblyCode code, List<AssembleError> errorList)
        {
            InstructionAnalyzeInfos = new InstructionAnalyzeInfo[code.Sections.Count][];

            for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
            {
                var sect = code.Sections[sectIdx];
                InstructionAnalyzeInfos[sectIdx] = new InstructionAnalyzeInfo[sect.AllInstructions.Length];

                for (int instrIdx = 0; instrIdx < sect.AllInstructions.Length; instrIdx++)
                {
                    var instr = sect.AllInstructions[instrIdx];

                    int type;
                    if (!InstructionAssembler.SearchAssemblerType(instr, out type, errorList))
                        return false;
                    InstructionAnalyzeInfos[sectIdx][instrIdx].Type = type;

                    int length;
                    bool needToAlign;
                    if (!InstructionAssembler.EstimateInstructionLength(instr, type, out length, out needToAlign, errorList))
                        return false;
                    InstructionAnalyzeInfos[sectIdx][instrIdx].LengthInHalfWord = length;
                    InstructionAnalyzeInfos[sectIdx][instrIdx].NeedToAlign = needToAlign;
                }
            }

            return true;
        }

        private bool AssignAddresses(AssemblyCode code, int startupSectionIndex, List<AssembleError> errorList, out uint ramBottomByteAddress, out uint instructionByteSize)
        {
            ramBottomByteAddress = 0;
            instructionByteSize = 0;

            //Assign address of instructions
            {
                if (startupSectionIndex >= 0)
                { //From section marked as startup
                    AssignAddressForSectionInstruction(code, startupSectionIndex, ref ramBottomByteAddress);
                }
                for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
                {
                    if (sectIdx == startupSectionIndex)
                        continue;

                    Section sect = code.Sections[sectIdx];
                    AssignAddressForSectionInstruction(code, sectIdx, ref ramBottomByteAddress);
                }
            }
            instructionByteSize = ramBottomByteAddress;

            //Alignment
            if (ramBottomByteAddress % 4 != 0)
                ramBottomByteAddress += 4 - (ramBottomByteAddress % 4);

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
                            From = ramBottomByteAddress,
                            To = ramBottomByteAddress
                        }
                    };
                    ramBottomByteAddress += 1 * 4;

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
                            From = ramBottomByteAddress,
                            To = ramBottomByteAddress
                        }
                    };
                    ramBottomByteAddress += 1 * 4;

                    e.PlacedInfo = placeInfo;
                }
            }

            return true;
        }
        private void AssignAddressForSectionInstruction(AssemblyCode code, int sectionIndex, ref uint ramBottomByteAddress)
        {
            var sect = code.Sections[sectionIndex];
            for (int instrIdx = 0; instrIdx < sect.AllInstructions.Length; instrIdx++)
            {
                Instruction instr = sect.AllInstructions[instrIdx];
                InstructionAnalyzeInfo analyzeInfo = InstructionAnalyzeInfos[sectionIndex][instrIdx];
                
                //Alignment (If needed)
                if (analyzeInfo.NeedToAlign && (ramBottomByteAddress % 4) != 0)
                {
                    ramBottomByteAddress += 4 - (ramBottomByteAddress % 4);
                }

                //Assign
                instr.PlacedInfo.MemoryNumber = 0;
                instr.PlacedInfo.Address = new Misc.AddressRange()
                {
                    From = ramBottomByteAddress,
                    To = ramBottomByteAddress + (uint)analyzeInfo.LengthInHalfWord * 2 - 1
                };

                //Update
                ramBottomByteAddress += (uint)analyzeInfo.LengthInHalfWord * 2;

                //This process is not necessary
                for (int oprIdx = 0; oprIdx < instr.Operands.Length; oprIdx++)
                {
                    instr.Operands[oprIdx].PlacedInfo.MemoryNumber = 0;
                    instr.Operands[oprIdx].PlacedInfo.Address = instr.PlacedInfo.Address; 
                }
            }
        }
        private void WriteInstructionData(Instruction instr, ushort[] buffer, int index, Execute.ExecuteSetupData setupData)
        {
            int halfAddr = (int)instr.PlacedInfo.Address.From / 2 + index;
            List<ExecuteSetupData.MemoryContent.WordElement> wordArray = setupData.MemoryContents[instr.PlacedInfo.MemoryNumber].Words;
            ExecuteSetupData.MemoryContent.WordElement updated = wordArray[halfAddr / 2];

            updated.DebugInfoCount = 2;
            var headDebugInfo = new ExecuteSetupData.MemoryContent.DebugInfoElement()
            {
                DebugDisplayText = instr.DebugText,
                IsDebugMarked = (instr.DebugText.Length > 0),
                DebugInfo = instr.ToString().Replace("\r\n", " ").Replace("\t", "  ") + $" ({ instr.AssemblePosition.GenerateLocationText() })",
                Usage = ExecuteSetupData.MemoryContent.WordElement.enumUsage.Instruction
            };
            var tailDebugInfo = new ExecuteSetupData.MemoryContent.DebugInfoElement()
            {
                IsDebugMarked = false,
                Usage = ExecuteSetupData.MemoryContent.WordElement.enumUsage.FollowHead
            };

            if (halfAddr % 2 == 0)
            { //31-16
                updated.InitialValue |= (uint)((buffer[index] << 16) & 0xFFFF0000);
                updated.DebugInfos[0] = (index == 0) ? headDebugInfo : tailDebugInfo;
            }
            else
            { //15-0
                updated.InitialValue |= (uint)(buffer[index] & 0xFFFF);
                updated.DebugInfos[1] = (index == 0) ? headDebugInfo : tailDebugInfo;
            }
            wordArray[halfAddr / 2] = updated;
        }
        private bool GenerateMemoryContents(AssemblyCode code, Execute.ExecuteSetupData setupData, List<AssembleError> errorList)
        {
            //Generate memory contents of instructions
            {
                ushort[] buffer = new ushort[InstructionAssembler.MaxLengthPerInstruction * 2];
                for (int sectIdx = 0; sectIdx < code.Sections.Count; sectIdx++)
                {
                    Section sect = code.Sections[sectIdx];

                    for (int instrIdx = 0; instrIdx < sect.AllInstructions.Length; instrIdx++)
                    {
                        Instruction instr = sect.AllInstructions[instrIdx];
                        
                        int length;
                        bool needToAlign;
                        if (!InstructionAssembler.AssembleInstruction(instr, InstructionAnalyzeInfos[sectIdx][instrIdx].Type, buffer, out length, out needToAlign, errorList))
                            return false;

                        //Write (Address is specified by byte address)
                        for (int pos = 0; pos < length; pos++)
                        {
                            WriteInstructionData(instr, buffer, pos, setupData);
                        }
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

        private bool GenerateMemoryContentForVariable(VariableAnalyzeInfo.ElementBase varblElem, Execute.ExecuteSetupData setupData, List<AssembleError> errorList)
        {
            //Genarate

            if (varblElem.GroupedVariables.Count == 1 && varblElem.GroupedVariables[0].InitialValues.Length > 1)
            { //Array type
                Variable varbl = varblElem.GroupedVariables[0];

                setupData.MemoryContents[varblElem.PlacedInfo.MemoryNumber][(int)varblElem.PlacedInfo.Address.From / 4] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
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

                setupData.MemoryContents[varblElem.PlacedInfo.MemoryNumber][(int)varblElem.PlacedInfo.Address.From / 4] = new Execute.ExecuteSetupData.MemoryContent.WordElement()
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
