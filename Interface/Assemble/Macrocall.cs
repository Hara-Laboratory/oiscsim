using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Macrocall
    {
        public AssemblePosition AssemblePosition;
        public string CallerName;
        public Block WrittenBlock;
        public List<Label> Labels;
        public class OperandElement
        {
            public AssemblePosition AssemblePosition;
            public Macrocall WrittenMacrocall;
            public ValueBase Immediate;

            public override string ToString()
            {
                return Immediate.ToString();
            }
            
            public OperandElement Clone(Block parentBlock,Macrocall writtenMacro)
            {
                var res = new OperandElement()
                {
                    AssemblePosition = this.AssemblePosition,
                    Immediate = this.Immediate.Clone(parentBlock),
                    WrittenMacrocall = writtenMacro
                };

                return res;
            }

            public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
            {
                int matchIdx;
                if (!Immediate.MatchIdentifier(identifiers, out matchIdx))
                    return true;

                Immediate = values[matchIdx].Immediate.Clone(WrittenMacrocall.WrittenBlock);
                return true;
            }
        }
        public OperandElement[] Operands;
        public string DebugText;


        public Macrocall()
        {

        }

        public override string ToString()
        {
            string res = "";
            foreach (var lbl in Labels)
            {
                res += lbl.ToString() + "\r\n";
            }
            res += "~" + CallerName + "(";
            if (Operands.Length > 0)
            {
                res += Operands[0].ToString();
                for (int i = 1; i < Operands.Length; i++)
                    res += "," + Operands[i].ToString();
            }
            res += ");";
            if (DebugText.Length > 0)
                res += " >>>>\"" + DebugText + "\"";
            return res;
        }

        public Macrocall Clone(Block parentBlock)
        {
            var res = new Macrocall()
            {
                AssemblePosition = this.AssemblePosition,
                CallerName = this.CallerName,
                DebugText = this.DebugText,
                WrittenBlock = parentBlock
            };

            //Labels
            res.Labels = new List<Assemble.Label>();
            for (int i=0;i<this.Labels.Count;i++)
            {
                Label clonedLbl = this.Labels[i].Clone(parentBlock);
                res.Labels.Add(clonedLbl);
            }

            //Operands
            res.Operands = new OperandElement[this.Operands.Length];
            for (int i=0;i<this.Operands.Length;i++)
            {
                res.Operands[i] = this.Operands[i].Clone(parentBlock,res);
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
            return true;
        }
    }
}
