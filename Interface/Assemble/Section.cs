using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class Section : Block
    {
        [Flags]
        public enum enumAttribute
        {
            Default = 0x0,
            Startup = 0x1
        }
        public enumAttribute Attributes;
        public Instruction[] AllInstructions;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("section " + Name + "\r\n");
            sb.Append("{");

            {
                StringBuilder contentSb = new StringBuilder();
                if ((Attributes & enumAttribute.Startup) == enumAttribute.Startup)
                    contentSb.Append( "\r\n" + ".startup");

                foreach (var e in Variables)
                {
                    contentSb.Append("\r\n" + e.ToString());
                }
                foreach (var e in Symbols)
                {
                    contentSb.Append( "\r\n" + e.ToString());
                }
                foreach (var e in MacroDefinitions)
                {
                    contentSb.Append("\r\n" + e.ToString());
                }
                foreach (var e in Statements)
                {
                    contentSb.Append("\r\n" + e.ToString());
                }

                string space = "    ";
                contentSb.Insert(0,space);
                contentSb = contentSb.Replace("\r\n","\r\n" + space);
                sb.Append(contentSb);
            }

            sb.Append("\r\n}");
            return sb.ToString();
        }

        public bool CollectAllInstructions(List<AssembleError> errorList)
        {
            List<Instruction> instrList = new List<Assemble.Instruction>();
            if (!this.CollectAllInstructions(instrList,errorList))
                return false;

            this.AllInstructions = instrList.ToArray();
            return true;
        }
    }
}
