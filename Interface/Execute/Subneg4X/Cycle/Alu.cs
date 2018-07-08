using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.Subneg4X
{
    public class Alu : SyncModuleBase
    {
        #region 同期入力
        public ModuleInputface<uint> OperandA_IFace;
        public ModuleInputface<uint> OperandB_IFace;
        public ModuleInputface<uint> OperandD_IFace;
        #endregion
        #region 同期出力
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<uint> AluResult_OFace;
        public AsyncModuleOutputface<bool> BranchCond_OFace;
        #endregion

        public Alu()
        {
            OperandA_IFace = CreateInputface<uint>();
            OperandB_IFace = CreateInputface<uint>();
            OperandD_IFace = CreateInputface<uint>();

            AluResult_OFace = CreateAsyncOutputface<uint>();
            BranchCond_OFace = CreateAsyncOutputface<bool>();

            AluResult_OFace.SetFunc(() =>
            {
                bool opcode = ((OperandD_IFace & 0x80000000) != 0);
                if (!opcode)
                { //subneg
                    uint res;
                    bool cond;
                    ComputeSubneg(OperandA_IFace,OperandB_IFace,out res,out cond);

                    return res;
                }
                else
                { //subnegx
                    uint res;
                    bool cond;
                    ComputeSubnegX(OperandA_IFace,OperandB_IFace,out res,out cond);

                    return res;
                }
            });
            BranchCond_OFace.SetFunc(() =>
            {
                bool opcode = ((OperandD_IFace & 0x80000000) != 0);
                if (!opcode)
                { //subneg
                    uint res;
                    bool cond;
                    ComputeSubneg(OperandA_IFace,OperandB_IFace,out res,out cond);

                    return cond;
                }
                else
                { //subnegx
                    uint res;
                    bool cond;
                    ComputeSubnegX(OperandA_IFace,OperandB_IFace,out res,out cond);

                    return cond;
                }
            });
        }

        protected override void UpdateModuleCycle()
        {
        }

        public static void ComputeSubneg(uint operandA,uint operandB,out uint result,out bool condition)
        {
            result = operandB - operandA;
            condition = (result & 0x80000000) != 0;
        }

        public static void ComputeSubnegX(uint operandA,uint operandB,out uint result,out bool condition)
        {
            uint msb = operandB < operandA ? 0x80000000 : 0x00000000;
            result = ((operandB & operandA) >> 1) | msb;
            condition = ((operandB & operandA) & 0x00000001) == 0;
        }
    }
}
