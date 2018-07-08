using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.SubRISC2
{
    public class Alu : SyncModuleBase
    {
        #region 同期入力
        public ModuleInputface<uint> OperandA_IFace;
        public ModuleInputface<uint> OperandB_IFace;
        public ModuleInputface<bool> OpFlag_IFace;
        public ModuleInputface<byte> CondFlag_IFace;
        #endregion
        #region 同期出力
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<uint> OpResult_OFace;
        public AsyncModuleOutputface<bool> CondResult_OFace;
        #endregion

        public Alu()
        {
            OperandA_IFace = CreateInputface<uint>();
            OperandB_IFace = CreateInputface<uint>();
            OpFlag_IFace = CreateInputface<bool>();
            CondFlag_IFace = CreateInputface<byte>();

            OpResult_OFace = CreateAsyncOutputface<uint>();
            CondResult_OFace = CreateAsyncOutputface<bool>();

            OpResult_OFace.SetFunc(() =>
            {
                bool opcode = OpFlag_IFace;
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
            CondResult_OFace.SetFunc(() =>
            {
                byte opcode = CondFlag_IFace;
                if ((opcode & 1) != 0)
                { //!cout
                    ulong opa = (ulong)OperandA_IFace;
                    ulong opb = (ulong)OperandB_IFace;
                    ulong sub = opb - opa;

                    return !((sub & 0x100000000) != 0); //has no carry (carry bit == 0)
                }
                else if ((opcode & 4) != 0)
                { //subneg
                    uint res;
                    bool cond;
                    ComputeSubneg(OperandA_IFace, OperandB_IFace, out res, out cond);

                    return cond;
                }
                else
                { //subnegx
                    uint res;
                    bool cond;
                    ComputeSubnegX(OperandA_IFace, OperandB_IFace, out res, out cond);

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
            result = operandB & operandA;//((operandB & operandA) >> 1) | msb; //
            condition = ((operandB & operandA) & 0x00000001) == 0;
        }
    }
}
