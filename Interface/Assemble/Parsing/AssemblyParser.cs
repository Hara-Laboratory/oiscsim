using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Interface.Assemble.Parsing
{
    class AssemblyParser
    {
        public List<AssembleError> Errors = new List<AssembleError>();
        private string ParsingFilePath;

        public AssemblyParser()
        {
        }

        public void ReportError(string title,string detail,ParseTreeNode node)
        {
            this.Errors.Add(new AssembleError()
            {
                Title = title,
                Detail = detail,
                Position = new AssemblePosition(ParsingFilePath,node)
            });
        }

        public bool Parse(ParseTreeNode root,string filePath,out AssemblyCode res,out List<string> includeFiles)
        {
            res = new AssemblyCode();
            return ParseAndAdd(root,filePath,res,out includeFiles);
        }

        public bool ParseAndAdd(ParseTreeNode root,string filePath,AssemblyCode res,out List<string> includeFiles)
        {
            includeFiles = new List<string>();
            this.ParsingFilePath = filePath;
            ParseTreeNode fileroot = root;

            //Find rootdefs
            ParseTreeNode rootdefs;
            if (fileroot.ChildNodes.Count != 1 || fileroot.ChildNodes[0].Term.Name != "root-defines")
            {
                ReportError("Root","root-defines not found.",fileroot);
                return false;
            }
            rootdefs = fileroot.ChildNodes[0];

            //Parse rootdefs
            for (int i = 0; i < rootdefs.ChildNodes.Count; i++)
            {
                ParseTreeNode rootdef = rootdefs.ChildNodes[i];
                if (rootdef.ChildNodes[0].Term.Name == "using-description")
                {
                    includeFiles.Add((string)rootdef.ChildNodes[0].ChildNodes[1].Token.Value);
                }
                else if (rootdef.ChildNodes[0].Term.Name == "sectiondef")
                {
                    Section sec;
                    if (!ParseSection(rootdef.ChildNodes[0],res,out sec))
                    {
                        return false;
                    }

                    res.Sections.Add(sec);
                }
                else if (rootdef.ChildNodes[0].Term.Name == "memorysize-description")
                {
                    res.MinMemorySize = Math.Max(res.MinMemorySize,(int)rootdef.ChildNodes[0].ChildNodes[1].Token.Value);
                }
                else if (rootdef.ChildNodes[0].Term.Name == "targetisa-description")
                {
                    res.TargetISAName = (string)rootdef.ChildNodes[0].ChildNodes[1].Token.Value;
                }
            }

            return true;
        }
        
        private bool ParseSection(ParseTreeNode node,AssemblyCode rootCode,out Section res)
        {
            res = new Section();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.ParentBlock = null;
            res.RootCode = rootCode;
            { //section + name + { + attributes + contents + }
                //Name
                res.Name = node.ChildNodes[1].Token.Text;

                //Attributes
                if (!ParseSectionAttributes(node.ChildNodes[3],out res.Attributes))
                {
                    return false;
                }

                //Contents
                Block blockedRes = res;
                if (!ParseBlockContents(node.ChildNodes[4],ref blockedRes))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParseSectionAttributes(ParseTreeNode node,out Section.enumAttribute res)
        {
            res = Section.enumAttribute.Default;
            foreach (var e in node.ChildNodes)
            {
                Section.enumAttribute newAttr = Section.enumAttribute.Default;
                switch (e.ChildNodes[0].Token.Text)
                {
                    case ".startup":
                        newAttr = Section.enumAttribute.Startup;
                        break;
                    default:
                        ReportError("SectionAttributes","Unknown section attribute.",e.ChildNodes[0]);
                        return false;
                }

                //Apply
                res |= newAttr;
            }

            return true;
        }

        private bool ParseBlock(ParseTreeNode node,Block block,out Block res)
        {
            res = new Block();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.ParentBlock = block;
            res.RootCode = block.RootCode;
            if (node.ChildNodes.Count == 4)
            { //name + { + contents + }
                res.Name = node.ChildNodes[0].Token.Text;
                if (!ParseBlockContents(node.ChildNodes[2],ref res))
                {
                    return false;
                }
                return true;
            }
            else if (node.ChildNodes.Count == 3)
            { //{ + contents + }
                res.Name = "";
                if (!ParseBlockContents(node.ChildNodes[1],ref res))
                {
                    return false;
                }
                return true;
            }

            ReportError("Block","Unknown format block definition.",node);
            return false;
        }

        private bool ParseBlockContents(ParseTreeNode node,ref Block res)
        {
            { //Contents
                ParseTreeNode contents = node;
                foreach (var e in contents.ChildNodes)
                {
                    if (e.ChildNodes[0].Term.Name == "labeled-statement")
                    {
                        Block.StatementElement stmt;
                        if (!ParseLabeledStatement(e.ChildNodes[0],res,out stmt))
                        {
                            return false;
                        }

                        res.Statements.Add(stmt);
                    }
                    else if (e.ChildNodes[0].Term.Name == "blockdef")
                    {
                        Block.StatementElement stmt;
                        Block subBlock;
                        if (!ParseBlock(e.ChildNodes[0],res,out subBlock))
                        {
                            return false;
                        }

                        stmt = new Block.StatementElement()
                        {
                            Type = Block.enumStatementType.Block,
                            Block = subBlock
                        };
                        res.Statements.Add(stmt);
                    }
                    else if (e.ChildNodes[0].Term.Name == "macrodef")
                    {
                        MacroDefinition mcrodef;
                        if (!ParseMacroDefinition(e.ChildNodes[0],res,out mcrodef))
                        {
                            return false;
                        }

                        res.MacroDefinitions.Add(mcrodef);
                    }
                    else if (e.ChildNodes[0].Term.Name == "variabledef")
                    {
                        Variable varb;
                        if (!ParseVariable(e.ChildNodes[0],res,out varb))
                        {
                            return false;
                        }

                        res.Variables.Add(varb);
                    }
                    else if (e.ChildNodes[0].Term.Name == "constantdef")
                    {
                        Variable varb;
                        if (!ParseConstant(e.ChildNodes[0],res,out varb))
                        {
                            return false;
                        }

                        res.Variables.Add(varb);
                    }
                    else if (e.ChildNodes[0].Term.Name == "symboldef")
                    {
                        Symbol symbl;
                        if (!ParseSymbol(e.ChildNodes[0],res,out symbl))
                        {
                            return false;
                        }

                        res.Symbols.Add(symbl);
                    }
                }
            }

            return true;
        }

        private bool ParseLabeledStatement(ParseTreeNode node,Block block,out Block.StatementElement res)
        {
            List<Label> labelList = new List<Label>();
            ParseTreeNode statementNode = node;
            while (true)
            {
                if (statementNode.ChildNodes.Count == 1)
                { //statement
                    statementNode = statementNode.ChildNodes[0];
                    break;
                }
                else if (statementNode.ChildNodes.Count == 2)
                { //label + labeledstatement
                    ParseTreeNode lblNode = statementNode.ChildNodes[0];
                    Label lbl = new Label()
                    {
                        AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,lblNode),
                        Name = lblNode.ChildNodes[0].Token.Text,
                        DefinedBlock = block
                    };
                    labelList.Add(lbl);

                    statementNode = statementNode.ChildNodes[1];
                }
            }

            if (!ParseStatement(statementNode,block,labelList,out res))
            {
                return false;
            }
            return true;
        }

        private bool ParseStatement(ParseTreeNode node,Block block,List<Label> labels,out Block.StatementElement res)
        {
            res = new Block.StatementElement();

            if (node.ChildNodes[0].Term.Name == "instruction")
            { 
                Instruction instr;
                if (!ParseInstruction(node.ChildNodes[0],block,labels,out instr))
                {
                    return false;
                }

                res.Type = Block.enumStatementType.Instruction;
                res.Instruction = instr;
                return true;
            }
            else if (node.ChildNodes[0].Term.Name == "macrocall")
            {
                Macrocall mcrocall;
                if (!ParseMacrocall(node.ChildNodes[0],block,labels,out mcrocall))
                {
                    return false;
                }

                res.Type = Block.enumStatementType.Macrocall;
                res.Macrocall = mcrocall;
                return true;
            }

            ReportError("Statement","Unknown statement type.",node.ChildNodes[0]);
            return false;
        }

        private bool ParseInstruction(ParseTreeNode node,Block block,List<Label> labels,out Instruction res)
        {
            res = new Assemble.Instruction();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.WrittenBlock = block;
            { //nemonic + operands + [jump] + ; + [debugtext]
                //Nemonic
                res.Nimonic = node.ChildNodes[0].ChildNodes[0].Token.Text;

                //Operands
                if (!ParseInstructionOperands(node.ChildNodes[1],block,out res.Operands))
                {
                    return false;
                }
                foreach (var e in res.Operands)
                    e.WrittenInstruction = res;

                //Jump attribute
                if (node.ChildNodes[2].Term.Name == "instruction-jump")
                {
                    ParseTreeNode jumpattrNode = node.ChildNodes[2];
                    res.JumpAttributeInfo = new Instruction.JumpAttribute()
                    {
                        AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,jumpattrNode),
                        WrittenInstruction = res
                    };

                    //Mnemonic
                    res.JumpAttributeInfo.Nimonic = jumpattrNode.ChildNodes[1].ChildNodes[0].Token.Text;

                    //Target
                    ParseTreeNode jumpattrTargetNode = jumpattrNode.ChildNodes[2].ChildNodes[0];
                    if (jumpattrTargetNode.Term.Name == "operand-register")
                    {
                        ValueBase val;
                        if (!ParseRegisterInfo(jumpattrTargetNode, block, out val))
                        {
                            return false;
                        }

                        res.JumpAttributeInfo.Immediate = val;
                    }
                    else if (jumpattrTargetNode.Term.Name == "operand-reference")
                    { //Recognize as immediate
                        res.JumpAttributeInfo.Immediate = new ValueReference(jumpattrTargetNode.ChildNodes[0].Token.Text,block);
                        res.JumpAttributeInfo.Immediate.AssemblePosition = res.AssemblePosition;
                    }
                    else if (jumpattrTargetNode.Term.Name == "operand-immediate")
                    {
                        ValueBase val;
                        if (!ParseImmediateValue(jumpattrTargetNode.ChildNodes[0],block,out val))
                        {
                            return false;
                        }
                        
                        res.JumpAttributeInfo.Immediate = val;
                    }
                }
                else
                {
                    res.JumpAttributeInfo = null;
                }

                //Debug text (if exists)
                if (node.ChildNodes[node.ChildNodes.Count - 1].Term.Name == "debugprint")
                {
                    ParseTreeNode debugtextNode = node.ChildNodes[node.ChildNodes.Count - 1];
                    res.DebugText = debugtextNode.ChildNodes[1].Token.ValueString;
                }
                else
                {
                    res.DebugText = "";
                }

                //Labels
                res.Labels = labels;
                foreach (var e in labels)
                {
                    e.SetLabelPlacedInfo(res.PlacedInfo);
                }
            }

            return true;
        }

        private bool ParseInstructionOperands(ParseTreeNode node,Block block,out Instruction.OperandElement[] res)
        {
            res = null;
            List<Instruction.OperandElement> resList = new List<Assemble.Instruction.OperandElement>();
            foreach (var subNode in node.ChildNodes)
            {
                Instruction.OperandElement opr;
                if (!ParseLabeledInstructionOperand(subNode,block,out opr))
                {
                    return false;
                }

                resList.Add(opr);
            }

            res = resList.ToArray();
            return true;
        }
        
        private bool ParseLabeledInstructionOperand(ParseTreeNode node,Block block,out Instruction.OperandElement res)
        {
            List<Label> labelList = new List<Label>();
            ParseTreeNode operandNode = node;
            while (true)
            {
                if (operandNode.ChildNodes.Count == 1)
                { //instrOperand
                    operandNode = operandNode.ChildNodes[0];
                    break;
                }
                else if (operandNode.ChildNodes.Count == 2)
                { //label + labeledInstrOperands
                    ParseTreeNode lblNode = operandNode.ChildNodes[0];
                    Label lbl = new Label()
                    {
                        AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,lblNode),
                        Name = lblNode.ChildNodes[0].Token.Text,
                        DefinedBlock = block
                    };
                    labelList.Add(lbl);

                    operandNode = operandNode.ChildNodes[1];
                }
            }

            if (!ParseInstructionOperand(operandNode,block,labelList,out res))
            {
                return false;
            }
            return true;
        }

        private bool ParseInstructionOperand(ParseTreeNode node,Block block,List<Label> labels,out Instruction.OperandElement res)
        {
            res = new Assemble.Instruction.OperandElement();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.Labels = labels;
            foreach (var e in labels)
            {
                e.SetLabelPlacedInfo(res.PlacedInfo);
            }

            ParseTreeNode op = node.ChildNodes[0];
            if (op.Term.Name == "operand-register")
            {
                ValueBase val;
                if (!ParseRegisterInfo(op, block, out val))
                {
                    return false;
                }
                
                res.Immediate = val;
                return true;
            }
            else if (op.Term.Name == "operand-reference")
            { //Recognize as immediate
                res.Immediate = new ValueReference(op.ChildNodes[0].Token.Text,block);
                res.Immediate.AssemblePosition = res.AssemblePosition;
                return true;
            }
            else if (op.Term.Name == "operand-immediate")
            {
                ValueBase val;
                if (!ParseImmediateValue(op.ChildNodes[0],block,out val))
                {
                    return false;
                }
                
                res.Immediate = val;
                return true;
            }

            ReportError("InstructionOperand","Unknown instruction operand type.",op);
            return false;
        }

        private bool ParseMacrocall(ParseTreeNode node,Block block,List<Label> labels,out Macrocall res)
        {
            res = new Assemble.Macrocall();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.WrittenBlock = block;
            { //~ + name + ( + operands + ) + ; + [debugtext]
                //Nemonic
                res.CallerName = node.ChildNodes[1].ChildNodes[0].Token.Text;

                //Operands
                if (!ParseMacrocallOperands(node.ChildNodes[3],block,out res.Operands))
                {
                    return false;
                }
                foreach (var e in res.Operands)
                    e.WrittenMacrocall = res;

                //Debug text (if exists)
                if (node.ChildNodes.Count == 6 && node.ChildNodes[5].Term.Name == "debugprint")
                {
                    ParseTreeNode debugtextNode = node.ChildNodes[5];
                    res.DebugText = debugtextNode.ChildNodes[1].Token.ValueString;
                }
                else
                {
                    res.DebugText = "";
                }

                //Labels
                res.Labels = labels;
            }

            return true;
        }

        private bool ParseMacrocallOperands(ParseTreeNode node,Block block,out Macrocall.OperandElement[] res)
        {
            res = null;
            List<Macrocall.OperandElement> resList = new List<Assemble.Macrocall.OperandElement>();
            foreach (var subNode in node.ChildNodes)
            {
                Macrocall.OperandElement opr;
                if (!ParseMacrocallOperand(subNode,block,out opr))
                {
                    return false;
                }

                resList.Add(opr);
            }

            res = resList.ToArray();
            return true;
        }

        private bool ParseMacrocallOperand(ParseTreeNode node,Block block,out Macrocall.OperandElement res)
        {
            res = new Assemble.Macrocall.OperandElement();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);

            ParseTreeNode op = node.ChildNodes[0];
            if (op.Term.Name == "operand-register")
            {
                ValueBase val;
                if (!ParseRegisterInfo(op, block, out val))
                {
                    return false;
                }
                
                res.Immediate = val;
                return true;
            }
            else if (op.Term.Name == "operand-reference")
            { //Recognize as immediate
                res.Immediate = new ValueReference(op.ChildNodes[0].Token.Text,block);
                res.Immediate.AssemblePosition = res.AssemblePosition;
                return true;
            }
            else if (op.Term.Name == "operand-immediate")
            {
                ValueBase val;
                if (!ParseImmediateValue(op.ChildNodes[0],block,out val))
                {
                    return false;
                }
                
                res.Immediate = val;
                return true;
            }

            ReportError("InstructionOperand","Unknown instruction operand type.",op);
            return false;
        }

        private bool ParseVariable(ParseTreeNode node,Block block,out Variable res)
        {
            res = new Assemble.Variable();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.DefinedBlock = block;
            if (node.ChildNodes.Count == 3 || node.ChildNodes.Count == 4)
            { //.variable + name + [variabledefPositionHint] + ;
                //Name
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;

                //No content
                res.InitialValues = new ValueBase[1] { ValueBase.Zero };

                //PositionHint
                if (node.ChildNodes.Count == 4)
                    res.PositionHint = (int)(node.ChildNodes[2].ChildNodes[2].Token.Value);

                //Mark attribute
                res.IsConstant = false;
                res.IsNeedInitialization = false;
            }
            else if (node.ChildNodes.Count == 5 || node.ChildNodes.Count == 6)
            { //.variable + name + [variabledefPositionHint] + = + content + ;
                //Name
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;
                
                //PositionHint
                if (node.ChildNodes.Count == 6)
                    res.PositionHint = (int)(node.ChildNodes[2].ChildNodes[2].Token.Value);

                //Content
                ParseTreeNode contentNode = node.ChildNodes[node.ChildNodes.Count - 2];
                ValueBase initialValue;
                if (!ParseImmediateValue(contentNode.ChildNodes[0],block,out initialValue))
                {
                    return false;
                }
                res.InitialValues = new ValueBase[1] { initialValue };

                //Mark attribute
                res.IsConstant = false;
                res.IsNeedInitialization = true;
            }
            else if (node.ChildNodes.Count == 10 || node.ChildNodes.Count == 11)
            { //.variable + name + [ + number + ] + [variabledefPositionHint] + = + { + contents + } + ;
                //Name
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;

                //PositionHint
                if (node.ChildNodes.Count == 11)
                    res.PositionHint = (int)(node.ChildNodes[5].ChildNodes[2].Token.Value);

                //Content
                ParseTreeNode contentNode = node.ChildNodes[node.ChildNodes.Count - 3];
                List<ValueBase> initialValues;
                if (!ParseVariableInitialValues(contentNode,block,out initialValues))
                {
                    return false;
                }

                //Fill by zero
                int length = (int)node.ChildNodes[3].Token.Value;
                if (length <= 0)
                {
                    ReportError("Variable","Variable array length is invalid.",node.ChildNodes[3]);
                    return false;
                }
                if (length < initialValues.Count)
                {
                    ReportError("Variable","Variable array length is too small for its initial values.",node.ChildNodes[3]);
                    return false;
                }
                while (initialValues.Count < length)
                    initialValues.Add(ValueBase.Zero);
                res.InitialValues = initialValues.ToArray();

                //Mark attribute
                res.IsConstant = false;
                res.IsNeedInitialization = true; //配列に対するインデックスアクセス方法を提供しないので，要素が最適化されてもかまわない一時変数のために配列を使うことはないだろう
            }

            return true;
        }

        private bool ParseVariableInitialValues(ParseTreeNode node,Block block,out List<ValueBase> res)
        {
            res = new List<ValueBase>();
            foreach (var e in node.ChildNodes)
            {
                if (e.Term.Name == "variabledef-content")
                {
                    ValueBase val;
                    if (!ParseImmediateValue(e.ChildNodes[0],block,out val))
                    {
                        return false;
                    }

                    res.Add(val);
                }
            }

            return true;
        }

        private bool ParseConstant(ParseTreeNode node,Block block,out Variable res)
        {
            res = new Assemble.Variable();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.DefinedBlock = block;
            if (node.ChildNodes.Count == 5)
            { //.constant + name + = + content + ;
                //Name
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;

                //Content
                ValueBase initialValue;
                if (!ParseImmediateValue(node.ChildNodes[3].ChildNodes[0],block,out initialValue))
                {
                    return false;
                }
                res.InitialValues = new ValueBase[1] { initialValue };

                //Mark attribute
                res.IsConstant = true;
            }
            else if (node.ChildNodes.Count == 10)
            { //.constant + name + [ + number + ] + = + { + contents + } + ;
                //Name
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;

                //Content
                List<ValueBase> initialValues;
                if (!ParseConstantInitialValues(node.ChildNodes[7],block,out initialValues))
                {
                    return false;
                }

                //Fill by zero
                int length = (int)node.ChildNodes[3].Token.Value;
                if (length <= 0)
                {
                    ReportError("Constant","Constant array length is invalid.",node.ChildNodes[3]);
                    return false;
                }
                if (length < initialValues.Count)
                {
                    ReportError("Constant","Constant array length is too small for its initial values.",node.ChildNodes[3]);
                    return false;
                }
                while (initialValues.Count < length)
                    initialValues.Add(ValueBase.Zero);
                res.InitialValues = initialValues.ToArray();

                //Mark attribute
                res.IsConstant = true;
            }
            return true;
        }

        private bool ParseConstantInitialValues(ParseTreeNode node,Block block,out List<ValueBase> res)
        {
            res = new List<ValueBase>();
            foreach (var e in node.ChildNodes)
            {
                if (e.Term.Name == "constantdef-content")
                {
                    ValueBase val;
                    if (!ParseImmediateValue(e.ChildNodes[0],block,out val))
                    {
                        return false;
                    }

                    res.Add(val);
                }
            }

            return true;
        }
        
        private bool ParseSymbol(ParseTreeNode node,Block block,out Symbol res)
        {
            res = new Assemble.Symbol();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.DefinedBlock = block;
            { //.symbol + name + = + content + ;
                res.Name = node.ChildNodes[1].ChildNodes[0].Token.Text;

                var contentNode = node.ChildNodes[3];
                if (contentNode.ChildNodes[0].Term.Name == "operand-register")
                {
                    if (!ParseRegisterInfo(contentNode.ChildNodes[0], block, out res.Content))
                        return false;
                }
                else if (contentNode.ChildNodes.Count == 2)
                { //@ + number
                    res.Content = new ValueInteger((int)contentNode.ChildNodes[1].Token.Value);
                }
                else
                { //Immediate
                    if (!ParseImmediateValue(contentNode.ChildNodes[0],block,out res.Content))
                        return false;
                }
            }

            return true;
        }

        private bool ParseImmediateValue(ParseTreeNode node,Block block,out ValueBase res)
        {
            res = null;
            //Determine pattern
            if (node.Term.Name == "value-integer")
            {
                res = new Assemble.ValueInteger((int)node.ChildNodes[0].Token.Value);
                res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
                return true;
            }
            else if (node.Term.Name == "value-character")
            {
                res = new Assemble.ValueChar((char)node.ChildNodes[0].Token.Value);
                res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
                return true;
            }
            else if (node.Term.Name == "value-referaddress")
            {
                if (node.ChildNodes.Count == 2)
                {
                    res = new Assemble.ValueReference(node.ChildNodes[1].Token.Text,block);
                    res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
                }
                else
                {
                    res = new Assemble.ValueReference(node.ChildNodes[1].Token.Text,(int)node.ChildNodes[3].Token.Value,block);
                    res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
                }
                return true;
            }

            ReportError("ImmediateValue","Unknown immediate value format.",node);
            return false;
        }

        private bool ParseRegisterInfo(ParseTreeNode node, Block block, out ValueBase res)
        {
            res = null;
            //Determine pattern
            if (node.Term.Name == "operand-register")
            {
                var spcfyNode = node.ChildNodes[1];
                if (spcfyNode.Term.Name == "identifier")
                {
                    res = new ValueRegister(new RegisterInfo(spcfyNode.Token.Text));
                }
                else
                {
                    res = new ValueRegister(new RegisterInfo((int)spcfyNode.Token.Value));
                }
                return true;
            }
            
            ReportError("RegisterImmediate", "Unknown immediate value format.", node);
            return false;
        }

        private bool ParseMacroDefinition(ParseTreeNode node,Block block,out MacroDefinition res)
        {
            res = new Assemble.MacroDefinition();
            res.AssemblePosition = new Assemble.AssemblePosition(this.ParsingFilePath,node);
            res.ParentBlock = block;
            res.DefinedBlock = block;
            res.RootCode = block.RootCode;
            { //macro + name + ( + args + ) + { + contents + }
                //Name
                res.Name = node.ChildNodes[1].Token.Text;
                 
                //Arguments
                if (!ParseMacroDefinitionArguments(node.ChildNodes[3],block,out res.Arguments))
                {
                    return false;
                }

                //Contents
                Block blkedRes = res;
                if (!ParseBlockContents(node.ChildNodes[6],ref blkedRes))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ParseMacroDefinitionArguments(ParseTreeNode node,Block block,out MacroDefinition.ArgumentElement[] res)
        {
            res = null;
            List<MacroDefinition.ArgumentElement> resList = new List<Assemble.MacroDefinition.ArgumentElement>();
            foreach (var e in node.ChildNodes)
            {
                MacroDefinition.ArgumentElement arg;
                if (!ParseMacroDefinitionArgument(e,block,out arg))
                {
                    return false;
                }

                resList.Add(arg);
            }

            res = resList.ToArray();
            return true;
        }

        private bool ParseMacroDefinitionArgument(ParseTreeNode node,Block block,out MacroDefinition.ArgumentElement res)
        {
            res = new Assemble.MacroDefinition.ArgumentElement();
            { //name
                res.Name = node.ChildNodes[0].Token.Text;
                return true;
            }
        }
    }
}
