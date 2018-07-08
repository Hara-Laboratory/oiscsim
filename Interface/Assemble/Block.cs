using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Interface.Assemble
{
    public class Block : IHasIdentifiers
    {
        public AssemblePosition AssemblePosition;
        public string Name;
        public Block ParentBlock;
        public AssemblyCode RootCode;
        public enum enumStatementType
        {
            Instruction,
            Macrocall,
            Block
        }
        public class StatementElement
        {
            public enumStatementType Type;
            public Instruction Instruction;
            public Macrocall Macrocall;
            public Block Block;

            public override string ToString()
            {
                switch (Type)
                {
                    case enumStatementType.Instruction:
                        return Instruction.ToString();
                    case enumStatementType.Macrocall:
                        return Macrocall.ToString();
                    case enumStatementType.Block:
                        return Block.ToString();
                }
                return "?";
            }

            public StatementElement Clone(Block parentBlock)
            {
                var res = new StatementElement()
                {
                    Type = this.Type
                };
                switch (this.Type)
                {
                    case enumStatementType.Block:
                        res.Block = this.Block.Clone(parentBlock);
                        break;
                    case enumStatementType.Instruction:
                        res.Instruction = this.Instruction.Clone(parentBlock);
                        break;
                    case enumStatementType.Macrocall:
                        res.Macrocall = this.Macrocall.Clone(parentBlock);
                        break;
                }

                return res;
            }

            public bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
            {
                switch (this.Type)
                {
                    case enumStatementType.Instruction:
                        {
                            if (!Instruction.ReplaceByIdentifiers(identifiers,values,errorList))
                                return false;
                        }
                        break;
                    case enumStatementType.Macrocall:
                        {
                            if (!Macrocall.ReplaceByIdentifiers(identifiers,values,errorList))
                                return false;
                        }
                        break;
                    case enumStatementType.Block:
                        {
                            if (!Block.ReplaceByIdentifiers(identifiers,values,errorList))
                                return false;
                        }
                        break;
                }

                return true;
            }

            public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
            {
                res = new Assemble.IdentifierSearchResult();
                switch (this.Type)
                {
                    case enumStatementType.Instruction:
                        {
                            if (Instruction.FindIdentifier(name,type,out res))
                                return true;
                        }
                        break;
                    case enumStatementType.Block:
                        {
                            //Don't search sub block's contents
                            if (Block.FindIdentifierOfThis(name,type,out res))
                                return true;
                        }
                        break;
                }
                
                return false;
            }

            public bool SolveAllReferences(List<AssembleError> errorList)
            {
                switch (this.Type)
                {
                    case enumStatementType.Instruction:
                        {
                            if (!Instruction.SolveAllReferences(errorList))
                                return false;
                        }
                        break;
                    case enumStatementType.Macrocall:
                        {
                        }
                        break;
                    case enumStatementType.Block:
                        {
                            if (!Block.SolveAllReferences(errorList))
                                return false;
                        }
                        break;
                }

                return true;
            }
        }
        public List<StatementElement> Statements = new List<StatementElement>(); //機械語に変換されるので無駄な構造になるのは諦めて良い
        public List<MacroDefinition> MacroDefinitions = new List<MacroDefinition>();
        public List<Variable> Variables = new List<Variable>();
        public List<Label> Labels = new List<Label>();
        public List<Symbol> Symbols = new List<Symbol>();
        public AddressSymbolInfo PlacedInfo
        {
            get
            {
                if (Statements.Count < 0)
                    return null;

                switch (Statements[0].Type)
                {
                    case enumStatementType.Block:
                        return Statements[0].Block.PlacedInfo;
                    case enumStatementType.Instruction:
                        return Statements[0].Instruction.PlacedInfo;
                }
                return null;
            }
        }
        public int OptimizedVariableCount
        {
            get;
            protected set;
        }
        protected int OptimizedVariableCountInThis;
        protected int OptimizedVariableCountInSubblocks;
        private ConcurrentDictionary<IdentifierType,ConcurrentDictionary<string,IdentifierSearchResult>> SymbolFindCacheTable = new ConcurrentDictionary<IdentifierType,ConcurrentDictionary<string,IdentifierSearchResult>>();


        public Block()
        {
        }

        public virtual bool FindMacrodef(string name,out MacroDefinition res,bool recursive = true)
        {
            foreach (var e in MacroDefinitions)
            {
                if (e.Name != name)
                    continue;

                res = e;
                return true;
            }

            //Down to parent
            if (recursive)
            {
                if (ParentBlock != null)
                    return ParentBlock.FindMacrodef(name,out res);
                else
                    return RootCode.FindMacrodef(name,out res);
            }

            res = null;
            return false;
        }

        public bool ExpandMacro(int statementIdx,List<AssembleError> errorList)
        {
            StatementElement stmt = Statements[statementIdx];
            Macrocall macrocall = stmt.Macrocall;

            //マクロ定義を有効なスコープと全セクションから検索する
            MacroDefinition macrodef; 
            if (!FindMacrodef(macrocall.CallerName,out macrodef))
            {
                errorList.Add(new AssembleError()
                {
                    Title = "Macro expanding",
                    Detail = "Macro definition '" + macrocall.CallerName + "' not found.",
                    Position = this.AssemblePosition
                });
                return false;
            }

            //展開先のブロックを作成しする
            Block expandedBlock = new Block()
            {
                AssemblePosition = stmt.Macrocall.AssemblePosition,
                Name = stmt.Macrocall.CallerName + "_expanded",
                ParentBlock = this,
                RootCode = this.RootCode
            };
            StatementElement expandedStmt = new StatementElement()
            {
                Type = enumStatementType.Block,
                Block = expandedBlock
            };

            //マクロの内容をコピーする
            macrodef.CopyToBlock(expandedBlock);
            
            //置換を行う
            if (!expandedStmt.ReplaceByIdentifiers(macrodef.Arguments,macrocall.Operands,errorList))
                return false;

            //マクロ呼出しのラベルを展開先のブロックに設定する
            foreach (var lbl in macrocall.Labels)
            {
                var newLbl = lbl.Clone(expandedBlock.ParentBlock);
                expandedBlock.Labels.Add(newLbl);
                newLbl.SetLabelPlacedInfo(expandedBlock.PlacedInfo);
            }

            //マクロ呼出しを削除して展開先ブロックに置き換える
            Statements[statementIdx] = expandedStmt;

            return true;
        }

        public virtual bool ExpandAllMacros(List<AssembleError> errorList)
        {
            for (int stmtIdx = 0; stmtIdx < this.Statements.Count; stmtIdx++)
            {
                var stmt = this.Statements[stmtIdx];

                switch (stmt.Type)
                {
                    case enumStatementType.Macrocall:
                        //マクロ呼出しを展開する
                        if (!this.ExpandMacro(stmtIdx,errorList))
                            return false;

                        stmtIdx--;
                        break;
                    case enumStatementType.Block:
                        //ブロックの中を更に展開させる
                        if (!stmt.Block.ExpandAllMacros(errorList))
                            return false;
                        break;
                }
            }
            return true;
        }

        public override string ToString()
        {
            string res = "";
            if (Name.Length > 0)
                res += Name + "\r\n";
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

        public virtual Block Clone(Block parentBlock)
        {
            var res = new Assemble.Block()
            {
                AssemblePosition = this.AssemblePosition,
                Name = this.Name,
                RootCode = this.RootCode,
                ParentBlock = parentBlock
            };

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

            //Labels
            res.Labels = new List<Assemble.Label>();
            for (int i = 0; i < this.Labels.Count; i++)
            {
                Label clonedLbl = this.Labels[i].Clone(parentBlock);
                res.Labels.Add(clonedLbl);
                clonedLbl.SetLabelPlacedInfo(res.PlacedInfo);
            }

            //Symbols
            res.Symbols = new List<Assemble.Symbol>();
            for (int i = 0; i < this.Symbols.Count; i++)
            {
                Symbol clonedSymbl = this.Symbols[i].Clone(parentBlock);
                res.Symbols.Add(clonedSymbl);
            }
            return res;
        }

        public virtual bool ReplaceByIdentifiers(MacroDefinition.ArgumentElement[] identifiers,Macrocall.OperandElement[] values,List<AssembleError> errorList)
        {
            //Variables
            for (int i = 0; i < Variables.Count; i++)
            {
                if (!Variables[i].ReplaceByIdentifiers(identifiers,values,errorList))
                    return false;
            }

            //Symbols
            for (int i = 0; i < Symbols.Count; i++)
            {
                if (!Symbols[i].ReplaceByIdentifiers(identifiers, values, errorList))
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

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            return FindIdentifier(name,type,out res,true);
        }

        private void RegisterIdentifierToCache(string name,IdentifierSearchResult identifier)
        {
            ConcurrentDictionary<string,IdentifierSearchResult> nameDic;
            if (!this.SymbolFindCacheTable.TryGetValue(identifier.Type,out nameDic))
            {
                nameDic = new ConcurrentDictionary<string,IdentifierSearchResult>();
                this.SymbolFindCacheTable.TryAdd(identifier.Type,nameDic);
            }

            nameDic.TryAdd(name,identifier);
        }

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res,bool recursiveToRoot)
        {
            res = new IdentifierSearchResult();
            {
                int typeFlagPos = 0;
                uint typeFlagRest = (uint)type;
                while (typeFlagRest != 0)
                {
                    ConcurrentDictionary<string,IdentifierSearchResult> nameDic;
                    if (SymbolFindCacheTable.TryGetValue((IdentifierType)(1 << typeFlagPos),out nameDic) &&
                        nameDic.TryGetValue(name,out res))
                    {
                        return true;
                    }
                    typeFlagRest >>= 1;
                    typeFlagPos++;
                }
            }

            //Statements
            foreach (var stmt in Statements)
            {
                if (stmt.FindIdentifier(name,type,out res))
                {
                    RegisterIdentifierToCache(name,res);
                    return true;
                }
            }

            //Variables
            foreach (var varbl in Variables)
            {
                if (varbl.FindIdentifier(name,type,out res))
                {
                    RegisterIdentifierToCache(name,res);
                    return true;
                }
            }

            //Symbols
            foreach (var symbl in Symbols)
            {
                if (symbl.FindIdentifier(name,type,out res))
                {
                    RegisterIdentifierToCache(name,res);
                    return true;
                }
            }

            //This block (last)
            if (FindIdentifierOfThis(name,type,out res))
            {
                RegisterIdentifierToCache(name,res);
                return true;
            }

            //Parent block or sections
            if (ParentBlock != null)
            {
                if (ParentBlock.FindIdentifier(name,type,out res))
                {
                    RegisterIdentifierToCache(name,res);
                    return true;
                }
            }
            else
            {
                if (recursiveToRoot)
                {
                    if (RootCode.FindIdentifier(name,type,out res))
                    {
                        RegisterIdentifierToCache(name,res);
                        return true;
                    }
                }
            }
            
            return false;
        }

        public bool FindIdentifierOfThis(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new Assemble.IdentifierSearchResult();
            if (type.HasFlag(IdentifierType.ReferencingAddress) && this.Name == name)
            {
                res.Type = IdentifierType.ReferencingAddress;
                res.AddressIdentifier = this.PlacedInfo;
                return true;
            }

            foreach (var lbl in this.Labels)
            {
                if (lbl.FindIdentifier(name,type,out res))
                    return true;
            }
            
            return false;
        }

        public bool SolveAllReferences(List<AssembleError> errorList)
        {
            //Statements
            foreach (var stmt in Statements)
            {
                if (!stmt.SolveAllReferences(errorList))
                    return false;
            }

            //Variables
            foreach (var varbl in Variables)
            {
                if (!varbl.SolveAllReferences(errorList))
                    return false;
            }

            //Symbols
            foreach (var symbl in Symbols)
            {
                if (!symbl.SolveAllReferences(errorList))
                    return false;
            }

            return true;
        }
        
        public bool AnalyzeNotOptimizedArrayVariables(List<AssembleError> errorList)
        {
            //Variables
            //Constants
            foreach (var e in Variables)
            {
                //PositionHintは定数にはつかない前提
                if (!e.IsConstant)
                    continue;
                if (e.InitialValues.Length == 1)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadonlyVariable(e);
            }
            //Variables with initializer
            foreach (var e in Variables)
            {
                if (e.PositionHint != 0)
                    continue;
                if (e.IsConstant || !e.IsNeedInitialization)
                    continue;
                if (e.InitialValues.Length == 1)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadwriteVariableAsNew(e);
            }
            
            //Sub Blocks
            foreach (var e in Statements)
            {
                if (e.Type != enumStatementType.Block)
                    continue;

                if (!e.Block.AnalyzeNotOptimizedArrayVariables(errorList))
                    return false; //On error
            }

            return true;
        }

        public bool AnalyzeNotOptimizedSingleVariables(List<AssembleError> errorList)
        {
            //Variables
            //Constants
            foreach (var e in Variables)
            {
                //PositionHintは定数にはつかない前提
                if (!e.IsConstant)
                    continue;
                if (e.InitialValues.Length > 1)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadonlyVariable(e);
            }
            //Variables with initializer
            foreach (var e in Variables)
            {
                if (e.PositionHint != 0)
                    continue;
                if (e.IsConstant || !e.IsNeedInitialization)
                    continue;
                if (e.InitialValues.Length > 1)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadwriteVariableAsNew(e);
            }

            //Sub Blocks
            foreach (var e in Statements)
            {
                if (e.Type != enumStatementType.Block)
                    continue;

                if (!e.Block.AnalyzeNotOptimizedSingleVariables(errorList))
                    return false; //On error
            }

            return true;
        }

        public int AnalyzeOptimizedVariableCount()
        {
            OptimizedVariableCount = 0;

            //Variables without initializer
            OptimizedVariableCountInThis = 0;
            foreach (var e in Variables)
            {
                if (e.PositionHint != 0)
                    continue;
                if (e.IsConstant || e.IsNeedInitialization)
                    continue;

                OptimizedVariableCountInThis += e.InitialValues.Length;
            }

            //Sub blocks
            OptimizedVariableCountInSubblocks = 0;
            foreach (var stmt in Statements)
            {
                if (stmt.Type != enumStatementType.Block)
                    continue;

                int subCnt = stmt.Block.AnalyzeOptimizedVariableCount();
                OptimizedVariableCountInSubblocks = Math.Max(subCnt,OptimizedVariableCountInSubblocks);
            }

            OptimizedVariableCount = OptimizedVariableCountInThis + 
                                     OptimizedVariableCountInSubblocks;
            return OptimizedVariableCount;
        }
        
        public bool AnalyzeOptimizedVariables(List<AssembleError> errorList,int placeStartIdx)
        {
            int currentIdx = placeStartIdx;

            //Variables without initializer
            foreach (var e in Variables)
            {
                if (e.PositionHint != 0)
                    continue;
                if (e.IsConstant || e.IsNeedInitialization)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadwriteVariableOverbind(e,currentIdx);
                currentIdx += e.InitialValues.Length;
            }

            //Sub blocks
            foreach (var stmt in Statements)
            {
                if (stmt.Type != enumStatementType.Block)
                    continue;

                if (!stmt.Block.AnalyzeOptimizedVariables(errorList,currentIdx))
                    return false;
            }

            return true;
        }

        public bool AnalyzePositionSpecifiedVariables()
        {
            //Variables
            foreach (var e in Variables)
            {
                if (e.PositionHint == 0)
                    continue;
                if (e.IsConstant)
                    continue;

                RootCode.VariableAnalyzeResult.RegisterReadwriteVariablePositionSpecified(e);
            }

            //Sub blocks
            foreach (var stmt in Statements)
            {
                if (stmt.Type != enumStatementType.Block)
                    continue;

                if (!stmt.Block.AnalyzePositionSpecifiedVariables())
                    return false;
            }
            return true;
        }

        public virtual bool CollectAllInstructions(List<Instruction> res,List<AssembleError> errorList)
        {
            foreach (var stmt in Statements)
            {
                switch (stmt.Type)
                {
                    case enumStatementType.Macrocall:
                        errorList.Add(new AssembleError()
                        {
                            Title = "Instruction collecting",
                            Detail = "There is macrocall not expanded.",
                            Position = stmt.Macrocall.AssemblePosition
                        });
                        return false;
                    case enumStatementType.Block:
                        if (!stmt.Block.CollectAllInstructions(res,errorList))
                            return false;
                        break;
                    case enumStatementType.Instruction:
                        res.Add(stmt.Instruction);
                        break;
                }
            }
            return true;
        }
    }
}
