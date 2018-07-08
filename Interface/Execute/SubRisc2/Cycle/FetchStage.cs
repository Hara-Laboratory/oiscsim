using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.SubRISC2
{
    public class FetchStage : SyncModuleBase
    {
        #region 同期入力
        public ModuleInputface<bool> Stall_IFace;
        public ModuleInputface<ushort> LMemRdata_IFace;
        public ModuleInputface<ushort> HMemRdata_IFace;
        public ModuleInputface<bool> BranchRequestFromCS_IFace;
        public ModuleInputface<bool> BranchPCRelModeFromCS_IFace;
        public ModuleInputface<uint> BranchPCFromCS_IFace;
        #endregion
        #region 同期出力
        public SyncModuleOutputface<uint> PCOut_OFace;
        public SyncModuleOutputface<uint> Instruction_OFace;
        public SyncModuleOutputface<bool> ValidCS_OFace;
        public SyncModuleOutputface<bool> Branched_OFace;
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<bool> LMemEn_OFace;
        public AsyncModuleOutputface<uint> LMemAddr_OFace;
        public AsyncModuleOutputface<bool> HMemEn_OFace;
        public AsyncModuleOutputface<uint> HMemAddr_OFace;
        #endregion
        public bool DelayBranchEnabled
        {
            get;
            private set;
        }

        public int[] InstructionCountPerType = new int[2];

        public FetchStage(bool delayBranchEnabled)
        {
            DelayBranchEnabled = delayBranchEnabled;

            Stall_IFace = CreateInputface<bool>();
            LMemRdata_IFace = CreateInputface<ushort>();
            HMemRdata_IFace = CreateInputface<ushort>();
            BranchRequestFromCS_IFace = CreateInputface<bool>();
            BranchPCRelModeFromCS_IFace = CreateInputface<bool>();
            BranchPCFromCS_IFace = CreateInputface<uint>();

            PCOut_OFace = CreateSyncOutputface<uint>(0);
            Instruction_OFace = CreateSyncOutputface<uint>(0);
            ValidCS_OFace = CreateSyncOutputface<bool>(false);
            Branched_OFace = CreateSyncOutputface<bool>(false);

            LMemEn_OFace = CreateAsyncOutputface<bool>();
            LMemAddr_OFace = CreateAsyncOutputface<uint>();
            HMemEn_OFace = CreateAsyncOutputface<bool>();
            HMemAddr_OFace = CreateAsyncOutputface<uint>();

            LMemEn_OFace.SetFunc(() =>
            {
                return !Stall_IFace;
            });
            LMemAddr_OFace.SetFunc(() =>
            {
                bool isPCRelMemInstruction =
                    TestBit(Instruction_OFace.Value, 31) &&
                    TestBit(Instruction_OFace.Value, 24) &&
                    TestBit(Instruction_OFace.Value, 22);
                bool isJumpableInstruction =
                    !TestBit(Instruction_OFace.Value, 31) &&
                    TestBit(Instruction_OFace.Value, 29);

                uint res = PCOut_OFace.Value >> 1;
                if (Branched_OFace.Value)
                {
                    res += 0;
                }
                else if (isPCRelMemInstruction && TestBit(PCOut_OFace.Value, 0))
                    res += 2;
                else if (isPCRelMemInstruction || (isJumpableInstruction && TestBit(PCOut_OFace.Value, 0)))
                    res += 1;

                return res;
            });
            HMemEn_OFace.SetFunc(() =>
            {
                return !Stall_IFace;
            });
            HMemAddr_OFace.SetFunc(() =>
            {
                bool isPCRelMemInstruction =
                    TestBit(Instruction_OFace.Value, 31) &&
                    TestBit(Instruction_OFace.Value, 24) &&
                    TestBit(Instruction_OFace.Value, 22);
                bool isJumpableInstruction =
                    !TestBit(Instruction_OFace.Value, 31) &&
                    TestBit(Instruction_OFace.Value, 29);

                uint res = PCOut_OFace.Value >> 1;
                if (Branched_OFace.Value)
                {
                    if (TestBit(PCOut_OFace.Value, 0))
                        res += 1;
                    else
                        res += 0;
                }
                else if (isPCRelMemInstruction && TestBit(PCOut_OFace.Value, 0))
                    res += 2;
                else if (isPCRelMemInstruction || isJumpableInstruction || TestBit(PCOut_OFace.Value, 0))
                    res += 1;
                
                return res;
            });
        }

        protected override void UpdateModuleCycle()
        {
            bool isPCRelMemInstruction =
               TestBit(Instruction_OFace.Value, 31) &&
               TestBit(Instruction_OFace.Value, 24) &&
               TestBit(Instruction_OFace.Value, 22);
            bool isJumpableInstruction =
                !TestBit(Instruction_OFace.
              Value, 31) &&
                TestBit(Instruction_OFace.Value, 29);
            
            { //Instruction
                if (!Stall_IFace)
                {
                    InstructionCountPerType[TestBit(Instruction_OFace.Value, 31) || isJumpableInstruction ? 1 : 0]++;

                    if (isPCRelMemInstruction)
                        Instruction_OFace.Assign((((uint)HMemRdata_IFace << 16) & 0xFFFF0000) | ((uint)LMemRdata_IFace & 0xFFFF));
                    else
                    {
                        if (TestBit(PCOut_OFace.Value, 0) ^ (isJumpableInstruction && !Branched_OFace.Value))
                            Instruction_OFace.Assign((((uint)LMemRdata_IFace << 16) & 0xFFFF0000) | ((uint)HMemRdata_IFace & 0xFFFF));
                        else
                            Instruction_OFace.Assign((((uint)HMemRdata_IFace << 16) & 0xFFFF0000) | ((uint)LMemRdata_IFace & 0xFFFF));
                    }
                }
            }
            { //PC
                if (!Stall_IFace)
                {
                    if (Branched_OFace.Value)
                    {
                        //分岐先の命令をフェッチ完了した場合
                        PCOut_OFace.Assign((PCOut_OFace.Value) + 1);
                        ValidCS_OFace.Assign(true);
                        Branched_OFace.Assign(false);
                    }
                    else if (isPCRelMemInstruction)
                    {
                        //ComputeステージでPC相対メモリアクセス命令を実行中の場合
                        if (TestBit(PCOut_OFace.Value, 0))
                            PCOut_OFace.Assign((PCOut_OFace.Value & 0xFFFFFFFE) + 4 + 1);
                        else
                            PCOut_OFace.Assign((PCOut_OFace.Value & 0xFFFFFFFE) + 2 + 1);
                        ValidCS_OFace.Assign(true);
                        Branched_OFace.Assign(false); 
                    }
                    else if (BranchRequestFromCS_IFace)
                    {
                        //Computeステージから分岐成立が伝えられた場合
                        //=>次のPCを分岐先に設定  ※今のサイクルは遅延分岐スロットをフェッチしている
                        if (BranchPCRelModeFromCS_IFace)
                            PCOut_OFace.Assign(PCOut_OFace.Value + BranchPCFromCS_IFace);
                        else
                            PCOut_OFace.Assign(BranchPCFromCS_IFace);

                        //遅延分岐スロットが無効なら、今フェッチした命令は無効にする
                        ValidCS_OFace.Assign(DelayBranchEnabled);

                        //分岐することをマークする => 次は「if (Branched_OFace.Value)」に入る
                        Branched_OFace.Assign(true);
                    }
                    else if (isJumpableInstruction)
                    {
                        //Computeステージで条件分岐命令を実行中の場合
                        PCOut_OFace.Assign(PCOut_OFace.Value + 2);
                        ValidCS_OFace.Assign(true);
                        Branched_OFace.Assign(false);
                    }
                    else
                    {
                        PCOut_OFace.Assign((PCOut_OFace.Value) + 1);
                        ValidCS_OFace.Assign(true);
                        Branched_OFace.Assign(false);
                    }
                }
            }

            base.UpdateModuleCycle();
        }
    }
}

//          ComputeステージのPC
//0: sub    1
//1: sub    2
//2: sub    3

//          ComputeステージのPC
//0: sub -< 1
//2: sub -< 3
//4: sub    5
//5: sub    6

//          ComputeステージのPC
//0: sub -< 1
//2: sub -< 3
//4: sub    5
//5: sub    6
