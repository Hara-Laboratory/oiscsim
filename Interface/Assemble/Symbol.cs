using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Symbol
    {
        public AssemblePosition AssemblePosition;
        public string Name;
        public Block DefinedBlock;
        public ValueBase Content;

        public Symbol()
        {
        }

        public override string ToString()
        {
            string res = "";
            res += ".symbol " + Name + " := " + Content.ToString();
            res += ";";
            return res;
        }

        public Symbol Clone(Block parentBlock)
        {
            Symbol res = new Assemble.Symbol()
            {
                AssemblePosition = this.AssemblePosition,
                Name = this.Name,
                DefinedBlock = parentBlock,
                Content = Content.Clone(parentBlock)
            };
            return res;
        }

        public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
        {
            int matchIdx;
            if (!Content.MatchIdentifier(identifiers,out matchIdx))
                return false;

            /*
            if (values[matchIdx].Type != Macrocall.enumOperandType.Immediate)
            { //Error
                errorList.Add(new Assemble.AssembleError()
                {
                    Title = "Replacement",
                    Detail = "Register number cannot be used for symbol refering target.",
                    Position = this.AssemblePosition
                });
                return false;
            }
            */
            //Replace
            this.Content = values[matchIdx].Immediate.Clone(DefinedBlock);
            return true;
        }

        public bool SolveAllReferences(List<AssembleError> errorList)
        {
            if (!Content.SolveReferences(errorList))
                return false;

            return true;
        }

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new IdentifierSearchResult();

            if (type.HasFlag(IdentifierType.ReferencingSymbol) && name == this.Name)
            {
                res.ImmediateIdentifier = this;
                res.Type = IdentifierType.ReferencingSymbol;
                return true;
            }

            return false;
        }
    }
}
