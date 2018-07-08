using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Variable : IHasIdentifiers
    {
        public AssemblePosition AssemblePosition;
        public string Name;
        public Block DefinedBlock;
        public bool IsConstant;
        public bool IsNeedInitialization; //初期値を持っている前提の値か これをつけている場合は変数を1つにまとめることは出来ない
        public ValueBase[] InitialValues;
        public readonly AddressSymbolInfo PlacedInfo = new AddressSymbolInfo();
        public VariableAnalyzeInfo.ElementBase[] AnalyzeResults; //変数名が適当
        public int PositionHint = 0; //配置位置のヒント


        public Variable()
        {
        }

        public override string ToString()
        {
            string res = "";
            res += ((IsConstant) ? ".constant " : ".variable ") + Name;
            if (!IsNeedInitialization && !IsConstant)
            {
                if (PositionHint != 0)
                    res += $" (@{PositionHint})";
            }
            else if (InitialValues.Length == 1)
            {
                if (PositionHint != 0)
                    res += $" (@{PositionHint})";
                res += " = ";
                res += InitialValues[0].ToString();
            }
            else
            {
                res += "[" + InitialValues.Length.ToString() + "]";
                if (PositionHint != 0)
                    res += $" (@{PositionHint}) ";
                res += " = ";
                res += "{ " + InitialValues[0].ToString();
                for (int i = 1; i < InitialValues.Length; i++)
                    res += "," + InitialValues[i].ToString();
                res += " }";
            }
            res += ";";
            return res;
        }

        public Variable Clone(Block parentBlock)
        {
            Variable res = new Assemble.Variable()
            {
                AssemblePosition = this.AssemblePosition,
                Name = this.Name,
                PositionHint = this.PositionHint,
                DefinedBlock = parentBlock,
                IsConstant = this.IsConstant,
                IsNeedInitialization = this.IsNeedInitialization,
            };

            //InitialValues
            res.InitialValues = new ValueBase[this.InitialValues.Length];
            for (int i = 0; i < res.InitialValues.Length; i++)
            {
                res.InitialValues[i] = this.InitialValues[i].Clone(parentBlock);
            }

            return res;
        }

        public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
        {
            //InitialValues
            for (int i = 0; i < InitialValues.Length; i++)
            {
                int matchIdx;
                if (!InitialValues[i].MatchIdentifier(identifiers,out matchIdx))
                    continue;
                /*
                if (values[matchIdx].Immediate.Type != ValueBaseType.Immediate)
                { //Error
                    errorList.Add(new Assemble.AssembleError()
                    {
                        Title = "Replacement",
                        Detail = "Register number cannot be used for variable initial value.",
                        Position = this.AssemblePosition
                    });
                    return false;
                }*/

                //Replace
                this.InitialValues[i] = values[matchIdx].Immediate.Clone(DefinedBlock);
            }
            return true;
        }

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new Assemble.IdentifierSearchResult();
            if (type.HasFlag(IdentifierType.ReferencingAddress) && this.Name == name)
            {
                res.Type = IdentifierType.ReferencingAddress;
                res.AddressIdentifier = this.PlacedInfo;
                return true;
            }

            return false;
        }

        public bool SolveAllReferences(List<AssembleError> errorList)
        {
            foreach (var e in InitialValues)
            {
                if (!e.SolveReferences(errorList))
                    return false;
            }
            return true;
        }
    }
}
