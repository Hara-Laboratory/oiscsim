using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public struct IdentifierSearchResult
    {
        public IdentifierType Type;
        public AddressSymbolInfo AddressIdentifier;
        public Symbol ImmediateIdentifier;
    }
}
