using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Interface.Assemble.Parsing
{
    [Language("AssemblyGrammar","1.0","AssemblyGrammer")]
    public class AssemblyGrammar : Irony.Parsing.Grammar
    {
        public AssemblyGrammar()
        {
            #region Declare Terminals Here
            CommentTerminal blockComment = new CommentTerminal("block-comment","/*","*/");
            CommentTerminal lineComment = new CommentTerminal("line-comment","//","\r","\n","\u2085","\u2028","\u2029");
            //CommentTerminal sharpComment = new CommentTerminal("sharp-comment","#","\r","\n","\u2085","\u2028","\u2029");
            NonGrammarTerminals.Add(blockComment);
            NonGrammarTerminals.Add(lineComment);
            //NonGrammarTerminals.Add(sharpComment);

            StringLiteral stringLiteral = new StringLiteral("string-literal","\"");
            StringLiteral charLiteral = new StringLiteral("char-literal","\'",StringOptions.IsChar);
            NumberLiteral number = new NumberLiteral("number",NumberOptions.AllowSign | NumberOptions.IntOnly);
            number.AddPrefix("0x",NumberOptions.Hex);
            number.AddPrefix("0b",NumberOptions.Binary);
            IdentifierTerminal identifier = TerminalFactory.CreateCSharpIdentifier("identifier");
            #endregion

            #region Declare NonTerminals Here;
            NonTerminal fileroot = new NonTerminal("fileroot");

            NonTerminal rootdefs = new NonTerminal("root-defines");
            NonTerminal rootdef = new NonTerminal("root-define");

            NonTerminal usingDesc = new NonTerminal("using-description");

            NonTerminal memorySizeDesc = new NonTerminal("memorysize-description");
            NonTerminal targetISADesc = new NonTerminal("targetisa-description");

            NonTerminal label = new NonTerminal("label");

            NonTerminal operandReg = new NonTerminal("operand-register");
            NonTerminal operandRef = new NonTerminal("operand-reference");
            NonTerminal operandImm = new NonTerminal("operand-immediate");

            NonTerminal valueInt = new NonTerminal("value-integer");
            NonTerminal valueChar = new NonTerminal("value-character");
            NonTerminal valueRefAddr = new NonTerminal("value-referaddress");

            NonTerminal labeledStatement = new NonTerminal("labeled-statement");
            NonTerminal statement = new NonTerminal("statement");

            NonTerminal instr = new NonTerminal("instruction");
            NonTerminal instrLabels = new NonTerminal("instruction-labels");
            NonTerminal instrNemonic = new NonTerminal("instruction-nemonic");
            NonTerminal instrOperands = new NonTerminal("instruction-operands");
            NonTerminal instrOperand = new NonTerminal("instruction-operand");
            NonTerminal labeledInstrOperand = new NonTerminal("labeled-instruction-operand");

            NonTerminal instrJump = new NonTerminal("instruction-jump");
            NonTerminal instrJumpNemonic = new NonTerminal("instruction-jump-nemonic");
            NonTerminal instrJumpTarget = new NonTerminal("instruction-jump-target");

            NonTerminal macrocall = new NonTerminal("macrocall");
            NonTerminal macrocallLabels = new NonTerminal("macrocall-labels");
            NonTerminal macrocallName = new NonTerminal("macrocall-name");
            NonTerminal macrocallOperands = new NonTerminal("macrocall-operands");
            NonTerminal macrocallOperand = new NonTerminal("macrocall-operand");

            NonTerminal symboldef = new NonTerminal("symboldef");
            NonTerminal symboldefName = new NonTerminal("symboldef-name");
            NonTerminal symboldefContent = new NonTerminal("symboldef-content");

            NonTerminal variabledef = new NonTerminal("variabledef");
            NonTerminal variabledefName = new NonTerminal("variabledef-name");
            NonTerminal variabledefContents = new NonTerminal("variabledef-contents");
            NonTerminal variabledefContent = new NonTerminal("variabledef-content");
            NonTerminal variabledefPositionHint = new NonTerminal("variabledef-poshint");

            NonTerminal constantdef = new NonTerminal("constantdef");
            NonTerminal constantdefName = new NonTerminal("constantdef-name");
            NonTerminal constantdefContents = new NonTerminal("constantdef-content");
            NonTerminal constantdefContent = new NonTerminal("constantdef-content");

            NonTerminal macrodef = new NonTerminal("macrodef");
            NonTerminal macrodefArguments = new NonTerminal("macrodef-arguments");
            NonTerminal macrodefArgument = new NonTerminal("macrodef-arguments");
            NonTerminal macrodefContents = new NonTerminal("macrodef-contents");
            NonTerminal macrodefContent = new NonTerminal("macrodef-content");

            NonTerminal sectiondef = new NonTerminal("sectiondef");
            NonTerminal sectiondefAttributes = new NonTerminal("sectiondef-attributes");
            NonTerminal sectiondefAttribute = new NonTerminal("sectiondef-attribute");
            NonTerminal sectiondefContents = new NonTerminal("sectiondef-contents");
            NonTerminal sectiondefContent = new NonTerminal("sectiondef-content");

            NonTerminal blockdef = new NonTerminal("blockdef");
            NonTerminal blockdefContents = new NonTerminal("blockdef-contents");
            NonTerminal blockdefContent = new NonTerminal("blockdef-content");

            NonTerminal debugprint = new NonTerminal("debugprint");
            #endregion

            #region Place Rules Here
            this.Root = fileroot;
            fileroot.Rule = rootdefs;

            rootdefs.Rule = MakeStarRule(rootdefs,rootdef);
            rootdef.Rule = sectiondef
                         | usingDesc
                         | memorySizeDesc
                         | targetISADesc;

            usingDesc.Rule = ToTerm("#include") + stringLiteral + ToTerm(";");
            memorySizeDesc.Rule = ToTerm("#memorysize") + number + ToTerm(";");
            targetISADesc.Rule = ToTerm("#targetisa") + stringLiteral + ToTerm(";");

            //Commons
            operandImm.Rule = valueInt
                            | valueChar
                            | valueRefAddr;
            valueInt.Rule = number;
            valueChar.Rule = charLiteral;
            valueRefAddr.Rule = ToTerm("&") + identifier
                              | ToTerm("&") + identifier + ToTerm("[") + number + ToTerm("]");
            operandRef.Rule = identifier;
            operandReg.Rule = ToTerm("$") + number
                            | ToTerm("$") + identifier;

            //Variable
            variabledef.Rule = ToTerm(".variable") + variabledefName + ToTerm(";") //With no initial value (these will be grouped into one variable by optimization.)
                             | ToTerm(".variable") + variabledefName + ToTerm("=") + variabledefContent + ToTerm(";") //With initial value (these will be not grouped.)
                             | ToTerm(".variable") + variabledefName + ToTerm("[") + number + ToTerm("]") + ToTerm("=") + ToTerm("{") + variabledefContents + ToTerm("}") + ToTerm(";")
                             | ToTerm(".variable") + variabledefName + variabledefPositionHint + ToTerm(";") //With no initial value (these will be grouped into one variable by optimization.)
                             | ToTerm(".variable") + variabledefName + variabledefPositionHint + ToTerm("=") + variabledefContent + ToTerm(";") //With initial value (these will be not grouped.)
                             | ToTerm(".variable") + variabledefName + ToTerm("[") + number + ToTerm("]") + variabledefPositionHint + ToTerm("=") + ToTerm("{") + variabledefContents + ToTerm("}") + ToTerm(";"); ;
            variabledefPositionHint.Rule = ToTerm("(") + ToTerm("@") + number + ToTerm(")");
            variabledefName.Rule = identifier;
            variabledefContents.Rule = MakeStarRule(variabledefContents,ToTerm(","),variabledefContent);
            variabledefContent.Rule = valueInt
                                    | valueChar
                                    | valueRefAddr;

            //Constant
            constantdef.Rule = ToTerm(".constant") + constantdefName + ToTerm("=") + constantdefContent + ToTerm(";")
                             | ToTerm(".constant") + constantdefName + ToTerm("[") + number + ToTerm("]") + ToTerm("=") + ToTerm("{") + constantdefContents + ToTerm("}") + ToTerm(";");
            constantdefName.Rule = identifier;
            constantdefContents.Rule = MakeStarRule(constantdefContents,ToTerm(","),constantdefContent);
            constantdefContent.Rule = valueInt
                                    | valueChar
                                    | valueRefAddr;

            //Statement instr | callmacro
            debugprint.Rule = ToTerm(">>>>") + stringLiteral;
            label.Rule = identifier + ToTerm(":");
            labeledStatement.Rule = label + labeledStatement
                                  | statement;
            statement.Rule = instr
                           | macrocall;
            //Instruction
            instr.Rule = instrNemonic + instrOperands + ToTerm(";")
                       | instrNemonic + instrOperands + ToTerm(";") + debugprint
                       | instrNemonic + instrOperands + instrJump + ToTerm(";")
                       | instrNemonic + instrOperands + instrJump + ToTerm(";") + debugprint;
            instrNemonic.Rule = identifier;
            instrLabels.Rule = MakeStarRule(instrLabels,label);
            instrOperands.Rule = MakeStarRule(instrOperands,ToTerm(","),labeledInstrOperand);
            labeledInstrOperand.Rule = label + labeledInstrOperand
                                     | instrOperand;
            instrOperand.Rule = operandRef
                              | operandImm
                              | operandReg;

            //Instruction Jump
            instrJump.Rule = ToTerm("-<") + instrJumpNemonic + instrJumpTarget;
            instrJumpNemonic.Rule = identifier;
            instrJumpTarget.Rule = operandRef
                              | operandImm
                              | operandReg;

            //Macrocall
            macrocall.Rule = ToTerm("~") + macrocallName + ToTerm("(") + macrocallOperands + ToTerm(")") + ToTerm(";")
                           | ToTerm("~") + macrocallName + ToTerm("(") + macrocallOperands + ToTerm(")") + ToTerm(";") + debugprint;
            macrocallName.Rule = identifier;
            macrocallOperands.Rule = MakeStarRule(macrocallOperands,ToTerm(","),macrocallOperand);
            macrocallOperand.Rule = operandRef
                                  | operandImm
                                  | operandReg;

            //Macrodefinition
            macrodef.Rule = ToTerm("macro") + identifier + ToTerm("(") + macrodefArguments + ToTerm(")") +
                                    ToTerm("{") + macrodefContents + ToTerm("}");
            macrodefArguments.Rule = MakeStarRule(macrodefArguments,ToTerm(","),macrodefArgument);
            macrodefArgument.Rule = identifier;
            macrodefContents.Rule = MakeStarRule(macrodefContents,macrodefContent);
            macrodefContent.Rule = blockdef
                                 | labeledStatement
                                 | variabledef
                                 | constantdef
                                 | macrodef;

            //Symboldefinition
            symboldef.Rule = ToTerm(".symbol") + symboldefName + ToTerm("=") + symboldefContent + ToTerm(";")
                           | ToTerm(".symbol") + symboldefName + ToTerm(":=") + symboldefContent + ToTerm(";");
            symboldefName.Rule = identifier;
            symboldefContent.Rule = operandReg
                                  | ToTerm("@") + number
                                  | valueInt
                                  | valueChar
                                  | valueRefAddr;

            //Section 属性を設定可能
            sectiondef.Rule = ToTerm("section") + identifier + ToTerm("{") + sectiondefAttributes + sectiondefContents + ToTerm("}");
            sectiondefAttributes.Rule = MakeStarRule(sectiondefAttributes,sectiondefAttribute);
            sectiondefAttribute.Rule = ToTerm(".startup");
            sectiondefContents.Rule = MakeStarRule(sectiondefContents,sectiondefContent);
            sectiondefContent.Rule = blockdef
                                   | labeledStatement
                                   | variabledef
                                   | constantdef
                                   | symboldef
                                   | macrodef;

            //Block ただのスコープ
            blockdef.Rule = ToTerm("{") + blockdefContents + ToTerm("}")
                          | identifier + ToTerm("{") + blockdefContents + ToTerm("}");
            blockdefContents.Rule = MakeStarRule(blockdefContents,blockdefContent);
            blockdefContent.Rule = blockdef
                                 | labeledStatement
                                 | variabledef
                                 | constantdef
                                 | symboldef
                                 | macrodef;

            //セクションで定義された変数や定数・命令は最適化されて好き勝手な場所に配置される．
            //デバッグ情報からその変数がどこのセクションで定義され，どこに配置されているかが確認できる
            //セクションはその親のセクションを持っている
            //ラベルを参照する際にはそのラベルが使用されたセクションから順に上をたどって参照先を検索する
            //マクロが展開されるとその中身は新しいブロックの中に展開される

            //.startup
            //.variable NAME = 
            //.constant NAME = 0x23a1s,65409

            //'srl: rd,rs,rt
            //{
            //    
            //}
            //
            //@ROM
            //{
            //    
            //}
            #endregion

            #region Define Keywords
            this.MarkReservedWords("break","continue","else","extern","for",
                "if","int","return","static","void","while");
            #endregion
        }
    }
}
