using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    [Flags]
    public enum IdentifierType
    {
        /// <summary>
        /// ブロック、ラベル、変数等の名前がついた識別子
        /// </summary>
        ReferencingAddress = 0x1,

        /// <summary>
        /// シンボルを示す識別子
        /// </summary>
        ReferencingSymbol = 0x2
    }
}
