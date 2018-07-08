using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.CommonModule
{
    public class SyncRAMInterfaceRW1R1 : SyncModuleBase
    {
        public RAM RAM
        {
            get;
            private set;
        }
        public struct ReadCommand
        {
            public bool Enabled;
            public uint Address;
            public byte ByteMask;
            public EnumMemorymAccessType AccessType;
        }
        public struct WriteCommand
        {
            public bool Enabled;
            public uint Address;
            public uint Value;
            public byte ByteMask;
        }
        #region 入力
        public ModuleInputface<ReadCommand> ReadCmd1_IFace;
        public ModuleInputface<ReadCommand> ReadCmd2_IFace;
        public ModuleInputface<WriteCommand> WriteCmd_IFace;
        #endregion
        #region 出力
        public SyncModuleOutputface<uint> ReadValue1_OFace;
        public SyncModuleOutputface<uint> ReadValue2_OFace;
        #endregion
        #region 統計
        public long Port1ReadCycleCount = 0;
        public long Port1WriteCycleCount = 0;
        public long Port1CycleCount = 0;
        public long Port2ReadCycleCount = 0;
        public long Port2CycleCount = 0;
        #endregion

        public SyncRAMInterfaceRW1R1(RAM ram)
        {
            this.RAM = ram;

            ReadCmd1_IFace = CreateInputface<ReadCommand>();
            ReadCmd2_IFace = CreateInputface<ReadCommand>();
            WriteCmd_IFace = CreateInputface<WriteCommand>();

            ReadValue1_OFace = CreateSyncOutputface<uint>();
            ReadValue2_OFace = CreateSyncOutputface<uint>();
        }

        protected override void UpdateModuleCycle()
        {
            uint res;
            if (ProcessReadCmd(ReadCmd1_IFace, 0,out res))
                ReadValue1_OFace.Assign(res);

            ProcessWriteCmd(WriteCmd_IFace, 0);
            Port1CycleCount++;

            if (ProcessReadCmd(ReadCmd2_IFace, 1,out res))
                ReadValue2_OFace.Assign(res);
            Port2CycleCount++;

            base.UpdateModuleCycle();
        }

        private bool ProcessReadCmd(ReadCommand cmd, int port,out uint res)
        {
            res = 0;
            if (!cmd.Enabled)
            {
                return false;
            }

            switch (port)
            {
                case 0:
                    Port1ReadCycleCount++;
                    break;
                case 1:
                    Port2ReadCycleCount++;
                    break;
            }
            this.RAM.LoadWord(cmd.Address,out res, cmd.AccessType);
            return true;
        }

        private void ProcessWriteCmd(WriteCommand cmd, int port)
        {
            if (!cmd.Enabled)
            {
                return;
            }

            switch (port)
            {
                case 0:
                    Port1WriteCycleCount++;
                    break;
            }
            this.RAM.StoreWord(cmd.Address,cmd.Value);
        }

        public string GetStatisticsInfo()
        {
            return $"[Port1]\n" +
                   $"Read rate = { ((double)Port1ReadCycleCount / Port1CycleCount * 100).ToString("0.00") } %\n" +
                   $"Write rate = { ((double)Port1WriteCycleCount / Port1CycleCount * 100).ToString("0.00") } %\n" + 
                   $"[Port2]\n" +
                   $"Read rate = { ((double)Port2ReadCycleCount / Port2CycleCount * 100).ToString("0.00") } %\n";
        }
    }
}
