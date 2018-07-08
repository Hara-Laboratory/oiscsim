using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Misc;

namespace Interface.Assemble
{
    //解決された参照情報を格納する
    //参照されうるもの:
    //Labeled Instruction
    //Block
    //Section
    //Variable
    //参照する主体:
    //Immediate-reference
    //  - Instruction
    //  - Variable
    public class AddressSymbolInfo
    {
        public int MemoryNumber;
        public AddressRange Address;
    }
}
