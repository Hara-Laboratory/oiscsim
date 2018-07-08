using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.SubRISC
{
    public class RegisterFile : SyncModuleBase
    {
        public const int EntryCount = 16;
        public struct EntryElement
        {
            public uint Content;
            public long ReadAccessCount;
            public long WriteAccessCount;
        }
        public EntryElement[] Entries;
        public long CycleCount = 0;

        public struct ReadCommand
        {
            public bool Enabled;
            public int No;
        }
        public struct WriteCommand
        {
            public bool Enabled;
            public int No;
            public uint Value;
        }
        #region 同期入力
        public ModuleInputface<ReadCommand> Read0_IFace;
        public ModuleInputface<ReadCommand> Read1_IFace;
        public ModuleInputface<WriteCommand> Write_IFace;
        #endregion
        #region 同期出力
        private ReadCommand PreviousRead0;
        private ReadCommand PreviousRead1;
        private WriteCommand PreviousWrite;
        #endregion
        #region 即時出力
        public AsyncModuleOutputface<uint> Read0_OFace;
        public AsyncModuleOutputface<uint> Read1_OFace;
        #endregion

        public RegisterFile()
        {
            Entries = new SubRISC.RegisterFile.EntryElement[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                Entries[i] = new EntryElement()
                {
                    Content = 0,
                    ReadAccessCount = 0,
                    WriteAccessCount = 0
                };
            }

            Read0_IFace = CreateInputface<ReadCommand>();
            Read1_IFace = CreateInputface<ReadCommand>();
            Write_IFace = CreateInputface<WriteCommand>();

            Read0_OFace = CreateAsyncOutputface<uint>();
            Read1_OFace = CreateAsyncOutputface<uint>();

            PreviousRead0 = new SubRISC.RegisterFile.ReadCommand()
            {
                Enabled = false,
                No = -1
            };
            PreviousRead1 = new SubRISC.RegisterFile.ReadCommand()
            {
                Enabled = false,
                No = -1
            };

            Read0_OFace.SetFunc(() =>
            {
                ReadCommand rCmd = Read0_IFace.Get();
                WriteCommand wCmd = Write_IFace.Get();
                int no = rCmd.No % EntryCount;
                return wCmd.Enabled && wCmd.No == no ? wCmd.Value
                                     : Entries[no].Content;
            });
            Read1_OFace.SetFunc(() =>
            {
                ReadCommand rCmd = Read1_IFace.Get();
                WriteCommand wCmd = Write_IFace.Get();
                int no = rCmd.No % EntryCount;
                return wCmd.Enabled && wCmd.No == no ? wCmd.Value
                                     : Entries[no].Content;
            });
        }

        protected override void UpdateModuleCycle()
        {
            this.CycleCount++;
            { //Perform writing
                WriteCommand wCmd = Write_IFace.Get();
                int no = wCmd.No % EntryCount;
                if (wCmd.Enabled)
                {
                    Entries[no].Content = wCmd.Value;
                    Entries[no].WriteAccessCount++;
                }
                PreviousWrite = wCmd;
            }

            { //Count reading
                ReadCommand rCmd = Read0_IFace.Get();
                WriteCommand wCmd = Write_IFace.Get();
                int no = rCmd.No % EntryCount;
                if (rCmd.Enabled && no != PreviousRead0.No && !(wCmd.Enabled && wCmd.No == no))
                    Entries[no].ReadAccessCount++;
                PreviousRead0 = rCmd;
            }
            { //Count reading
                ReadCommand rCmd = Read1_IFace.Get();
                WriteCommand wCmd = Write_IFace.Get();
                int no = rCmd.No % EntryCount;
                if (rCmd.Enabled && no != PreviousRead1.No && !(wCmd.Enabled && wCmd.No == no))
                    Entries[no].ReadAccessCount++;
                PreviousRead1 = rCmd;
            }

            base.UpdateModuleCycle();
        }

        public string GetStatisticsInfo()
        {
            long readAccessCount = 0, writeAccessCount = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                readAccessCount += Entries[i].ReadAccessCount;
                writeAccessCount += Entries[i].WriteAccessCount;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Whole]\r\n" +
                   $"Read rate = { ((double)readAccessCount / CycleCount * 100).ToString("0.00") } %\r\n" +
                   $"Write rate = { ((double)writeAccessCount / CycleCount * 100).ToString("0.00") } %");
            sb.AppendLine($"[Per entry]");
            for (int i = 0; i < EntryCount; i++)
            {
                sb.AppendLine($"{i.ToString("00")}: Read rate = { ((double)Entries[i].ReadAccessCount / CycleCount * 100).ToString("0.00") } %, Write rate = { ((double)Entries[i].WriteAccessCount / CycleCount * 100).ToString("0.00") } %");
            }
            return sb.ToString();
        }
    }
}
