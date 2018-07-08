using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Instruction : IHasIdentifiers
    {
        public AssemblePosition AssemblePosition;
        public string Nimonic;
        public Block WrittenBlock;
        public List<Label> Labels;
        public class OperandElement
        {
            public AssemblePosition AssemblePosition;
            public Instruction WrittenInstruction;
            public RegisterMapping.RegisterElement Register;
            public int RegisterNo;
            public ValueBase Immediate;
            public List<Label> Labels;
            public readonly AddressSymbolInfo PlacedInfo = new AddressSymbolInfo(); //Blocking instance changing

            public override string ToString()
            {
                string res = "";
                foreach (var lbl in Labels)
                {
                    res += lbl.ToString() + " ";
                }
                res += Immediate.ToString();
                return res;
            }

            public OperandElement Clone(Block parentBlock,Instruction writtenInstr)
            {
                var res = new OperandElement()
                {
                    AssemblePosition = this.AssemblePosition,
                    Immediate = this.Immediate.Clone(parentBlock),
                     WrittenInstruction = writtenInstr
                };

                res.Labels = new List<Label>();
                for (int lblIdx = 0; lblIdx < this.Labels.Count; lblIdx++)
                {
                    Label clonedLbl = this.Labels[lblIdx].Clone(parentBlock);
                    res.Labels.Add(clonedLbl);
                    clonedLbl.SetLabelPlacedInfo(res.PlacedInfo);
                    if (res.PlacedInfo == null)
                    {

                    }
                }

                return res;
            }

            public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
            {
                int matchIdx;
                if (!Immediate.MatchIdentifier(identifiers,out matchIdx))
                    return true;

                Immediate = values[matchIdx].Immediate.Clone(WrittenInstruction.WrittenBlock);
                return true;
            }

            public bool SolveAllReferences(List<AssembleError> errorList)
            {
                if (!Immediate.SolveReferences(errorList))
                    return false;

                return true;
            }

            public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
            {
                res = new Assemble.IdentifierSearchResult();
                //Labels
                foreach (var lbl in Labels)
                {
                    if (lbl.FindIdentifier(name,type,out res))
                        return true;
                }

                return false;
            }
        }
        public OperandElement[] Operands;
        public string DebugText;
        public readonly AddressSymbolInfo PlacedInfo = new AddressSymbolInfo(); //Blocking instance changing
        public enum enumJumpAttributeType
        {
            Register,
            Immediate
        }
        public class JumpAttribute
        {
            public string Nimonic;
            public ValueBase Immediate;
            public Instruction WrittenInstruction;
            public AssemblePosition AssemblePosition;

            public override string ToString()
            {
                string res = " -< " + Nimonic + " ";

                res += Immediate.ToString();
                return res;
            }

            public JumpAttribute Clone(Block parentBlock,Instruction writtenInstr)
            {
                var res = new JumpAttribute()
                {
                    Nimonic = this.Nimonic,
                    AssemblePosition = this.AssemblePosition,
                    Immediate = this.Immediate.Clone(parentBlock),
                    WrittenInstruction = writtenInstr
                };

                return res;
            }

            public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
            {
                int matchIdx;
                if (!Immediate.MatchIdentifier(identifiers, out matchIdx))
                    return true;

                Immediate = values[matchIdx].Immediate.Clone(WrittenInstruction.WrittenBlock);
                return true;
            }

            public bool SolveAllReferences(List<AssembleError> errorList)
            {
                if (!Immediate.SolveReferences(errorList))
                    return false;

                return true;
            }

        }
        public JumpAttribute JumpAttributeInfo = null;


        public Instruction()
        {

        }

        public override string ToString()
        {
            string res = "";
            foreach (var lbl in Labels)
            {
                res += lbl.ToString() + "\r\n";
            }
            res += "" + Nimonic + "\t";
            if (Operands.Length > 0)
            {
                res += Operands[0].ToString();
                for (int i = 1; i < Operands.Length; i++)
                    res += "," + Operands[i].ToString();
            }
            if (this.JumpAttributeInfo != null)
            {
                res += JumpAttributeInfo.ToString();
            }
            res += ";";
            return res;
        }

        public Instruction Clone(Block parentBlock)
        {
            var res = new Instruction()
            {
                AssemblePosition = this.AssemblePosition,
                Nimonic = this.Nimonic,
                DebugText = this.DebugText,
                WrittenBlock = parentBlock
            };

            //Labels
            res.Labels = new List<Assemble.Label>();
            for (int i = 0; i < this.Labels.Count; i++)
            {
                Label clonedLbl = this.Labels[i].Clone(parentBlock);
                res.Labels.Add(clonedLbl);
                clonedLbl.SetLabelPlacedInfo(res.PlacedInfo);
            }

            //Operands
            res.Operands = new OperandElement[this.Operands.Length];
            for (int i = 0; i < this.Operands.Length; i++)
            {
                res.Operands[i] = this.Operands[i].Clone(parentBlock,res);
            }

            //JumpAttributeInfo
            res.JumpAttributeInfo = null;
            if (this.JumpAttributeInfo != null)
            {
                res.JumpAttributeInfo = this.JumpAttributeInfo.Clone(parentBlock,res);
            }

            return res;
        }

        public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
        {
            //Operands
            for (int i = 0; i < Operands.Length; i++)
            {
                if (!Operands[i].ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            //JumpAttributeInfo
            if (this.JumpAttributeInfo != null)
            {
                if (!JumpAttributeInfo.ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// この命令からの全ての参照を解決します
        /// </summary>
        public bool SolveAllReferences(List<AssembleError> errorList)
        {
            //Operands
            for (int oprIdx = 0; oprIdx < Operands.Length; oprIdx++)
            {
                if (!Operands[oprIdx].SolveAllReferences(errorList))
                    return false;
            }

            //JumpAttributeInfo
            if (this.JumpAttributeInfo != null)
            {
                if (!JumpAttributeInfo.SolveAllReferences(errorList))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// この命令の中に指定された識別子によって参照されるものがあるか検索します
        /// </summary>
        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new Assemble.IdentifierSearchResult();
            //Operands
            foreach (var opr in Operands)
            {
                if (opr.FindIdentifier(name,type,out res))
                    return true;
            }

            //Labels
            for (int lblIdx = 0; lblIdx < Labels.Count; lblIdx++)
            {
                if (Labels[lblIdx].FindIdentifier(name,type,out res))
                    return true;
            }
            
            return false;
        }
    }
}
