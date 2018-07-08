using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    /// <summary>
    /// マクロ定義
    /// </summary>
    /// <remarks>
    /// 引数名と一致する値記述がすべて置換されるブロックだと思うことにする
    /// 置換が起こるかもしれないため，展開時に内容は全てディープコピーしておかなくてはいけない
    /// </remarks>
    public class MacroDefinition : Block
    {
        public Block DefinedBlock;
        public class ArgumentElement
        {
            public string Name;

            public override string ToString()
            {
                return Name;
            }

            public ArgumentElement Clone(Block parentBlock)
            {
                return new Assemble.MacroDefinition.ArgumentElement()
                {
                    Name = this.Name
                };
            }
        }
        public ArgumentElement[] Arguments;

        public MacroDefinition()
        {
        }

        public override string ToString()
        {
            string res = "macro " + Name + "(";
            if (Arguments.Length > 0)
            {
                res += Arguments[0].ToString();
                for (int i = 1; i < Arguments.Length; i++)
                    res += "," + Arguments[i].ToString();
            }
            res += ")\r\n";
            res += "{";

            {
                string content = "";
                foreach (var e in Variables)
                {
                    content += "\r\n" + e.ToString();
                }
                foreach (var e in Symbols)
                {
                    content += "\r\n" + e.ToString();
                }
                foreach (var e in MacroDefinitions)
                {
                    content += "\r\n" + e.ToString();
                }
                foreach (var e in Statements)
                {
                    content += "\r\n" + e.ToString();
                }

                string space = "    ";
                content = space + content;
                content = content.Replace("\r\n","\r\n" + space);
                res += content;
            }

            res += "\r\n}";
            return res;
        }

        public new MacroDefinition Clone(Block parentBlock)
        {
            var res = new Assemble.MacroDefinition()
            {
                AssemblePosition = this.AssemblePosition,
                Name = this.Name,
                RootCode = this.RootCode,
                ParentBlock = parentBlock,
                DefinedBlock = this.DefinedBlock,//////////////////////////////////////////////
            };

            //Arguments
            res.Arguments = new ArgumentElement[this.Arguments.Length];
            for (int i = 0; i < this.Arguments.Length; i++)
            {
                res.Arguments[i] = this.Arguments[i].Clone(res); //Parent is new result block
            }

            //Variables
            res.Variables = new List<Variable>();
            for (int i = 0; i < this.Variables.Count; i++)
            {
                res.Variables.Add(this.Variables[i].Clone(res));
            }

            //Statements
            res.Statements = new List<StatementElement>();
            for (int i = 0; i < this.Statements.Count; i++)
            {
                res.Statements.Add(this.Statements[i].Clone(res));
            }

            //MacroDefinitions
            res.MacroDefinitions = new List<MacroDefinition>();
            for (int i = 0; i < this.MacroDefinitions.Count; i++)
            {
                res.MacroDefinitions.Add(this.MacroDefinitions[i].Clone(res));
            }

            return res;
        }

        /// <summary>
        /// マクロの内容を指定されたブロックに複製します
        /// </summary>
        public void CopyToBlock(Block destination)
        {
            //Variables
            destination.Variables = new List<Variable>();
            for (int i = 0; i < this.Variables.Count; i++)
            {
                destination.Variables.Add(this.Variables[i].Clone(destination));
            }

            //Statements
            destination.Statements = new List<StatementElement>();
            for (int i = 0; i < this.Statements.Count; i++)
            {
                destination.Statements.Add(this.Statements[i].Clone(destination));
            }

            //MacroDefinitions
            destination.MacroDefinitions = new List<MacroDefinition>();
            for (int i = 0; i < this.MacroDefinitions.Count; i++)
            {
                destination.MacroDefinitions.Add(this.MacroDefinitions[i].Clone(destination));
            }
        }

        public override bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
        {
            //Variables
            for (int i = 0; i < Variables.Count; i++)
            {
                if (!Variables[i].ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            //Statements
            for (int i = 0; i < Statements.Count; i++)
            {
                if (!Statements[i].ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            //MacroDefinitions
            for (int i = 0; i < MacroDefinitions.Count; i++)
            {
                if (!MacroDefinitions[i].ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            return true;
        }

        public override bool ExpandAllMacros(List<AssembleError> errorList)
        {
            throw new NotImplementedException();
        }
    }
}
