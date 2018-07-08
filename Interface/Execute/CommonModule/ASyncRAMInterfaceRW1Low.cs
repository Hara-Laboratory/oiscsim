using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.CommonModule
{
    public class ASyncRAMInterfaceRW1Low : SyncModuleBase
    {
        public RAM RAM
        {
            get;
            private set;
        }
        public struct ReadCommand
        {
            public bool Enabled;
            public int Address;
            public EnumMemorymAccessType AccessType;
        }
        public struct WriteCommand
        {
            public bool Enabled;
            public int Address;
            public ushort Value;
        }
        #region 入力
        public ModuleInputface<ReadCommand> Read_IFace;
        public ModuleInputface<WriteCommand> Write_IFace;
        #endregion
        private ReadCommand PreviousRead;
        private WriteCommand PreviousWrite;
        #region 出力
        public AsyncModuleOutputface<ushort> Read_OFace;
        #endregion
        #region 統計
        public long Port0ReadCycleCount = 0;
        public long Port0WriteCycleCount = 0;
        public long Port0CycleCount = 0;
        public int[] ReadAccessCycleTypes = new int[(int)EnumMemorymAccessType.Count];
        #endregion

        public ASyncRAMInterfaceRW1Low(RAM ram)
        {
            this.RAM = ram;

            Read_IFace = CreateInputface<ReadCommand>();
            Write_IFace = CreateInputface<WriteCommand>();

            Read_OFace = CreateAsyncOutputface<ushort>();

            PreviousRead = new ReadCommand()
            {
                Enabled = false,
                Address = -1
            };

            Read_OFace.SetFunc(() =>
            {
                ReadCommand rCmd = Read_IFace.Get();
                uint res;
                if (rCmd.Enabled)
                    this.RAM.LoadWord(
                        (uint)rCmd.Address, out res,
                        rCmd.Address != PreviousRead.Address ? rCmd.AccessType : EnumMemorymAccessType.No, 2, 1);
                else
                    res = 0;
                return (ushort)(res & 0xFFFF);
            });
        }

        protected override void UpdateModuleCycle()
        {
            this.Port0CycleCount++;
            { //Perform writing
                WriteCommand wCmd = Write_IFace.Get();
                if (wCmd.Enabled)
                {
                    uint loaded;
                    this.RAM.LoadWord((uint)wCmd.Address, out loaded, EnumMemorymAccessType.No);
                    this.RAM.StoreWord((uint)wCmd.Address, (loaded & 0xFFFF0000) | ((uint)wCmd.Value & 0xFFFF), 2, 1);
                    this.Port0WriteCycleCount++;
                }
                PreviousWrite = wCmd;
            }

            { //Count reading
                ReadCommand rCmd = Read_IFace.Get();
                if (rCmd.Enabled && rCmd.Address != PreviousRead.Address)
                {
                    this.Port0ReadCycleCount++;
                    if (rCmd.AccessType != EnumMemorymAccessType.No)
                        ReadAccessCycleTypes[(int)rCmd.AccessType]++;
                }
                PreviousRead = rCmd;
            }

            base.UpdateModuleCycle();
        }

        public string GetStatisticsInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Read rate = { ((double)Port0ReadCycleCount / Port0CycleCount * 100).ToString("0.00") } %");
            for (int i = 0; i < (int)EnumMemorymAccessType.Count; i++)
            {
                sb.AppendLine($"   -{Enum.GetName(typeof(EnumMemorymAccessType), i)}: {((double)ReadAccessCycleTypes[i] / Port0CycleCount * 100).ToString("0.00")}% ({((double)ReadAccessCycleTypes[i] / Port0ReadCycleCount * 100).ToString("0.00")}% for read)");
            }
            sb.AppendLine($"Write rate = { ((double)Port0WriteCycleCount / Port0CycleCount * 100).ToString("0.00") } %");
            return sb.ToString();
        }
    }
}
