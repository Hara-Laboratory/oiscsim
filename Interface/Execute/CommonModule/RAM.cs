using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute.CommonModule
{
    public class RAM
    {
        public struct DebugInfoElements
        {
            public DebugInfoElement Element0;
            public DebugInfoElement Element1;
            public DebugInfoElement Element2;
            public DebugInfoElement Element3;
            public DebugInfoElement this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return Element0;
                        case 1:
                            return Element1;
                        case 2:
                            return Element2;
                        case 3:
                            return Element3;
                    }
                    return default(DebugInfoElement);
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            Element0 = value;
                            break;
                        case 1:
                            Element1 = value;
                            break;
                        case 2:
                            Element2 = value;
                            break;
                        case 3:
                            Element3 = value;
                            break;
                    }
                }
            }
        }
        public class DebugInfoElement
        {
            public ExecuteSetupData.MemoryContent.WordElement.enumUsage Usage;

            public string DebugInfo;
            public bool IsDebugMarked; //命令であれば実行されるたびに，変数領域であれば読み書きされるたびに 命令の場合は先頭の領域にマークするかな？
            public string DebugDisplayText; //余裕があればここでも文法を使えるようにする

            public int ExecuteCycle;
            public int ReadAccessCycle;
            public int[] ReadAccessCycleTypes;
            public int WriteAccessCycle;

            public static DebugInfoElement ConvertFrom(Execute.ExecuteSetupData.MemoryContent.DebugInfoElement src)
            {
                return new CommonModule.RAM.DebugInfoElement()
                {
                    Usage = src.Usage,
                    IsDebugMarked = src.IsDebugMarked,
                    DebugInfo = src.DebugInfo,
                    DebugDisplayText = src.DebugDisplayText,

                    ReadAccessCycleTypes = new int[(int)EnumMemorymAccessType.Count]
                };
            }
        }
        public struct WordElement
        {
            public uint Content;
            public int DebugInfoCount;
            public DebugInfoElements DebugInfos;

            public static WordElement Empty
            {
                get
                {
                    return new WordElement()
                    {
                        Content = 0,
                        DebugInfoCount = 1,
                        DebugInfos = new DebugInfoElements()
                        {
                            Element0 = new DebugInfoElement()
                            {
                                IsDebugMarked = false
                            }
                        },
                    };
                }
            }
        }
        WordElement[] Words;
        public int WordCapacity
        {
            get { return Words.Length; }
        }


        public RAM()
        {
        }

        public bool Initialize(ExecuteSetupData.MemoryContent content)
        {
            this.Words = new WordElement[content.WordCapacity];

            if (this.Words.Length < content.WordCapacity)
            {
                MessageManager.ShowLine($"RAM: words capacity is too small for loading setup data.",enumMessageLevel.ProgressLog);
                return false;
            }

            for (int i = 0; i < content.Words.Count; i++)
            {
                this.Words[i] = new WordElement()
                {
                     Content = content.Words[i].InitialValue,
                     DebugInfoCount = content.Words[i].DebugInfoCount
                };

                this.Words[i].DebugInfos.Element0 = DebugInfoElement.ConvertFrom(content.Words[i].DebugInfos.Element0);
                this.Words[i].DebugInfos.Element1 = DebugInfoElement.ConvertFrom(content.Words[i].DebugInfos.Element1);
                this.Words[i].DebugInfos.Element2 = DebugInfoElement.ConvertFrom(content.Words[i].DebugInfos.Element2);
                this.Words[i].DebugInfos.Element3 = DebugInfoElement.ConvertFrom(content.Words[i].DebugInfos.Element3);
            }

            return true;
        }

        public uint this[int index]
        {
            get
            {
                return Words[index].Content;
            }
        }
        
        public bool GetDebugInfo(uint address,out string res,int wordDivideCount = 1,int wordByteIndex = 0)
        {
            //Border check
            if (address >= WordCapacity)
            {
                res ="";
                MessageManager.ShowLine($"RAM load: specified address 0x{ address.ToString("X8") } is out of range.",enumMessageLevel.ProgressLog);
                return false;
            }
            
            while (Words[address].DebugInfoCount < wordDivideCount)
            {
                wordDivideCount /= 2;
                wordByteIndex /= 2;
            }
            res = Words[address].DebugInfos[wordByteIndex].DebugInfo;
            return true;
        }

        public bool CountExecuteWord(uint address, int wordDivideCount = 1, int wordByteIndex = 0)
        {
            //Border check
            if (address >= WordCapacity)
            {
                MessageManager.ShowLine($"RAM load: specified address 0x{ address.ToString("X8") } is out of range.", enumMessageLevel.ProgressLog);
                return false;
            }

            //Compute byte index
            while (Words[address].DebugInfoCount < wordDivideCount)
            {
                wordDivideCount /= 2;
                wordByteIndex /= 2;
            }

            this.Words[address].DebugInfos[wordByteIndex].ExecuteCycle++;
            return true;
        }
        public bool LoadWord(uint address, out uint res, EnumMemorymAccessType accessType = EnumMemorymAccessType.No, int wordDivideCount = 1, int wordByteIndex = 0)
        {
            //Border check
            if (address >= WordCapacity)
            {
                res = 0;
                MessageManager.ShowLine($"RAM load: specified address 0x{ address.ToString("X8") } is out of range.",enumMessageLevel.ProgressLog);
                return false;
            }

            //Compute byte index
            while (Words[address].DebugInfoCount < wordDivideCount)
            {
                wordDivideCount /= 2;
                wordByteIndex /= 2;
            }

            //Load
            uint loaded = this.Words[address].Content;
            if (accessType >= 0)
            {
                this.Words[address].DebugInfos[wordByteIndex].ReadAccessCycle++;
                this.Words[address].DebugInfos[wordByteIndex].ReadAccessCycleTypes[(int)accessType]++;
            }

            //(If exists) show message
            if (this.Words[address].DebugInfoCount == 1 && 
                this.Words[address].DebugInfos.Element0.IsDebugMarked)
            {
                MessageManager.ShowLine(this.Words[address].DebugInfos.Element0.DebugDisplayText,enumMessageLevel.ExecutionLog);
            }

            res = loaded;
            return true;
        }

        public bool StoreWord(uint address,uint value, int wordDivideCount = 1, int wordByteIndex = 0)
        {
            //Border check
            if (address >= WordCapacity)
            {
                MessageManager.ShowLine($"RAM store: specified address 0x{ address.ToString("X8") } is out of range.",enumMessageLevel.ProgressLog);
                return false;
            }

            //Compute byte index
            while (Words[address].DebugInfoCount < wordDivideCount)
            {
                wordDivideCount /= 2;
                wordByteIndex /= 2;
            }

            //Load
            uint prevValue = this.Words[address].Content;
            this.Words[address].DebugInfos[wordByteIndex].WriteAccessCycle++;

            //(If exists) show message
            if (this.Words[address].DebugInfoCount == 1 &&
                this.Words[address].DebugInfos.Element0.IsDebugMarked)
            {
                MessageManager.ShowLine(this.Words[address].DebugInfos.Element0.DebugDisplayText,enumMessageLevel.ExecutionLog);
            }

            this.Words[address].Content = value;
            return true;
        }

        public bool GetWordAccessStatistics(uint address, int wordDivideCount, int wordByteIndex, out int execTimes, out int readTimes, out int writeTimes, out int[] readTimesPerType)
        {
            //Border check
            if (address >= WordCapacity)
            {
                readTimes = 0;
                writeTimes = 0;
                readTimesPerType = null;
                execTimes = 0;
                MessageManager.ShowLine($"RAM load: specified address 0x{ address.ToString("X8") } is out of range.", enumMessageLevel.ProgressLog);
                return false;
            }

            //Compute byte index
            while (Words[address].DebugInfoCount < wordDivideCount)
            {
                wordDivideCount /= 2;
                wordByteIndex /= 2;
            }

            //Load
            execTimes = this.Words[address].DebugInfos[wordByteIndex].ExecuteCycle;
            readTimes = this.Words[address].DebugInfos[wordByteIndex].ReadAccessCycle;
            writeTimes = this.Words[address].DebugInfos[wordByteIndex].WriteAccessCycle;
            readTimesPerType = this.Words[address].DebugInfos[wordByteIndex].ReadAccessCycleTypes;
            return true;
        }
    }
}
