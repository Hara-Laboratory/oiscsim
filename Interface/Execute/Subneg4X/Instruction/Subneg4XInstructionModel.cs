using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Assemble;
using Interface.Execute.CommonModule;

namespace Interface.Execute
{
    public class Subneg4XInstructionModel : SimulatorModelBase
    {
        const uint HaltAddress = 0x00400000;
        RAM Memory;
        uint ProgramCounter;
        ExecuteSetupData SetupData;

        public Subneg4XInstructionModel()
        {
        }

        public static SimulatorModelBase Instansinate()
        {
            return new Subneg4XInstructionModel();
        }

        public override bool SetupFromSetupData(ExecuteSetupData setupData)
        {
            this.SetupData = setupData;
            MessageManager.ShowLine($"Constructing memory of slot0...",enumMessageLevel.DetailProgressLog);
            this.Memory = new CommonModule.RAM();
            this.Memory.Initialize(setupData.MemoryContents[0]); //Slot.0
            InitializeExecutionTraceData(setupData, 0);

            MessageManager.ShowLine($"Setting ProgramCounter...",enumMessageLevel.DetailProgressLog);
            this.ProgramCounter = setupData.StartupAddress;

            base.IsHalted = false;
            return true;
        }

        public override bool SetupFromAssembly(AssemblyCode code)
        {
            throw new NotImplementedException();
        }

        public override bool StepCycle()
        {
            if (IsHalted)
                return false;
            
            string debugInfo;
            MessageManager.ShowLine($"-Cycle.{ this.CycleCount.ToString().PadLeft(8,' ') } -------------------------------------------",enumMessageLevel.ExecutionLog);
            MessageManager.GoInnerTab();
            MessageManager.Show($"PC: 0x{ ProgramCounter.ToString("X8") }" ,enumMessageLevel.ExecutionLog);
            Memory.GetDebugInfo(ProgramCounter,out debugInfo);
            MessageManager.Show($" ･･･ \"{ debugInfo }\"",enumMessageLevel.ExecutionLog);
            MessageManager.ShowLine($"",enumMessageLevel.ExecutionLog);

            //Fetch
            uint opA,opB,opC,opD;
            if (!Memory.LoadWord(ProgramCounter + 0,out opA, EnumMemorymAccessType.Instruction) ||
                !Memory.LoadWord(ProgramCounter + 1,out opB, EnumMemorymAccessType.Instruction) ||
                !Memory.LoadWord(ProgramCounter + 2,out opC, EnumMemorymAccessType.Instruction) ||
                !Memory.LoadWord(ProgramCounter + 3,out opD, EnumMemorymAccessType.Instruction))
            {
                MessageManager.GoOuterTab();
                return false;
            }

            //Read
            uint memOpA,memOpB;
            if (!Memory.LoadWord(opA,out memOpA, EnumMemorymAccessType.Data) ||
                !Memory.LoadWord(opB,out memOpB, EnumMemorymAccessType.Data))
            {
                MessageManager.GoOuterTab();
                return false;
            }

            //Alu
            bool opcode = (opD & 0x80000000) != 0;
            if (!opcode)
            {
                uint writeValue;
                bool branchCondition;
                Subneg4X.Alu.ComputeSubneg(memOpA,memOpB,out writeValue,out branchCondition);
                MessageManager.ShowLine($"Mem[0x{opC.ToString("X8")}]  <=  {((int)memOpB).ToString()} (Mem[0x{ opB.ToString("X8") }])  -  {((int)memOpA).ToString() } (Mem[0x{ opA.ToString("X8") }])",enumMessageLevel.ExecutionLog);
                MessageManager.ShowLine($"                  =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }",enumMessageLevel.ExecutionLog);

                //Write
                if (!Memory.StoreWord(opC,writeValue))
                {
                    MessageManager.GoOuterTab();
                    return false;
                }
                if (opC >= 261 && opC <= 261 + 4)
                {
                    MessageManager.ShowLine($"-Cycle.{ this.CycleCount.ToString().PadLeft(8, ' ') } -------------------------------------------", enumMessageLevel.ExecutionLog);
                    MessageManager.ShowLine($"Mem[0x{opC.ToString("X8")}]  <=  {((int)memOpB).ToString()} (Mem[0x{ opB.ToString("X8") }])  -  {((int)memOpA).ToString() } (Mem[0x{ opA.ToString("X8") }])", enumMessageLevel.ExecutionLog);
                    MessageManager.ShowLine($"                  =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }", enumMessageLevel.ExecutionLog);
                }

                //Branch
                uint branchTarget = opD & 0x7FFFFFFF;
                if (branchCondition && branchTarget != this.ProgramCounter + 4)
                {
                    MessageManager.ShowLine($"Jump to 0x{ (opD & 0x7FFFFFFF).ToString("X8") }",enumMessageLevel.ExecutionLog);

                    this.ProgramCounter = branchTarget;
                }
                else
                {
                    this.ProgramCounter += 4;
                }
            }
            else
            {
                uint writeValue;
                bool branchCondition;
                Subneg4X.Alu.ComputeSubnegX(memOpA,memOpB,out writeValue,out branchCondition);
                MessageManager.ShowLine($"Mem[0x{opC.ToString("X8")}]  <=  [31]: {((int)memOpB).ToString()} (Mem[0x{ opB.ToString("X8") }]  <  {((int)memOpA).ToString() } (Mem[0x{ opA.ToString("X8") }])",enumMessageLevel.ExecutionLog);
                MessageManager.ShowLine($"                  [30-0]: { memOpB.ToString()} (Mem[0x{ opB.ToString("X8") }])  -  {memOpA.ToString() } (Mem[0x{ opA.ToString("X8") }])",enumMessageLevel.ExecutionLog);
                MessageManager.ShowLine($"                  =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }",enumMessageLevel.ExecutionLog);


                //Write
                if (!Memory.StoreWord(opC,writeValue))
                {
                    MessageManager.GoOuterTab();
                    return false;
                }

                //Branch
                uint branchTarget = opD & 0x7FFFFFFF;
                if (branchCondition && branchTarget != this.ProgramCounter + 4)
                {
                    MessageManager.ShowLine($"Jump to 0x{ (opD & 0x7FFFFFFF).ToString("X8") }",enumMessageLevel.ExecutionLog);

                    this.ProgramCounter = branchTarget;
                }
                else
                {
                    this.ProgramCounter += 4;
                }
            }

            MessageManager.GoOuterTab();

            if (this.ProgramCounter >= HaltAddress)
            {
                MessageManager.ShowLine($"S Y S T E M  H A L T",enumMessageLevel.ExecutionLog);
                base.IsHalted = true;
            }

            CycleCount++;
            MarkExecutionTraceData((int)ProgramCounter * 2,(long)CycleCount);

            return true;
        }

        public override bool ShowExecutionInfo(enumMessageLevel level)
        {
            return true;
        }

        public override bool ShowMemoryDumpByMessage(bool codeInstr, bool codeVar, bool stack,enumMessageLevel level)
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
