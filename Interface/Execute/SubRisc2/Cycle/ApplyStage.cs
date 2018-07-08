using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.SubRISC2
{
    public class ApplyStage : SyncModuleBase
    {
        #region 同期入力
        public ModuleInputface<bool> MemOp_IFace;
        public ModuleInputface<bool> MemRw_IFace;
        public ModuleInputface<uint> AluRes_IFace;
        public ModuleInputface<uint> RegRdataC_IFace;
        public ModuleInputface<byte> RegNoC_IFace;
        public ModuleInputface<bool> ValidAS_IFace;
        public ModuleInputface<uint> MemRdata_IFace;
        #endregion
        #region 同期出力
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<bool> MemoryStallRequest_OFace;
        public AsyncModuleOutputface<bool> MemEn_OFace;
        public AsyncModuleOutputface<uint> MemAddr_OFace;
        public AsyncModuleOutputface<bool> MemWen_OFace;
        public AsyncModuleOutputface<uint> MemWdata_OFace;
        public AsyncModuleOutputface<bool> RegWen_OFace;
        public AsyncModuleOutputface<uint> RegWdata_OFace;
        public AsyncModuleOutputface<byte> RegWno_OFace;
        #endregion
        SyncModuleOutputface<bool> memReaded;

        public ApplyStage()
        {
            MemOp_IFace = CreateInputface<bool>();
            MemRw_IFace = CreateInputface<bool>();
            AluRes_IFace = CreateInputface<uint>();
            RegRdataC_IFace = CreateInputface<uint>();
            RegNoC_IFace = CreateInputface<byte>();
            ValidAS_IFace = CreateInputface<bool>();
            MemRdata_IFace = CreateInputface<uint>();

            MemoryStallRequest_OFace = CreateAsyncOutputface<bool>();
            MemEn_OFace = CreateAsyncOutputface<bool>();
            MemAddr_OFace = CreateAsyncOutputface<uint>();
            MemWen_OFace = CreateAsyncOutputface<bool>();
            MemWdata_OFace = CreateAsyncOutputface<uint>();
            RegWen_OFace = CreateAsyncOutputface<bool>();
            RegWdata_OFace = CreateAsyncOutputface<uint>();
            RegWno_OFace = CreateAsyncOutputface<byte>();

            memReaded = CreateSyncOutputface<bool>(false);

            MemoryStallRequest_OFace.SetFunc(() =>
            {
                return ValidAS_IFace && !memReaded.Value && MemOp_IFace;
            });
            MemEn_OFace.SetFunc(() =>
            {
                return !memReaded.Value;
            });
            MemAddr_OFace.SetFunc(() =>
            {
                return AluRes_IFace;// >> 2;
            });
            MemWen_OFace.SetFunc(() =>
            {
                return !memReaded.Value && MemRw_IFace;
            });
            MemWdata_OFace.SetFunc(() =>
            {
                return RegRdataC_IFace;
            });
            RegWen_OFace.SetFunc(() =>
            {
                return ValidAS_IFace && (!MemOp_IFace || (!MemRw_IFace && !memReaded.Value));
            });
            RegWdata_OFace.SetFunc(() =>
            {
                return MemOp_IFace ? MemRdata_IFace : AluRes_IFace;
            });
            RegWno_OFace.SetFunc(() =>
            {
                return RegNoC_IFace;
            });
        }

        protected override void UpdateModuleCycle()
        {
            { //memReaded
                if (ValidAS_IFace && MemOp_IFace && !memReaded.Value)
                    memReaded.Assign(true);
                else
                    memReaded.Assign(false);
            }
            base.UpdateModuleCycle();
        }
    }
}
