using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Interface.Assemble
{
    public struct AssemblePosition
    {
        public string FilePath;
        public ParseTreeNode ParseNode;
        public int LineNumber,ColumnNumber;

        public AssemblePosition(string path,ParseTreeNode parseNode)
        {
            this.FilePath = path;
            this.ParseNode = parseNode;
            this.LineNumber = this.ColumnNumber = 0;
        }

        public AssemblePosition(string path,int line,int column)
        {
            this.FilePath = path;
            this.ParseNode = null;
            this.LineNumber = line;
            this.ColumnNumber = column;
        }

        public string GenerateExplainText()
        {
            if (ParseNode != null)
            {
                var token = this.ParseNode.FindToken();
                return $"\"{ token.Text }\" (line: { token.Location.Line + 1 },col: { token.Location.Column })";
            }
            else
            {
                return $"line: { this.LineNumber + 1 },col: { this.ColumnNumber }";
            }
        }

        public string GenerateLocationText()
        {
            if (ParseNode != null)
            {
                var token = this.ParseNode.FindToken();
                return $"line: { token.Location.Line },col: { token.Location.Column }";
            }
            else
            {
                return $"line: { this.LineNumber },col: { this.ColumnNumber }";
            }
        }
    }
}
