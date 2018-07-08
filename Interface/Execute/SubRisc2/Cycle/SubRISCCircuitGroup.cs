using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Execute.CommonModule;

namespace Interface.Execute.SubRISC2
{
    public class SubRISCCircuitGroup : SyncModuleBase
    {
        public RAM Memory; //Slot.0
        public ASyncRAMInterfaceRW1High SyncMemoryHigh;
        public ASyncRAMInterfaceRW1Low SyncMemoryLow;
        public FetchStage FS;
        public ComputeStage CS;
        public ApplyStage AS;
        public uint ProgramCounter
        {
            get
            {
                return FS.PCOut_OFace.Value;
            }
        }
        #region サイクル統計
        public long CycleCount;
        public long BranchBubbleCount;
        public long MemoryStallCount;
        #endregion

        public AsyncModuleOutputface<ASyncRAMInterfaceRW1High.ReadCommand> SyncMemoryHighRead;
        public AsyncModuleOutputface<ASyncRAMInterfaceRW1High.WriteCommand> SyncMemoryHighWrite;
        public AsyncModuleOutputface<ASyncRAMInterfaceRW1Low.ReadCommand> SyncMemoryLowRead;
        public AsyncModuleOutputface<ASyncRAMInterfaceRW1Low.WriteCommand> SyncMemoryLowWrite;
        AsyncModuleOutputface<uint> memRdata;

        public SubRISCCircuitGroup(RAM ram,uint startupAddr,bool delayBranchEnabled)
        {
            Memory = ram;

            SyncMemoryHigh = new ASyncRAMInterfaceRW1High(this.Memory);
            RegisterSubModule(SyncMemoryHigh);
            SyncMemoryLow = new ASyncRAMInterfaceRW1Low(this.Memory);
            RegisterSubModule(SyncMemoryLow);
            memRdata = CreateAsyncOutputface<uint>();

            FS = new SubRISC2.FetchStage(delayBranchEnabled);
            RegisterSubModule(FS);
            CS = new SubRISC2.ComputeStage();
            RegisterSubModule(CS);
            AS = new SubRISC2.ApplyStage();
            RegisterSubModule(AS);

            {
                FS.Stall_IFace.BindSource(AS.MemoryStallRequest_OFace);
                FS.BranchPCFromCS_IFace.BindSource(CS.BranchPC_OFace);
                FS.BranchPCRelModeFromCS_IFace.BindSource(CS.BranchPCRelMode_OFace);
                FS.BranchRequestFromCS_IFace.BindSource(CS.BranchRequest_OFace);
                FS.LMemRdata_IFace.BindSource(SyncMemoryLow.Read_OFace);
                FS.HMemRdata_IFace.BindSource(SyncMemoryHigh.Read_OFace);

                CS.PC_IFace.BindSource(FS.PCOut_OFace);
                CS.Stall_IFace.BindSource(AS.MemoryStallRequest_OFace);
                CS.Instruction_IFace.BindSource(FS.Instruction_OFace);
                CS.ValidCS_IFace.BindSource(FS.ValidCS_OFace);
                CS.RegWen_IFace.BindSource(AS.RegWen_OFace);
                CS.RegWno_IFace.BindSource(AS.RegWno_OFace);
                CS.RegWdata_IFace.BindSource(AS.RegWdata_OFace);
                
                memRdata.SetFunc(() =>
                {
                    return (((uint)SyncMemoryHigh.Read_OFace.Value << 16) & 0xFFFF0000) | ((uint)SyncMemoryLow.Read_OFace.Value & 0xFFFF);
                });
                AS.MemOp_IFace.BindSource(CS.MemOp_OFace);
                AS.MemRw_IFace.BindSource(CS.MemRw_OFace);
                AS.AluRes_IFace.BindSource(CS.AluRes_OFace);
                AS.RegRdataC_IFace.BindSource(CS.RegRdataC_OFace);
                AS.RegNoC_IFace.BindSource(CS.RegNoC_OFace);
                AS.ValidAS_IFace.BindSource(CS.ValidAS_OFace);
                AS.MemRdata_IFace.BindSource(memRdata);

                SyncMemoryHighRead = CreateAsyncOutputface<ASyncRAMInterfaceRW1High.ReadCommand>();
                SyncMemoryHighWrite = CreateAsyncOutputface<ASyncRAMInterfaceRW1High.WriteCommand>();
                SyncMemoryLowRead = CreateAsyncOutputface<ASyncRAMInterfaceRW1Low.ReadCommand>();
                SyncMemoryLowWrite = CreateAsyncOutputface<ASyncRAMInterfaceRW1Low.WriteCommand>();
                SyncMemoryHighRead.SetFunc(() =>
                {
                    return new ASyncRAMInterfaceRW1High.ReadCommand()
                    {
                        Address = AS.MemoryStallRequest_OFace.Value ? (int)AS.MemAddr_OFace.Value 
                                                                    : (int)FS.HMemAddr_OFace.Value,
                        Enabled = (!AS.MemWen_OFace.Value && AS.MemoryStallRequest_OFace.Value) || FS.HMemEn_OFace.Value,
                        AccessType = AS.MemoryStallRequest_OFace.Value ? EnumMemorymAccessType.Data 
                                                                       : EnumMemorymAccessType.Instruction
                    };
                });
                SyncMemoryHighWrite.SetFunc(() =>
                {
                    return new ASyncRAMInterfaceRW1High.WriteCommand()
                    {
                        Address = (int)AS.MemAddr_OFace.Value,
                        Value = (ushort)(AS.MemWdata_OFace.Value >> 16),
                        Enabled = AS.MemWen_OFace.Value && AS.MemoryStallRequest_OFace.Value
                    };
                });
                SyncMemoryLowRead.SetFunc(() =>
                {
                    return new ASyncRAMInterfaceRW1Low.ReadCommand()
                    {
                        Address = AS.MemoryStallRequest_OFace.Value ? (int)AS.MemAddr_OFace.Value
                                                                    : (int)FS.LMemAddr_OFace.Value,
                        Enabled = (!AS.MemWen_OFace.Value && AS.MemoryStallRequest_OFace.Value) || FS.LMemEn_OFace.Value,
                        AccessType = AS.MemoryStallRequest_OFace.Value ? EnumMemorymAccessType.Data
                                                                       : EnumMemorymAccessType.Instruction
                    };
                });
                SyncMemoryLowWrite.SetFunc(() =>
                {
                    return new ASyncRAMInterfaceRW1Low.WriteCommand()
                    {
                        Address = (int)AS.MemAddr_OFace.Value,
                        Value = (ushort)(AS.MemWdata_OFace.Value & 0xFFFF),
                        Enabled = AS.MemWen_OFace.Value && AS.MemoryStallRequest_OFace.Value
                    };
                });
                SyncMemoryHigh.Read_IFace.BindSource(SyncMemoryHighRead);
                SyncMemoryHigh.Write_IFace.BindSource(SyncMemoryHighWrite);
                SyncMemoryLow.Read_IFace.BindSource(SyncMemoryLowRead);
                SyncMemoryLow.Write_IFace.BindSource(SyncMemoryLowWrite);
            }
        }

        protected override void UpdateModuleCycle()
        {
            CycleCount++;
            if (AS.MemoryStallRequest_OFace.Value)
                MemoryStallCount++;
            if (!FS.DelayBranchEnabled)
            {
                if (!CS.ValidCS_IFace)
                    BranchBubbleCount++;
            }
            else
            {
                byte instrOpcode = (byte)((CS.Instruction_IFace >> 30) & 3);
                bool instrJumpFlag = TestBit(CS.Instruction_IFace, 29);
                byte instrOperandA = (byte)((CS.Instruction_IFace >> 25) & 15);
                byte instrOperandB = (byte)((CS.Instruction_IFace >> 20) & 31);
                byte instrOperandD = (byte)((CS.Instruction_IFace >> 16) & 15);
                bool instrJumpByRegister = TestBit(CS.Instruction_IFace, 15);
                byte instrJumpCondition = (byte)((CS.Instruction_IFace >> 12) & 7);
                uint instrJumpAddress = (uint)((CS.Instruction_IFace >> 0) & 0x7FF);
                bool instrIsMemoryOperation = TestBit(instrOpcode, 1);
                bool instrMemoryOperationRW = instrJumpFlag;

                if (FS.Branched_OFace.Value &&
                    ((instrOperandA & 15) == 0) &&
                     !TestBit(instrOperandB, 4) &&
                     ((instrOperandB & 15) == (instrOperandD & 15)))
                    BranchBubbleCount++;
            }

            base.UpdateModuleCycle();
        }

        public string GetStatisticsInfo()
        {
            StringBuilder sb = new StringBuilder();
            long execInstructions = CycleCount - MemoryStallCount - BranchBubbleCount;
            sb.AppendLine(
                   $"All:           { CycleCount } cycles\r\n" +
                   $"Memory stall:  { MemoryStallCount } cycles ( { ((double)MemoryStallCount / CycleCount * 100).ToString("0.00") } % )\r\n" +
                   $"Branch bubble: { BranchBubbleCount } cycles ( { ((double)BranchBubbleCount / CycleCount * 100).ToString("0.00") } % )\r\n" +
                   $"Instructions:  { execInstructions } instructions ( {((double)execInstructions / CycleCount * 100).ToString("0.00") } % )");
            return sb.ToString();
        }
    }
}
