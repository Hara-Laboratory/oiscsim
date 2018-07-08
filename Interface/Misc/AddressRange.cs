using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Misc
{
    public struct AddressRange
    {
        public uint From;
        public uint To;
        public uint Size { get { return To - From + 1; } }
    }
}
