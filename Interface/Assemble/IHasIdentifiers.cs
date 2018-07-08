using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public interface IHasIdentifiers
    {
        bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res);
    }
}
