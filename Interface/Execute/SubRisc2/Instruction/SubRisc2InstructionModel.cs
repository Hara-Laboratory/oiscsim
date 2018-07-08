using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Interface.Assemble;
using Interface.Execute.CommonModule;

//命令だけがワードアドレスになればよい
//つまり．メモリロードストアに利用するベースアドレスがワードアドレス刻みになればよい
//データ読み書きは今までワード単位で行っていたため問題ない
//PC相対ロードのベースアドレスが今までバイトアドレスで与えられていたことが問題
//
//今まで
//[Case.1]
//0x00: mr -1, $PC, $D; //実行されるときはベースアドレス=0x02, ベースワードアドレス=0x00
//0x02: (padding)
//0x04: imm -1;
//(ベースアドレス - (-1)*2)/4 = (0x02 + 2)/4 = 0x04がアクセスされる
//
//[Case.2]
//0x02: mr -1, $PC, $D; //実行されるときはベースアドレス=0x04, ベースワードアドレス=0x01
//0x04: imm -1; 
//(ベースアドレス - (-1)*2)/4 = (0x04 + 2)/4 = 0x04がアクセスされる
//
//
//これから
//[Case.1]
//0x00: mr -1, $PC, $D; //実行されるときはベースアドレス=0x02, ベースワードアドレス=0x00
//0x02: (padding)
//0x04: imm -1;
//ベースワードアドレス - (-1) = 0x00 + 1 = 0x01をアクセスしたいので、
//「mr -1, $PC, $D;」のオフセットは-1としてコンパイルする
//
//[Case.2]
//0x02: mr -1, $PC, $D; //実行されるときはベースアドレス=0x04, ベースワードアドレス=0x01
//0x04: imm -1; 
//ベースワードアドレス - (0) = 0x01 + 0 = 0x01をアクセスしたいので、
//「mr -1, $PC, $D;」のオフセットは0としてコンパイルする
//
//
//また、分岐先はバイトアドレスだが、変数の参照はワードアドレスに直す必要がある -> immもそうだし、変数に初期値として格納したアドレスもワードアドレスに直さなくては

namespace Interface.Execute
{
    public class SubRisc2InstructionModel : SimulatorModelBase
    {
        const uint HaltAddress = 0x00FFFFFF;
        RAM Memory;
        uint ProgramCounter; //By byte address
        uint PrevProgramCounter;
        uint PrevPrevProgramCounter;
        uint StackPointerMin;
        ExecuteSetupData SetupData;
        bool BranchHappened = false;
        uint BranchTarget = 0;
        const int RegisterNum_Z = 16;
        const int RegisterNum_INC = 17;
        const int RegisterNum_DEC = 18;
        const int RegisterNum_PC = 19;
        const int RegisterNum_WIDTH = 20;
        const int RegisterNum_NFOUR = 21;
        const int RegisterEntryCount = 32;
        uint[] RegisterEntrys = new uint[RegisterEntryCount];
        int[] OperandAIndexTable = new int[16]
        {
            RegisterNum_Z,RegisterNum_INC,RegisterNum_DEC,RegisterNum_NFOUR,
            4,5,6,7,8,9,10,11,12,13,14,15
        };
        int[] OperandBIndexTable = new int[32]
        {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,
            RegisterNum_Z,
            RegisterNum_INC,
            RegisterNum_DEC,-1,
            RegisterNum_PC,-1,-1,-1,
            RegisterNum_WIDTH,-1,-1,-1,-1,-1,-1,-1
        };
        int[] OperandCIndexTable = new int[16]
        {
            0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
        };
        protected string ConvertRegisterNum(uint num)
        {
            int idx;
            RegisterMapping.RegisterElement reg;

            idx = Array.IndexOf(OperandAIndexTable, (int)num);
            if (idx >= 0 && SubRisc2Assembler.RegisterMapping.SearchByOperand(idx, 0, out reg))
            {
                return reg.Name;
            }

            idx = Array.IndexOf(OperandBIndexTable, (int)num);
            if (idx >= 0 && SubRisc2Assembler.RegisterMapping.SearchByOperand(idx, 1, out reg))
            {
                return reg.Name;
            }

            idx = Array.IndexOf(OperandCIndexTable, (int)num);
            if (idx >= 0 && SubRisc2Assembler.RegisterMapping.SearchByOperand(idx, 2, out reg))
            {
                return reg.Name;
            }

            return "??";
        }


        public SubRisc2InstructionModel()
        {
        }
        
        public static SimulatorModelBase Instansinate()
        {
            return new SubRisc2InstructionModel();
        }

        public override bool SetupFromSetupData(ExecuteSetupData setupData)
        {
            this.SetupData = setupData;
            MessageManager.ShowLine($"Constructing memory of slot0...", enumMessageLevel.DetailProgressLog);
            this.Memory = new CommonModule.RAM();
            this.Memory.Initialize(setupData.MemoryContents[0]); //Slot.0
            InitializeExecutionTraceData(setupData, 0);

            MessageManager.ShowLine($"Setting ProgramCounter...", enumMessageLevel.DetailProgressLog);
            this.ProgramCounter = setupData.StartupAddress;
            this.PrevProgramCounter = 0xFFFFFFFF;
            this.PrevPrevProgramCounter = 0xFFFFFFFF;
            RegisterEntrys[1] = (uint)setupData.MemoryContents[0].WordCapacity * 4 - 4;
            StackPointerMin = RegisterEntrys[1];

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
                return true;

            string debugInfo;
            MessageManager.ShowLine($"-Cycle.{ this.CycleCount.ToString().PadLeft(8, ' ') } -------------------------------------------", enumMessageLevel.ExecutionLog);
            MessageManager.GoInnerTab();
            MessageManager.Show($"PC: 0x{ ProgramCounter.ToString("X8") }", enumMessageLevel.ExecutionLog);
            Memory.GetDebugInfo(ProgramCounter / 4, out debugInfo, 4, (int)(ProgramCounter % 4));
            MessageManager.Show($" ･･･ \"{ debugInfo }\"", enumMessageLevel.ExecutionLog);
            MessageManager.ShowLine($"", enumMessageLevel.ExecutionLog);

            RegisterEntrys[RegisterNum_PC] = (ProgramCounter + 2) / 4;
            RegisterEntrys[RegisterNum_Z] = 0;
            RegisterEntrys[RegisterNum_DEC] = 1;
            RegisterEntrys[RegisterNum_INC] = 0xFFFFFFFF;
            RegisterEntrys[RegisterNum_NFOUR] = 1u + ~4u;
            RegisterEntrys[RegisterNum_WIDTH] = 32;

            //Fetch
            uint instr;
            { //Left instruction
                if (!Memory.LoadWord(ProgramCounter / 4, out instr, EnumMemorymAccessType.Instruction, 2, ((int)ProgramCounter / 2) % 2))
                {
                    MessageManager.GoOuterTab();
                    return false;
                }
                Memory.CountExecuteWord(ProgramCounter / 4, 2, ((int)ProgramCounter / 2) % 2);

                if (ProgramCounter % 4 == 0)
                    instr = (instr >> 16) & 0xFFFF;
                else
                    instr = instr & 0xFFFF;
            }

            //Decode
            byte opcode = (byte)((instr >> 14) & 0x3);
            bool jumpFlag = ((instr >> 13) & 0x1) != 0;
            uint op0, op1, op2;
            op0 = ((instr >> 9) & 0xF);
            op1 = ((instr >> 4) & 0x1F);
            op2 = (instr & 0xF);
            int offset0 = (int)(((op0 >> 3) & 1) != 0 ? (0xFFFFFFF0 | op0) : op0);
            op0 = (uint)OperandAIndexTable[op0];
            op1 = (uint)OperandBIndexTable[op1];
            op2 = (uint)OperandCIndexTable[op2];
            if ((int)op0 < 0 || (int)op1 < 0 || (int)op2 < 0)
            {
                MessageManager.ShowLine($"op0={(int)op0},op1={(int)op1},op2={(int)op2}", enumMessageLevel.ExecutionLog);

                MessageManager.GoOuterTab();
                return false;
            }

            //Branch
            bool branchCond = false;
            bool branchNotDelayed = false;
            uint branchAddress = 0;
            if (opcode < 2 && jumpFlag)
            {
                uint jumpAttr;
                { //Fetch
                    uint jumpAttrByteAddr = ProgramCounter + 2;
                    if (!Memory.LoadWord(jumpAttrByteAddr / 4, out jumpAttr, EnumMemorymAccessType.Instruction, 2, ((int)jumpAttrByteAddr / 2) % 2))
                    {
                        MessageManager.GoOuterTab();
                        return false;
                    }

                    if (jumpAttrByteAddr % 4 == 0)
                        jumpAttr = (jumpAttr >> 16) & 0xFFFF;
                    else
                        jumpAttr = jumpAttr & 0xFFFF;
                }

                byte condFlag = (byte)((jumpAttr >> 12) & 0x7);
                byte regFlag = (byte)((jumpAttr >> 15) & 0x1);
                uint targetAddr = (uint)(jumpAttr & 0x00000FFF) | ((jumpAttr & 0x000000800) != 0 ? 0xFFFFF000 : 0);
                branchAddress = ProgramCounter + 2 + targetAddr * 2; //((opcode < 2 && jumpFlag) ? 2u : 0u) + 
                { //Determine
                    switch (condFlag)
                    {
                        case 1: //carry
                            ulong brtmp1 = (ulong)RegisterEntrys[op1] - (ulong)RegisterEntrys[op0];
                            branchCond = (brtmp1 & 0x100000000) == 0;
                            break;
                        case 2: //lsb
                            branchCond = ((RegisterEntrys[op1] & RegisterEntrys[op0]) & 0x1) == 0;
                            break;
                        case 4: //neg
                            branchCond = (int)(RegisterEntrys[op1] - RegisterEntrys[op0]) < 0;
                            break;
                        default:

                            break;
                    }
                }
            }
            
            //Wriite
            switch (opcode)
            {
                case 0: //Sub
                    {
                        uint writeValue = RegisterEntrys[op1] - RegisterEntrys[op0];
                        MessageManager.ShowLine($"${ConvertRegisterNum(op2).PadRight(2)}  <=  {((int)RegisterEntrys[op1]).ToString()} (${ConvertRegisterNum(op1)})  -  {((int)RegisterEntrys[op0]).ToString()} (${ConvertRegisterNum(op0)})", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"      =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }", enumMessageLevel.ExecutionLog);

                        RegisterEntrys[op2] = writeValue;
                    }
                    break;
                case 1: //Xan
                    {
                        uint writeValue = RegisterEntrys[op1] & RegisterEntrys[op0];
                        /*
                                            (((RegisterEntrys[op1] & RegisterEntrys[op0]) >> 1) & 0x7FFFFFFF) |
                                              (RegisterEntrys[op1] < RegisterEntrys[op0] ? 0x80000000 : 0);
                                              */
                        MessageManager.ShowLine($"${ConvertRegisterNum(op2).PadRight(2)}  <=  [31]: {((int)RegisterEntrys[op1]).ToString()} (${ConvertRegisterNum(op1)})  <  {((int)RegisterEntrys[op0]).ToString() } (${ConvertRegisterNum(op0)})", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"        [30-0]: { RegisterEntrys[op1].ToString()} (${ConvertRegisterNum(op1)})  -  {RegisterEntrys[op0].ToString()} (${ConvertRegisterNum(op0)})", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"      =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }", enumMessageLevel.ExecutionLog);

                        RegisterEntrys[op2] = writeValue;
                    }
                    break;
                case 3: //Shift
                    {
                        uint writeValue = (RegisterEntrys[op0]) >> 8;

                        MessageManager.ShowLine($"${ConvertRegisterNum(op2).PadRight(2)}  <=  {((int)RegisterEntrys[op1]).ToString()} (${ConvertRegisterNum(op1)}) >> 8", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"      =  { ((int)writeValue).ToString() } = 0x{ writeValue.ToString("X8") }", enumMessageLevel.ExecutionLog);

                        RegisterEntrys[op2] = writeValue;
                    }
                    break;
                case 2: //Mr,Mw (by jump flag)
                    branchCond = false;
                    branchNotDelayed = true;
                    if (!jumpFlag)
                    { //Mr
                        uint mem;
                        if (!Memory.LoadWord((uint)(RegisterEntrys[op1] - offset0), out mem, EnumMemorymAccessType.Data, 1, 0))
                        {
                            MessageManager.GoOuterTab();
                            return false;
                        }

                        MessageManager.ShowLine($"${ConvertRegisterNum(op2).PadRight(2)}  <=  MEM[0x{RegisterEntrys[op1].ToString("X8")}(${ConvertRegisterNum(op1)}) - ({offset0})]", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"      =  MEM[0x{((RegisterEntrys[op1] - offset0)).ToString("X8")}]", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"      = {mem} = 0x{mem.ToString("X8")}", enumMessageLevel.ExecutionLog);

                        RegisterEntrys[op2] = mem;

                        if (op1 == RegisterNum_PC) //PC Relative
                        {
                            branchCond = true;
                            branchAddress = (ProgramCounter & 0xFFFFFFFC) + 8;
                        }
                    }
                    else
                    { //Mw
                        /*if ((uint)(RegisterEntrys[op1] - offset0 * 2) / 4 >= 34 &&
                            (uint)(RegisterEntrys[op1] - offset0 * 2) / 4 <= 34 + 4)
                        {
                            MessageManager.ShowLine($"-Cycle.{ this.CycleCount.ToString().PadLeft(8, ' ') } -------------------------------------------", enumMessageLevel.ProgressLog);
                            MessageManager.ShowLine($"MEM[0x{RegisterEntrys[op1].ToString("X8")}(${ConvertRegisterNum(op1)})-({offset0 * 2}) = 0x{(RegisterEntrys[op1] - offset0 * 2).ToString("X8")}]", enumMessageLevel.ProgressLog);
                            MessageManager.ShowLine($"                <= ${ConvertRegisterNum(op2)}", enumMessageLevel.ProgressLog);
                            MessageManager.ShowLine($"                 = {RegisterEntrys[op2]} (0x{RegisterEntrys[op2].ToString("X8")})", enumMessageLevel.ProgressLog);
                        }*/
                        if (!Memory.StoreWord((uint)(RegisterEntrys[op1] - offset0), RegisterEntrys[op2], 1, 0))
                        {
                            MessageManager.GoOuterTab();
                            return false;
                        }

                        MessageManager.ShowLine($"MEM[0x{RegisterEntrys[op1].ToString("X8")}(${ConvertRegisterNum(op1)})-({offset0}) = 0x{(RegisterEntrys[op1] - offset0).ToString("X8")}]", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"                <= ${ConvertRegisterNum(op2)}", enumMessageLevel.ExecutionLog);
                        MessageManager.ShowLine($"                 = {RegisterEntrys[op2]} (0x{RegisterEntrys[op2].ToString("X8")})", enumMessageLevel.ExecutionLog);
                    }
                    break;
            }
            
            //Branch
            if (branchCond)
            {
                MessageManager.ShowLine($"Jump Happen to 0x{branchAddress.ToString("X8")}", enumMessageLevel.ExecutionLog);

                if (!branchNotDelayed)
                {
                    BranchHappened = true;
                    BranchTarget = branchAddress;
                    ProgramCounter += (opcode < 2 && jumpFlag) ? 4u : 2u;
                }
                else
                {
                    ProgramCounter = branchAddress;
                }
            }
            else
            {
                if (BranchHappened)
                {
                    ProgramCounter = BranchTarget;
                    BranchHappened = false;
                }
                else
                {
                    ProgramCounter += (opcode < 2 && jumpFlag) ? 4u : 2u;
                }
            }
            MessageManager.GoOuterTab();

            if (this.ProgramCounter == this.PrevPrevProgramCounter)
            {
                MessageManager.ShowLine($"S Y S T E M  H A L T", enumMessageLevel.ExecutionLog);
                base.IsHalted = true;
            }
            this.PrevPrevProgramCounter = this.PrevProgramCounter;
            this.PrevProgramCounter = this.ProgramCounter;

            CycleCount++;
            MarkExecutionTraceData((int)ProgramCounter / 2, (long)CycleCount);
            StackPointerMin = Math.Min(StackPointerMin, RegisterEntrys[1]);
            /*
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                sb.Append(RegisterEntrys[i].ToString("X8") + ",");
            }
            System.IO.File.AppendAllText(@"E:\instr.txt", sb.ToString() + "\r\n");
            */
            return true;
        }

        public override bool ShowExecutionInfo(enumMessageLevel level)
        {
            //Stack
            MessageManager.ShowLine("*Stack\r\nUsage of stack:" + (SetupData.MemoryContents[0].WordCapacity * 4 - StackPointerMin) + " bytes", level);

            return true;
        }

        public override string PrintExecutionTraceData(int length)
        {
            return base.PrintExecutionTraceData(length) + ";" + (SetupData.MemoryContents[0].WordCapacity * 4 - RegisterEntrys[1]);
        }

        public override bool ShowMemoryDumpByMessage(bool codeInstr, bool codeVar, bool stack, enumMessageLevel level)
        {
            ShowMemoryDumpByMessage(SetupData.MemoryContents[0], Memory, codeInstr, codeVar, stack, level);
            return true;
        }

        public override bool SaveMemoryDump(Stream s)
        {
            SaveMemoryDump(SetupData.MemoryContents[0], Memory, s);
            return true;
        }
    }
}
