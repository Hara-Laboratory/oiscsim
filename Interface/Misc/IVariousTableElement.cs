using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Misc
{
    public interface IVariousTableElement
    {
        void Read(VariousTable table);

        void Write(VariousTable table);
    }
}
