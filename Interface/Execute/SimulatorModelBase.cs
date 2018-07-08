using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Interface.Execute.ExecuteSetupData;

namespace Interface.Execute
{
    public abstract class SimulatorModelBase
    {
        protected struct ExecutionTraceElement
        {
            public long Cycle;
        }
        protected ExecutionTraceElement[] ExecutionTraceData;
        protected static char[] DensityLetter = new char[] { ' ', '.', '-', '=', '#', '@' };
        protected long PreviousUpdateCycle = 0;
        protected void InitializeExecutionTraceData(ExecuteSetupData setupData, int slot)
        {
            int size = setupData.MemoryContents[slot].CodeSize - 1;
            while (size > 0)
            {
                if (setupData.MemoryContents[slot].GetDebugInfo(size, 1, 0).Usage == MemoryContent.WordElement.enumUsage.Instruction)
                    break;
                size--;
            }
            size += 1;
            while (size < setupData.MemoryContents[slot].CodeSize)
            {
                if (setupData.MemoryContents[slot].GetDebugInfo(size, 1, 0).Usage != MemoryContent.WordElement.enumUsage.FollowHead)
                    break;
                size++;
            }

            ExecutionTraceData = new ExecutionTraceElement[size * 2];
        }
        protected void MarkExecutionTraceData(int halfAddr, long cycle)
        {
            if (halfAddr < 0 || halfAddr >= ExecutionTraceData.Length)
                return;
            ExecutionTraceData[halfAddr].Cycle = cycle;
        }
        public virtual string PrintExecutionTraceData(int length)
        {
            int interval = ExecutionTraceData.Length / length;
            long updateTime = Math.Max(1, (long)CycleCount - PreviousUpdateCycle);
            PreviousUpdateCycle = (long)CycleCount;
            Func<long, double> computeDensity = (c) =>
            {
                long elapsedCycle = Math.Max(0, (long)CycleCount - c);
                double elapsedRate = 0 + (double)elapsedCycle / (double)updateTime; //Latest=0, updateTime=2
                
                return Math.Pow(0.4, elapsedRate);
            };

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length - 1; i++)
            {
                int from = i * interval;
                int to = i * interval+ interval - 1;

                long maxCycle = 0;
                for (int sbi = from; sbi <= to; sbi++)
                    maxCycle = Math.Max(maxCycle, ExecutionTraceData[sbi].Cycle);
                double density = computeDensity(maxCycle);
                sb.Append(DensityLetter[(int)(density * (DensityLetter.Length - 1))]);
            }
            {
                int from = (length - 1) * interval;
                int to = length - 1;

                long maxCycle = 0;
                for (int sbi = from; sbi <= to; sbi++)
                    maxCycle = Math.Max(maxCycle, ExecutionTraceData[sbi].Cycle);
                double density = computeDensity(maxCycle);
                sb.Append(DensityLetter[(int)(density * (DensityLetter.Length - 1))]);
            }
            return sb.ToString();
        }

        public bool IsHalted
        {
            get;
            protected set;
        }
        public ulong CycleCount
        {
            get;
            protected set;
        }

        public SimulatorModelBase()
        {
            CycleCount = 1;
        }
        
        public abstract bool SetupFromSetupData(ExecuteSetupData setupData);

        public abstract bool SetupFromAssembly(Assemble.AssemblyCode code);

        public abstract bool StepCycle();

        public abstract bool ShowMemoryDumpByMessage(bool codeInstr, bool codeVar, bool stack,enumMessageLevel level);

        public abstract bool SaveMemoryDump(System.IO.Stream s);

        public abstract bool ShowExecutionInfo(enumMessageLevel level);

        private static uint ApplyByteMask(uint value,int pos)
        {
            switch (pos)
            {
                case 0:
                    return (value & 0xFF000000) >> 24;
                case 1:
                    return (value & 0xFF0000) >> 16;
                case 2:
                    return (value & 0xFF00) >> 8;
                case 3:
                    return (value & 0xFF);
            }
            return 0;
        }

        protected bool ShowMemoryDumpByMessage(ExecuteSetupData.MemoryContent setupMemoryContent,CommonModule.RAM memory,bool codeInstr, bool codeVar,bool stack,enumMessageLevel level)
        {
            if (!codeInstr && !codeVar && !stack)
            {
                return false;
            }
            {
                var mem = setupMemoryContent;
                const int contentTextWidth = 40;
                MessageManager.ShowLine($"ByteAddress     { "Raw".PadRight(contentTextWidth,' ') } Mean",level);
                if (codeInstr || codeVar)
                {
                    for (int memByteAddrHead = 0; memByteAddrHead < mem.CodeSize * 4; memByteAddrHead++)
                    {
                        int memByteAddrTail = memByteAddrHead;
                        for (int cursor = memByteAddrHead + 1; cursor < mem.CodeSize * 4; cursor++)
                        {
                            if (mem.GetDebugInfo(cursor / 4,4,cursor % 4).Usage != MemoryContent.WordElement.enumUsage.FollowHead)
                                break;

                            memByteAddrTail = cursor;
                        }

                        //Statistics
                        string statisticsText = "";

                        //Content
                        string contentText = "";
                        var usageType = mem.GetDebugInfo(memByteAddrHead / 4, 4, memByteAddrHead % 4).Usage;
                        switch (usageType)
                        {
                            case MemoryContent.WordElement.enumUsage.Variable:
                            case MemoryContent.WordElement.enumUsage.VariableArray:
                            case MemoryContent.WordElement.enumUsage.FollowHead:
                                if (!codeVar)
                                {
                                    memByteAddrHead = memByteAddrTail;
                                    continue;
                                }

                                memByteAddrTail = memByteAddrHead + 3; //Disabling group display
                                contentText = $"0x{ memory[memByteAddrHead / 4].ToString("X8") } ({ (int)memory[memByteAddrHead / 4]})";

                                {
                                    int wordReadTimes, wordWriteTimes, execTimes;
                                    int[] wordReadTimesPerType;
                                    memory.GetWordAccessStatistics((uint)memByteAddrHead / 4, 4, memByteAddrHead % 4, out execTimes, out wordReadTimes, out wordWriteTimes, out wordReadTimesPerType);
                                    statisticsText = $"\r\n        #Access: RI={wordReadTimesPerType[(int)CommonModule.EnumMemorymAccessType.Instruction]}, RD={wordReadTimesPerType[(int)CommonModule.EnumMemorymAccessType.Data]}; W={wordWriteTimes}";
                                    statisticsText += "\r\n";
                                }
                                break;
                            case MemoryContent.WordElement.enumUsage.Unknown:
                            default:
                                if (memByteAddrHead >= setupMemoryContent.CodeSize)
                                {
                                    contentText = $"0x{ memory[memByteAddrHead / 4].ToString("X8") } ({ (int)memory[memByteAddrHead / 4]})";
                                }
                                else
                                    contentText = "(Alignment)";
                                statisticsText += "\r\n";
                                break;
                            case MemoryContent.WordElement.enumUsage.Instruction:
                                if (!codeInstr)
                                {
                                    memByteAddrHead = memByteAddrTail;
                                    continue;
                                }

                                contentText = "";
                                for (int i = 0; i < memByteAddrTail - memByteAddrHead + 1; i++)
                                {
                                    if ((memByteAddrHead + i) % 4 == 0 || (mem[(memByteAddrHead + i) / 4].DebugInfoCount >= 2 && (memByteAddrHead + i) % 2 == 0))
                                        contentText += " ";
                                    contentText += "" + ApplyByteMask(memory[(memByteAddrHead + i) / 4],(memByteAddrHead + i) % 4).ToString("X2");
                                }

                                contentText = contentText.TrimStart(' ');
                                {
                                    int wordReadTimes, wordWriteTimes, execTimes;
                                    int[] wordReadTimesPerType;
                                    memory.GetWordAccessStatistics((uint)memByteAddrHead / 4, 4, memByteAddrHead%4, out execTimes, out wordReadTimes, out wordWriteTimes, out wordReadTimesPerType);
                                    statisticsText = $"\r\n        #Access: EX={execTimes}, RI={wordReadTimesPerType[(int)CommonModule.EnumMemorymAccessType.Instruction]}, RD={wordReadTimesPerType[(int)CommonModule.EnumMemorymAccessType.Data]}; W={wordWriteTimes}";
                                    statisticsText += "\r\n";
                                }
                                break;
                        }

                        //Start address
                        if (memByteAddrHead == memByteAddrTail)
                            MessageManager.Show($"{ memByteAddrHead.ToString("X8") }:       ",level);
                        else
                            MessageManager.Show($"{ memByteAddrHead.ToString("X8") }~[+{((memByteAddrTail - memByteAddrHead).ToString() + "]:").PadRight(5,' ') }",level);

                        //Contents
                        contentText = contentText.PadRight(contentTextWidth,' ');
                        MessageManager.Show(
                            $"{ contentText } { mem.GetDebugInfo(memByteAddrHead / 4,4,memByteAddrHead % 4).DebugInfo?.PadRight(usageType == MemoryContent.WordElement.enumUsage.Instruction ? 70 : 0) }",level);

                        MessageManager.Show(statisticsText, level);


                        memByteAddrHead = memByteAddrTail;
                    }
                }
                if (stack)
                {
                    for (int memByteAddrHead = mem.CodeSize * 4; memByteAddrHead < mem.WordCapacity * 4; memByteAddrHead++)
                    {
                        int memByteAddrTail = memByteAddrHead;
                        for (int cursor = memByteAddrHead + 1; cursor < mem.WordCapacity * 4; cursor++)
                        {
                            if (mem.GetDebugInfo(cursor / 4,4,cursor % 4).Usage != MemoryContent.WordElement.enumUsage.FollowHead)
                                break;

                            memByteAddrTail = cursor;
                        }

                        //Content
                        string contentText;
                        switch (mem.GetDebugInfo(memByteAddrHead / 4,4,memByteAddrHead % 4).Usage)
                        {
                            case MemoryContent.WordElement.enumUsage.Variable:
                            case MemoryContent.WordElement.enumUsage.VariableArray:
                            case MemoryContent.WordElement.enumUsage.FollowHead:
                                memByteAddrTail = memByteAddrHead + 3; //Disabling group display
                                contentText = $"0x{ memory[memByteAddrHead / 4].ToString("X8") } ({ (int)memory[memByteAddrHead / 4]})";
                                break;
                            case MemoryContent.WordElement.enumUsage.Unknown:
                            default:
                                if (memByteAddrHead >= setupMemoryContent.CodeSize)
                                {
                                    contentText = $"0x{ memory[memByteAddrHead / 4].ToString("X8") } ({ (int)memory[memByteAddrHead / 4]})";
                                }
                                else
                                    contentText = "(Alignment)";
                                break;
                            case MemoryContent.WordElement.enumUsage.Instruction:
                                contentText = "";
                                for (int i = 0; i < memByteAddrTail - memByteAddrHead + 1; i++)
                                {
                                    if ((memByteAddrHead + i) % 4 == 0 || (mem[(memByteAddrHead + i) / 4].DebugInfoCount >= 2 && (memByteAddrHead + i) % 2 == 0))
                                        contentText += " ";
                                    contentText += "" + ApplyByteMask(memory[(memByteAddrHead + i) / 4],(memByteAddrHead + i) % 4).ToString("X2");
                                }
                                contentText = contentText.TrimStart(' ');
                                break;
                        }

                        //Start address
                        if (memByteAddrHead == memByteAddrTail)
                            MessageManager.Show($"{ memByteAddrHead.ToString("X8") }:       ",level);
                        else
                            MessageManager.Show($"{ memByteAddrHead.ToString("X8") }~[+{((memByteAddrTail - memByteAddrHead).ToString() + "]:").PadRight(5,' ') }",level);

                        //Contents
                        contentText = contentText.PadRight(contentTextWidth,' ');
                        MessageManager.ShowLine(
                            $"{ contentText } { mem.GetDebugInfo(memByteAddrHead / 4,4,memByteAddrHead % 4).DebugInfo }",level);

                        memByteAddrHead = memByteAddrTail;
                    }
                }
                MessageManager.ShowLine($"-----------------------------------------------------------------------",level);
                MessageManager.ShowLine("",level);
            }
            return true;
        }

        protected bool SaveMemoryDump(ExecuteSetupData.MemoryContent setupMemoryContent, CommonModule.RAM memory, System.IO.Stream s)
        {
            byte[] buf;
            for (int i = 0; i < memory.WordCapacity; i++)
            {
                uint word = memory[i];
                buf = BitConverter.GetBytes(word);

                s.Write(buf, 0, 4);
            }
            return true;
        }
    }
}
