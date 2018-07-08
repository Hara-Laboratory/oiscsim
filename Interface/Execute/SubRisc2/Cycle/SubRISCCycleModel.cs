using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Assemble;
using Interface.Execute.CommonModule;
using Interface.Execute.SubRISC2;

namespace Interface.Execute
{
    //この感じなら命令レベルで処理してしまうモデルと
    // サイクル単位でしっかり計算するモデルの両方が作れるね
    public class SubRISC2CycleModel : SimulatorModelBase
    {
        public RAM Memory; //Slot.0
        public SubRISCCircuitGroup CircuitGroup;
        const uint HaltAddress = 0x7FFFFFFF;
        ExecuteSetupData SetupData;
        uint StackPointerMin;
        uint PrevProgramCounter;
        uint PrevPrevProgramCounter;
        bool PrevStalled = true;
        bool DelayBranchEnabled;

        public SubRISC2CycleModel(bool delayBranchEnabled)
        {
            PrevPrevProgramCounter = 0xFFFFFFFF;
            PrevProgramCounter = 0xFFFFFFFF;
            this.DelayBranchEnabled = delayBranchEnabled;
        }

        public static SimulatorModelBase InstansinateWithoutDelayBranch()
        {
            return new SubRISCCycleModel(false);
        }
        public static SimulatorModelBase InstansinateWithDelayBranch()
        {
            return new SubRISC2CycleModel(true);
        }

        public override bool SetupFromSetupData(ExecuteSetupData setupData)
        {
            this.SetupData = setupData;
            MessageManager.ShowLine($"Constructing memory of slot0...",enumMessageLevel.DetailProgressLog);
            this.Memory = new CommonModule.RAM();
            this.Memory.Initialize(setupData.MemoryContents[0]); //Slot.0
            InitializeExecutionTraceData(setupData, 0);

            base.IsHalted = false;

            MessageManager.ShowLine($"Constructing circuit modules...",enumMessageLevel.DetailProgressLog);
            CircuitGroup = new SubRISCCircuitGroup(this.Memory,setupData.StartupAddress,DelayBranchEnabled);
            CircuitGroup.CS.RegisterFile.Entries[1].Content = (uint)setupData.MemoryContents[0].WordCapacity * 1 - 1;
            StackPointerMin = CircuitGroup.CS.RegisterFile.Entries[1].Content;
            return true;
        }

        public override bool SetupFromAssembly(AssemblyCode code)
        {
            throw new NotImplementedException();
        }

        public override bool StepCycle()
        {
            if (IsHalted)
                return true;

            string debugInfo;
            MessageManager.ShowLine($"-Cycle.{ this.CycleCount.ToString().PadLeft(8,' ') } -------------------------------------------",enumMessageLevel.ExecutionLog);
            {
                MessageManager.GoInnerTab();
                MessageManager.ShowLine($"PC: 0x{ (CircuitGroup.FS.PCOut_OFace.Value*2).ToString("X8") }", enumMessageLevel.ExecutionLog);

                Action showRAMHighPin = () =>
                {
                    MessageManager.Show("  (RAM-High) : ", enumMessageLevel.ExecutionLog);
                    MessageManager.GoInnerTab();
                    if (CircuitGroup.AS.MemoryStallRequest_OFace.Value)
                    { //MA
                        if (CircuitGroup.SyncMemoryHighRead.Value.Enabled)
                        {
                            uint readed = (uint)CircuitGroup.SyncMemoryHigh.Read_OFace.Value << 16 | CircuitGroup.SyncMemoryLow.Read_OFace.Value;
                            MessageManager.Show($"Memory read at 0x{(CircuitGroup.SyncMemoryHighRead.Value.Address * 4).ToString("X8")}", enumMessageLevel.ExecutionLog);
                            MessageManager.Show($" => {(int)readed} (0x{readed.ToString("X8")})", enumMessageLevel.ExecutionLog);
                            
                            Memory.GetDebugInfo((uint)CircuitGroup.SyncMemoryHighRead.Value.Address, out debugInfo, 1, 0);
                            if (debugInfo != "")
                                MessageManager.Show($" ･･･ \"{ debugInfo }\"", enumMessageLevel.ExecutionLog);
                            MessageManager.ShowLine($"", enumMessageLevel.ExecutionLog);
                        }
                        else if (CircuitGroup.SyncMemoryHighWrite.Value.Enabled)
                        {
                            uint written = (uint)CircuitGroup.SyncMemoryHighWrite.Value.Value << 16 | CircuitGroup.SyncMemoryLowWrite.Value.Value;
                            MessageManager.Show($"Memory write to 0x{(CircuitGroup.SyncMemoryHighWrite.Value.Address * 4).ToString("X8")}", enumMessageLevel.ExecutionLog);
                            MessageManager.Show($" <= {(int)written} (0x{written.ToString("X8")})", enumMessageLevel.ExecutionLog);

                            Memory.GetDebugInfo((uint)CircuitGroup.SyncMemoryHighRead.Value.Address, out debugInfo, 1, 0);
                            if (debugInfo != "")
                                MessageManager.Show($" ･･･ \"{ debugInfo }\"", enumMessageLevel.ExecutionLog);
                            MessageManager.ShowLine($"", enumMessageLevel.ExecutionLog);
                        }
                    }
                    else
                    { //fetch
                        uint pc = (uint)CircuitGroup.SyncMemoryHighRead.Value.Address * 2;
                        MessageManager.Show($"Fetching at PC: 0x{ (pc*2).ToString("X8") }", enumMessageLevel.ExecutionLog);

                        Memory.GetDebugInfo(pc / 2, out debugInfo, 2, (int)pc % 2);
                        if (debugInfo != "")
                            MessageManager.ShowLine($" ･･･ \"{ debugInfo }\"", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"", enumMessageLevel.ExecutionLog);
                    }
                    MessageManager.GoOuterTab();
                };
                Action showRAMLowPin = () =>
                {
                    if (CircuitGroup.AS.MemoryStallRequest_OFace.Value)
                    { //MA
                    }
                    else
                    { //fetch
                        MessageManager.Show("  (RAM-Low) : ", enumMessageLevel.ExecutionLog);
                        MessageManager.GoInnerTab();
                        uint pc = (uint)CircuitGroup.SyncMemoryLowRead.Value.Address * 2 + 1;
                        MessageManager.Show($"Fetching at PC: 0x{ (pc * 2).ToString("X8") }", enumMessageLevel.ExecutionLog);
                        Memory.GetDebugInfo(pc / 2, out debugInfo, 2, (int)pc % 2);
                        if (debugInfo != "")
                            MessageManager.ShowLine($" ･･･ \"{ debugInfo }\"", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"", enumMessageLevel.ExecutionLog);
                        MessageManager.GoOuterTab();
                    }
                };
                if (!CircuitGroup.AS.MemoryStallRequest_OFace.Value)
                {
                    MessageManager.ShowLine("[Fetch Stage] ", enumMessageLevel.ExecutionLog);
                    if (CircuitGroup.SyncMemoryLowRead.Value.Address == CircuitGroup.SyncMemoryHighRead.Value.Address)
                    {
                        ushort intsr = CircuitGroup.SyncMemoryHigh.Read_OFace.Value;
                        byte instrOpcode = (byte)((intsr >> 14) & 3);
                        bool instrJumpFlag = ((intsr >> 13) & 1) != 0;

                        showRAMHighPin();
                        if ((instrOpcode & 2) == 0 && instrJumpFlag)
                            showRAMLowPin();
                    }
                    else
                    {
                        ushort intsr = CircuitGroup.SyncMemoryLow.Read_OFace.Value;
                        byte instrOpcode = (byte)((intsr >> 14) & 3);
                        bool instrJumpFlag = ((intsr >> 13) & 1) != 0;

                        showRAMLowPin();
                        if ((instrOpcode & 2) == 0 && instrJumpFlag)
                            showRAMHighPin();
                    }
                }
                else
                {
                    MessageManager.ShowLine($"**Stalling for Memory Access**", enumMessageLevel.ExecutionLog);
                    showRAMHighPin();
                    showRAMLowPin();
                }

                if (!CircuitGroup.AS.MemoryStallRequest_OFace.Value && CircuitGroup.CS.ValidCS_IFace)
                {
                    MessageManager.ShowLine("[Compute Stage]", enumMessageLevel.ExecutionLog);

                    MessageManager.GoInnerTab();
                    {
                        string opMode = "";
                        string condMode = "";
                        switch (CircuitGroup.CS.Instruction_IFace.Get() >> 30)
                        {
                            case 0:
                                opMode = "SUB";
                                if (((CircuitGroup.CS.Instruction_IFace.Get() >> 29) & 1) != 0)
                                    condMode = (CircuitGroup.CS.alu_condflag_OFace.Value == 1 ? "CAR" : CircuitGroup.CS.alu_condflag_OFace.Value == 4 ? "NEG" : "LSB");
                                break;
                            case 1:
                                opMode = "AND";
                                if (((CircuitGroup.CS.Instruction_IFace.Get() >> 29) & 1) != 0)
                                    condMode = (CircuitGroup.CS.alu_condflag_OFace.Value == 1 ? "CAR" : CircuitGroup.CS.alu_condflag_OFace.Value == 4 ? "NEG" : "LSB");
                                break;
                            case 3:
                                opMode = "SHR";
                                break;
                            case 2:
                                opMode = (((CircuitGroup.CS.Instruction_IFace.Get() >> 29) & 1) == 0) ? "MR" : "MW";
                                break;
                        }
                        MessageManager.Show($" *OpMode = { opMode }", enumMessageLevel.ExecutionLog);
                        if (condMode != "")
                            MessageManager.Show($" -< J{ condMode }", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine("", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine(" *ALU", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"   A = { (int)CircuitGroup.CS.alu_a_OFace.Value } (0x{CircuitGroup.CS.alu_a_OFace.Value.ToString("X8")})", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"   B = { (int)CircuitGroup.CS.alu_b_OFace.Value } (0x{CircuitGroup.CS.alu_b_OFace.Value.ToString("X8")})", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"   RES={ (int)CircuitGroup.CS.Alu.OpResult_OFace.Value } (0x{CircuitGroup.CS.Alu.OpResult_OFace.Value.ToString("X8")})", enumMessageLevel.ExecutionLog);
                        if (condMode != "")
                            MessageManager.ShowLine($"   CND={ CircuitGroup.CS.Alu.CondResult_OFace.Value }", enumMessageLevel.ExecutionLog);
                    }
                    MessageManager.GoOuterTab();
                }
                MessageManager.GoOuterTab();
            }


            if (CircuitGroup.AS.RegWen_OFace.Value && CircuitGroup.AS.RegWdata_OFace.Value == 0xFFFFFFFC)
            {

            }

            CircuitGroup.UpdateCycle();

            //System.IO.File.AppendAllText("PCLog.txt", "# " + (this.CircuitGroup.ProgramCounter * 2).ToString("X4") + "\n");

            if (this.CircuitGroup.ProgramCounter == this.PrevPrevProgramCounter && !PrevStalled)
            {
                MessageManager.ShowLine($"S Y S T E M  H A L T", enumMessageLevel.ExecutionLog);
                base.IsHalted = true;
            }
            this.PrevPrevProgramCounter = this.PrevProgramCounter;
            this.PrevProgramCounter = this.CircuitGroup.ProgramCounter;
            this.PrevStalled = this.CircuitGroup.AS.MemoryStallRequest_OFace.Value;

            this.CycleCount++;
            MarkExecutionTraceData((int)CircuitGroup.ProgramCounter, (long)CycleCount);
            StackPointerMin = Math.Min(StackPointerMin, CircuitGroup.CS.RegisterFile.Entries[1].Content);
            /*
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(this.CircuitGroup.CS.RegisterFile.Entries[i].Content.ToString("X8") + ",");
            }
            System.IO.File.AppendAllText(@"E:\cycle.txt", sb.ToString() + "\r\n");
            */
            return true;
        }

        public override bool ShowExecutionInfo(enumMessageLevel level)
        {
            //Instructions
            MessageManager.ShowLine("*Instructions", level);
            MessageManager.ShowLine("16bit = " + CircuitGroup.FS.InstructionCountPerType[0], level);
            MessageManager.ShowLine("32bit = " + CircuitGroup.FS.InstructionCountPerType[1], level);
            MessageManager.ShowLine("Average Length = " + ((double)(CircuitGroup.FS.InstructionCountPerType[0] * 2 + CircuitGroup.FS.InstructionCountPerType[1] * 4) / (CircuitGroup.FS.InstructionCountPerType[0] + CircuitGroup.FS.InstructionCountPerType[1])).ToString("0.00") + "bytes", level);
            MessageManager.ShowLine("", level);

            //Memory
            MessageManager.ShowLine("*MemoryHigh\r\n" + CircuitGroup.SyncMemoryHigh.GetStatisticsInfo(), level);
            MessageManager.ShowLine("*MemoryLow\r\n" + CircuitGroup.SyncMemoryLow.GetStatisticsInfo(), level);

            //Register
            MessageManager.ShowLine("*Register\r\n" + CircuitGroup.CS.RegisterFile.GetStatisticsInfo(), level);

            //Pipeline
            MessageManager.ShowLine("*Pipeline-stages\r\n" + CircuitGroup.GetStatisticsInfo(), level);

            //Stack
            MessageManager.ShowLine("*Stack\r\nUsage of stack:" + (SetupData.MemoryContents[0].WordCapacity * 4 - StackPointerMin) + " bytes", level);
            return true;
        }

        public override string PrintExecutionTraceData(int length)
        {
            return base.PrintExecutionTraceData(length) + ";" + (SetupData.MemoryContents[0].WordCapacity * 4 - CircuitGroup.CS.RegisterFile.Entries[1].Content);
        }

        public override bool ShowMemoryDumpByMessage(bool codeInstr, bool codeVar, bool stack, enumMessageLevel level)
        {
            ShowMemoryDumpByMessage(SetupData.MemoryContents[0], Memory, codeInstr, codeVar, stack, level);
            return true;
        }

        public override bool SaveMemoryDump(System.IO.Stream s)
        {
            SaveMemoryDump(SetupData.MemoryContents[0], Memory, s);
            return true;
        }
    }
}
