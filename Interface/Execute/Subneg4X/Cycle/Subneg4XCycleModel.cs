using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Assemble;
using Interface.Execute.CommonModule;
using Interface.Execute.Subneg4X;

namespace Interface.Execute
{
    //この感じなら命令レベルで処理してしまうモデルと
    // サイクル単位でしっかり計算するモデルの両方が作れるね
    public class Subneg4XCycleModel : SimulatorModelBase
    {
        public RAM Memory; //Slot.0
        public Subneg4XCircuitGroup CircuitGroup;
        const uint HaltAddress = 0x7FFFFFFF;
        ExecuteSetupData SetupData;

        public Subneg4XCycleModel()
        {
        }

        public static SimulatorModelBase Instansinate()
        {
            return new Subneg4XCycleModel();
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
            CircuitGroup = new Subneg4XCircuitGroup(this.Memory,setupData.StartupAddress);
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
            MessageManager.GoInnerTab();
            MessageManager.Show($"PC: 0x{ CircuitGroup.ProgramCounter.Value.ToString("X8") }",enumMessageLevel.ExecutionLog);
            Memory.GetDebugInfo(CircuitGroup.ProgramCounter.Value,out debugInfo);
            MessageManager.Show($" ･･･ \"{ debugInfo }\"",enumMessageLevel.ExecutionLog);
            MessageManager.ShowLine($"",enumMessageLevel.ExecutionLog);
            {
                MessageManager.ShowLine("[Status]",enumMessageLevel.ExecutionLog);
                MessageManager.GoInnerTab();
                MessageManager.Show($"Stage = { CircuitGroup.State.Value.ToString() }",enumMessageLevel.ExecutionLog);
                switch (CircuitGroup.State.Value)
                {
                    case -1:
                        MessageManager.ShowLine($" : RST,Issuing read PC+0 / PC+1 address",enumMessageLevel.ExecutionLog);
                        break;
                    case 0:
                        MessageManager.ShowLine($" : Issuing read MEM[PC+0] / MEM[PC+1] address",enumMessageLevel.ExecutionLog);
                        break;
                    case 1:
                        MessageManager.ShowLine($" : Saving to MemOpA / MemOpB,Issuing read PC+2 / PC+3 address",enumMessageLevel.ExecutionLog);
                        break;
                    case 2:
                        MessageManager.ShowLine($" : Writing ALU to MEM[PC+2] address,Issuing read PC+3 address",enumMessageLevel.ExecutionLog);
                        break;
                    case 3:
                        MessageManager.ShowLine($" : Branching,Issuing read NewPC+0 / NewPC+1 address",enumMessageLevel.ExecutionLog);
                        break;
                }
                MessageManager.ShowLine($"Mem[OperandA] = { ((int)CircuitGroup.MemOpA.Value).ToString() },Mem[OperandB] = { ((int)CircuitGroup.MemOpB.Value).ToString() }",enumMessageLevel.ExecutionDetailLog);
                MessageManager.GoOuterTab();

                MessageManager.ShowLine("[ALU]",enumMessageLevel.ExecutionDetailLog);
                MessageManager.GoInnerTab();
                MessageManager.ShowLine($"Result = { ((int)CircuitGroup.Alu.AluResult_OFace.Value).ToString() },BranchCond = { CircuitGroup.Alu.BranchCond_OFace.Value.ToString() }",enumMessageLevel.ExecutionDetailLog);
                MessageManager.GoOuterTab();

                MessageManager.ShowLine("[RAM]",enumMessageLevel.ExecutionLog);
                MessageManager.GoInnerTab();
                if (CircuitGroup.SyncMemory.ReadCmd1_IFace.SourceFace.Value.Enabled)
                    MessageManager.ShowLine($"ReadCmd1.{{ Addr=0x{CircuitGroup.SyncMemory.ReadCmd1_IFace.SourceFace.Value.Address.ToString("X8")} }}",enumMessageLevel.ExecutionLog);
                if (CircuitGroup.SyncMemory.ReadCmd2_IFace.SourceFace.Value.Enabled)
                    MessageManager.ShowLine($"ReadCmd2.{{ Addr=0x{CircuitGroup.SyncMemory.ReadCmd2_IFace.SourceFace.Value.Address.ToString("X8")} }}",enumMessageLevel.ExecutionLog);
                if (CircuitGroup.SyncMemory.WriteCmd_IFace.SourceFace.Value.Enabled)
                    MessageManager.ShowLine($"WriteCmd.{{ Addr=0x{CircuitGroup.SyncMemory.WriteCmd_IFace.SourceFace.Value.Address.ToString("X8")},Value={(int)CircuitGroup.SyncMemory.WriteCmd_IFace.SourceFace.Value.Value} (0x{CircuitGroup.SyncMemory.WriteCmd_IFace.SourceFace.Value.Value.ToString("X8")}) }}",enumMessageLevel.ExecutionLog);
                MessageManager.ShowLine($"ReadOut1 = {(int)CircuitGroup.SyncMemory.ReadValue1_OFace.Value} (0x{CircuitGroup.SyncMemory.ReadValue1_OFace.Value.ToString("X8")}),ReadOut2 = {(int)CircuitGroup.SyncMemory.ReadValue2_OFace.Value} (0x{CircuitGroup.SyncMemory.ReadValue2_OFace.Value.ToString("X8")})",enumMessageLevel.ExecutionLog);
                MessageManager.GoOuterTab();
            }
            MessageManager.GoOuterTab();
            
            CircuitGroup.UpdateCycle();

            if (this.CircuitGroup.ProgramCounterInput.Value == HaltAddress)
            {
                MessageManager.ShowLine($"S Y S T E M  H A L T",enumMessageLevel.ExecutionLog);
                IsHalted = true;
            }

            this.CycleCount++;
            MarkExecutionTraceData((int)CircuitGroup.ProgramCounter.Value * 2, (long)CycleCount);

            return true;
        }

        public override bool ShowExecutionInfo(enumMessageLevel level)
        {
            //Memory
            MessageManager.ShowLine("*Memory\n" + CircuitGroup.SyncMemory.GetStatisticsInfo(), level);
            return true;
        }

        public override bool ShowMemoryDumpByMessage(bool codeInstr,bool codeVar,bool stack,enumMessageLevel level)
        {
            ShowMemoryDumpByMessage(SetupData.MemoryContents[0],Memory,codeInstr,codeVar,stack,level);
            return true;
        }

        public override bool SaveMemoryDump(System.IO.Stream s)
        {
            SaveMemoryDump(SetupData.MemoryContents[0], Memory, s);
            return true;
        }
    }
}
