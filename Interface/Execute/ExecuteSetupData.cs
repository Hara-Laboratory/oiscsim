using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    public class ExecuteSetupData
    {
        public bool IsSixteenBitArch = false;
        public uint StartupAddress;
        public class MemoryContent
        {
            public int Slot
            {
                get;
                private set;
            }
            public int WordCapacity
            {
                get;
                private set;
            }
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
            public struct DebugInfoElement
            {
                public WordElement.enumUsage Usage;

                public string DebugInfo;
                public bool IsDebugMarked; //命令であれば実行されるたびに，変数領域であれば読み書きされるたびに 命令の場合は先頭の領域にマークするかな？
                public string DebugDisplayText; //余裕があればここでも文法を使えるようにする
            }
            public struct WordElement
            {
                public enum enumUsage
                {
                    Unknown,
                    Variable,
                    VariableArray,
                    Instruction,
                    FollowHead,
                }
                public uint InitialValue;

                public int DebugInfoCount;
                public DebugInfoElements DebugInfos;

                public static WordElement Empty
                {
                    get
                    {
                        return new WordElement()
                        {
                            InitialValue = 0,
                             DebugInfoCount = 1,
                              DebugInfos = new DebugInfoElements()
                              {
                                   Element0 = new DebugInfoElement()
                                   {
                                       IsDebugMarked = false
                                   }
                              }
                        };
                    }
                }
            }
            public List<WordElement> Words;
            public int CodeSize
            {
                get;
                private set;
            }
            /*public Dictionary<int,List<string>> SymbolNames
            {
                get;
                private set;
            }
            public Dictionary<string,int> SymbolTable
            {
                get;
                private set;
            }*/

            public MemoryContent(int slot)
            {
                this.Slot = slot;
                this.WordCapacity = 0;
                this.Words = new List<Execute.ExecuteSetupData.MemoryContent.WordElement>();
                /*this.SymbolTable = new Dictionary<string,int>();
                this.SymbolNames = new Dictionary<int,List<string>>();*/
            }
            
            public WordElement this[int index]
            {
                get { return Words[index]; }
                set { Words[index] = value; }
            }

            public uint ExpandCapacity(int expandWordAmount)
            {
                uint newRegionStart = (uint)this.WordCapacity;
                for (int i = 0; i < expandWordAmount; i++)
                {
                    Words.Add(WordElement.Empty);
                }
                this.WordCapacity += expandWordAmount;
                this.CodeSize = this.WordCapacity;

                return newRegionStart;
            }

            public uint ExpandCapacityForStack(int expandWordAmount)
            {
                uint newRegionStart = (uint)this.WordCapacity;
                for (int i = 0; i < expandWordAmount; i++)
                {
                    Words.Add(WordElement.Empty);
                }
                this.WordCapacity += expandWordAmount;

                return newRegionStart;
            }

            public DebugInfoElement GetDebugInfo(int address,int wordDivideCount = 1,int wordByteIndex = 0)
            {
                bool adjusted = false;
                while (Words[address].DebugInfoCount < wordDivideCount)
                {
                    wordDivideCount /= 2;
                    if (wordByteIndex % 2 != 0)
                        adjusted = true;
                    wordByteIndex /= 2;
                }
                DebugInfoElement res = Words[address].DebugInfos[wordByteIndex];
                if (adjusted)
                    res.Usage = WordElement.enumUsage.FollowHead;
                return res;
            }

            /*public void RegisterSymbol(int address,string name)
            {
                if (SymbolTable.ContainsKey(name))
                {
                    return;
                }

                this.SymbolTable.Add(name,address);

                List<string> names;
                if (!this.SymbolNames.TryGetValue(address,out names))
                {
                    names = new List<string>();
                    this.SymbolNames.Add(address,names);
                }
                names.Add(name);
            }*/
        }
        public MemoryContent[] MemoryContents;

        public ExecuteSetupData(int memorySlotCount)
        {
            this.MemoryContents = new MemoryContent[memorySlotCount];
            for (int i=0;i< memorySlotCount; i++)
            {
                this.MemoryContents[i] = new MemoryContent(i);
            }
        }
        
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

        public void ShowByMessage(enumMessageLevel level)
        {
            if (!MessageManager.TestLevel(level))
                return;

            MessageManager.ShowLine($"Entry point: 0x{ StartupAddress.ToString("X8") }",level);

            MessageManager.ShowLine($"Memory slots: Uses { MemoryContents.Length } memory slots",level);
            MessageManager.ShowLine("",level);

            MessageManager.ShowLine($"Memory initial contents:",level);
            MessageManager.GoInnerTab();
            foreach (var mem in MemoryContents)
            {
                const int contentTextWidth = 40;
                MessageManager.ShowLine($"[Slot.{ mem.Slot }]:",level);
                MessageManager.ShowLine($"Capacity = { mem.WordCapacity } words",level);
                MessageManager.ShowLine($"ByteAddress     { "Initial-value".PadRight(contentTextWidth,' ') } Mean",level);
                for (int memByteAddrHead=0;memByteAddrHead<mem.CodeSize*4;memByteAddrHead++)
                {
                    int memByteAddrTail = memByteAddrHead;
                    for (int cursor=memByteAddrHead+1; cursor < mem.CodeSize*4; cursor++)
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
                            contentText = $"0x{ mem[memByteAddrHead / 4].InitialValue.ToString("X8") } ({ (int)mem[memByteAddrHead / 4].InitialValue})";
                            break;
                        case MemoryContent.WordElement.enumUsage.Unknown:
                        default:
                            contentText = "(Alignment)";
                            break;
                        case MemoryContent.WordElement.enumUsage.Instruction:
                            contentText = "";
                            for (int i = 0; i < memByteAddrTail - memByteAddrHead + 1; i++)
                            {
                                if ((memByteAddrHead + i) % 4 == 0 || (mem[(memByteAddrHead + i) / 4].DebugInfoCount >= 2 && (memByteAddrHead + i) % 2 == 0))
                                    contentText += " ";
                                contentText += "" + ApplyByteMask(mem[(memByteAddrHead + i) / 4].InitialValue,(memByteAddrHead + i) % 4).ToString("X2");
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
                MessageManager.ShowLine($"-----------------------------------------------------------------------",level);
                MessageManager.ShowLine("",level);
            }
            MessageManager.GoOuterTab();
            MessageManager.ShowLine("",level);


        }

        public void GenerateHexFile(string path, bool splitWithLowHigh = false)
        {
            if (splitWithLowHigh)
            {
                GenerateLowHighHexFile(path);
                return;
            }

            MessageManager.ShowLine($"Entry point is 0x{ StartupAddress.ToString("X8") } (by word address)",enumMessageLevel.ProgressLog);

            StringBuilder sb = new StringBuilder();
            string displayFmt = IsSixteenBitArch ? "X4" : "X8";
            uint displayMask = IsSixteenBitArch ? 0xFFFF : 0xFFFFFFFF;
            int i = 0;
            {
                MemoryContent content = MemoryContents[i];
                for (int addr = 0; addr < content.WordCapacity; addr++)
                {
                    sb.AppendLine($"{(content.Words[addr].InitialValue & displayMask).ToString(displayFmt)}{ ((content.GetDebugInfo(addr).DebugInfo != null && false) ? ("  # " + content.GetDebugInfo(addr).DebugInfo) : "") } ");
                }
            }

            System.IO.File.WriteAllText(path,sb.ToString());
        }

        public void GenerateLowHighHexFile(string pathWithoutExt)
        {
            int extLength = System.IO.Path.GetExtension(pathWithoutExt).Length;
            if (extLength > 0)
                pathWithoutExt = pathWithoutExt.Remove(pathWithoutExt.Length - extLength);

            MessageManager.ShowLine($"Entry point is 0x{ StartupAddress.ToString("X8") } (by word address)", enumMessageLevel.ProgressLog);
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                {
                    MemoryContent content = MemoryContents[i];
                    for (int addr = 0; addr < content.WordCapacity; addr++)
                    {
                        uint val = content.Words[addr].InitialValue;
                        sb.AppendLine($"{((val >> 16) & 0xFFFF).ToString("X4")}{ ((content.GetDebugInfo(addr, 2, 0).DebugInfo != null && false) ? ("  # " + content.GetDebugInfo(addr, 2, 0).DebugInfo) : "") } ");
                    }
                }
                System.IO.File.WriteAllText(pathWithoutExt + "_h.hex", sb.ToString());
            }
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                {
                    MemoryContent content = MemoryContents[i];
                    for (int addr = 0; addr < content.WordCapacity; addr++)
                    {
                        uint val = content.Words[addr].InitialValue;
                        sb.AppendLine($"{(val & 0xFFFF).ToString("X4")}{ ((content.GetDebugInfo(addr, 2, 1).DebugInfo != null&&false) ? ("  # " + content.GetDebugInfo(addr, 2, 1).DebugInfo) : "") } ");
                    }
                }
                System.IO.File.WriteAllText(pathWithoutExt + "_l.hex", sb.ToString());
            }
        }
    }
}
