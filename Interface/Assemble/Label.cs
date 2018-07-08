using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Label : IHasIdentifiers
    {
        public AssemblePosition AssemblePosition;
        public string Name;
        public Block DefinedBlock;
        public AddressSymbolInfo PlacedInfo
        {
            get;
            private set;
        }

        public Label()
        {
        }

        public void SetLabelPlacedInfo(AddressSymbolInfo placedInfo)
        {
            if (placedInfo==null)
            {

            }
            this.PlacedInfo = placedInfo;
        }

        public override string ToString()
        {
            return Name + ":";
        }

        public Label Clone(Block parentBlock)
        {
            return new Assemble.Label()
            {
                AssemblePosition = this.AssemblePosition,
                Name = this.Name,
                DefinedBlock = parentBlock
            };
        }

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new Assemble.IdentifierSearchResult();

            if (((type & IdentifierType.ReferencingAddress) == IdentifierType.ReferencingAddress) && this.Name == name)
            {
                res.Type = IdentifierType.ReferencingAddress;
                res.AddressIdentifier = this.PlacedInfo;
                return true;
            }
            
            return false;
        }
    }
}
