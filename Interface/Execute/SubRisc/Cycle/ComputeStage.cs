using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.SubRISC
{
    public class ComputeStage : SyncModuleBase
    {
        #region レジスタファイル
        public RegisterFile RegisterFile;
        public AsyncModuleOutputface<RegisterFile.WriteCommand> reg_write0_OFace;
        public AsyncModuleOutputface<RegisterFile.ReadCommand> reg_read0_OFace;
        public AsyncModuleOutputface<RegisterFile.ReadCommand> reg_read1_OFace;
        #endregion
        #region Alu
        public Alu Alu;
        public AsyncModuleOutputface<uint> alu_a_OFace;
        public AsyncModuleOutputface<uint> alu_b_OFace;
        public AsyncModuleOutputface<bool> alu_opflag_OFace;
        public AsyncModuleOutputface<byte> alu_condflag_OFace;
        #endregion
        #region 同期入力
        public ModuleInputface<uint> PC_IFace;
        public ModuleInputface<bool> Stall_IFace;
        public ModuleInputface<uint> Instruction_IFace;
        public ModuleInputface<bool> ValidCS_IFace;
        public ModuleInputface<bool> RegWen_IFace;
        public ModuleInputface<uint> RegWdata_IFace;
        public ModuleInputface<byte> RegWno_IFace;
        #endregion
        #region 同期出力
        public SyncModuleOutputface<bool> MemOp_OFace;
        public SyncModuleOutputface<bool> MemRw_OFace;
        public SyncModuleOutputface<uint> AluRes_OFace;
        public SyncModuleOutputface<uint> RegRdataC_OFace;
        public SyncModuleOutputface<byte> RegNoC_OFace;
        public SyncModuleOutputface<bool> ValidAS_OFace;
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<bool> BranchRequest_OFace;
        public AsyncModuleOutputface<bool> BranchPCRelMode_OFace;
        public AsyncModuleOutputface<uint> BranchPC_OFace;
        #endregion

        public ComputeStage()
        {
            PC_IFace = CreateInputface<uint>();
            Stall_IFace = CreateInputface<bool>();
            Instruction_IFace = CreateInputface<uint>();
            ValidCS_IFace = CreateInputface<bool>();
            RegWen_IFace = CreateInputface<bool>();
            RegWdata_IFace = CreateInputface<uint>();
            RegWno_IFace = CreateInputface<byte>();

            MemOp_OFace = CreateSyncOutputface<bool>(false);
            MemRw_OFace = CreateSyncOutputface<bool>(false);
            AluRes_OFace = CreateSyncOutputface<uint>(0);
            RegRdataC_OFace = CreateSyncOutputface<uint>(0);
            RegNoC_OFace = CreateSyncOutputface<byte>(0);
            ValidAS_OFace = CreateSyncOutputface<bool>(false);

            BranchRequest_OFace = CreateAsyncOutputface<bool>();
            BranchPCRelMode_OFace = CreateAsyncOutputface<bool>();
            BranchPC_OFace = CreateAsyncOutputface<uint>();

            RegisterFile = new SubRISC.RegisterFile();
            reg_read0_OFace = CreateAsyncOutputface<RegisterFile.ReadCommand>();
            reg_read1_OFace = CreateAsyncOutputface<RegisterFile.ReadCommand>();
            reg_write0_OFace = CreateAsyncOutputface<RegisterFile.WriteCommand>();
            reg_read0_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                RegisterFile.ReadCommand res = new RegisterFile.ReadCommand();
                res.No = instrIsMemoryOperation ? instrOperandD
                                                : instrOperandA;
                res.Enabled = instrIsMemoryOperation ? instrMemoryOperationRW
                                                     : ((res.No & 12) != 0);
                return res;
            });
            reg_read1_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                RegisterFile.ReadCommand res = new RegisterFile.ReadCommand();
                res.No = (byte)(instrOperandB & 15);
                res.Enabled = !TestBit(instrOperandB, 4);
                return res;
            });
            reg_write0_OFace.SetFunc(() =>
            {
                RegisterFile.WriteCommand res = new RegisterFile.WriteCommand();
                res.No = RegWno_IFace;
                res.Enabled = RegWen_IFace;
                res.Value = RegWdata_IFace;
                return res;
            });
            RegisterFile.Read0_IFace.BindSource(reg_read0_OFace);
            RegisterFile.Read1_IFace.BindSource(reg_read1_OFace);
            RegisterFile.Write_IFace.BindSource(reg_write0_OFace);
            this.RegisterSubModule(RegisterFile);
            
            Alu = new SubRISC.Alu();
            alu_a_OFace = CreateAsyncOutputface<uint>();
            alu_b_OFace = CreateAsyncOutputface<uint>();
            alu_opflag_OFace = CreateAsyncOutputface<bool>();
            alu_condflag_OFace = CreateAsyncOutputface<byte>();
            alu_a_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;
                
                if (instrIsMemoryOperation)
                    return (uint)(TestBit((uint)instrOperandA, 3) ? 0xFFFFFFF0 : 0u) | (uint)((instrOperandA & 7) << 1); 
                else if (reg_read0_OFace.Value.Enabled)
                    return RegisterFile.Read0_OFace.Value;
                else if ((instrOperandA & 3) == 0)
                    return 0;
                else if ((instrOperandA & 3) == 1)
                    return 0xFFFFFFFF;
                else if ((instrOperandA & 3) == 2)
                    return 1;
                else
                    return 0xFFFFFFFC;
            });
            alu_b_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                if (reg_read1_OFace.Value.Enabled)
                    return RegisterFile.Read1_OFace.Value;
                else if (TestBit(instrOperandB, 0))
                    return 0xFFFFFFFF;
                else if (TestBit(instrOperandB, 1))
                    return 1;
                else if (TestBit(instrOperandB, 2))
                    return PC_IFace << 1;
                else if (TestBit(instrOperandB, 3))
                    return 32;
                else
                    return 0;
            });
            alu_opflag_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                return TestBit(instrOpcode, 0);
            });
            alu_condflag_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                return instrJumpCondition;
            });
            Alu.OperandA_IFace.BindSource(alu_a_OFace);
            Alu.OperandB_IFace.BindSource(alu_b_OFace);
            Alu.OpFlag_IFace.BindSource(alu_opflag_OFace);
            Alu.CondFlag_IFace.BindSource(alu_condflag_OFace);
            this.RegisterSubModule(Alu);

            BranchRequest_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                return ValidCS_IFace && !TestBit(Instruction_IFace, 31) && instrJumpFlag && Alu.CondResult_OFace.Value;
            });
            BranchPCRelMode_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                return true;
            });
            BranchPC_OFace.SetFunc(() =>
            {
                byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(Instruction_IFace, 29);
                byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
                bool instrMemoryOperationRW = instrJumpFlag;

                return (TestBit(Instruction_IFace, 11) ? 0xFFFFF800 : 0u) | (instrJumpAddress & 0x7FF);
            });
        }

        protected override void UpdateModuleCycle()
        {
            byte instrOpcode = (byte)((Instruction_IFace >> 30) & 3);
            bool instrJumpFlag = TestBit(Instruction_IFace, 29);
            byte instrOperandA = (byte)((Instruction_IFace >> 25) & 15);
            byte instrOperandB = (byte)((Instruction_IFace >> 20) & 31);
            byte instrOperandD = (byte)((Instruction_IFace >> 16) & 15);
            bool instrJumpByRegister = TestBit(Instruction_IFace, 15);
            byte instrJumpCondition = (byte)((Instruction_IFace >> 12) & 7);
            uint instrJumpAddress = (uint)((Instruction_IFace >> 0) & 0x7FF);
            bool instrIsMemoryOperation = TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0);
            bool instrMemoryOperationRW = instrJumpFlag;

            if (!Stall_IFace)
            {
                MemOp_OFace.Assign(TestBit(instrOpcode, 1) && !TestBit(instrOpcode, 0));
                MemRw_OFace.Assign(instrJumpFlag);

                //Shift
                if (TestBit(instrOpcode, 1) && TestBit(instrOpcode, 0))
                { //Shift
                    AluRes_OFace.Assign((uint)alu_a_OFace.Value >> 8);
                    //System.IO.File.AppendAllText("aa.txt", alu_a_OFace.Value.ToString("X") + "\r\n");
                }
                else
                { //Other
                    AluRes_OFace.Assign(Alu.OpResult_OFace.Value);
                }

                RegRdataC_OFace.Assign(RegisterFile.Read0_OFace.Value);
                RegNoC_OFace.Assign(instrOperandD);
                ValidAS_OFace.Assign(ValidCS_IFace);
            }

            base.UpdateModuleCycle();
        }
    }
}
