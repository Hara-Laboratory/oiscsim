using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Interface.Assemble
{
    public class AssemblyCode
    {
        public int MinMemorySize = 1;
        public string TargetISAName = "sng4x";
        public List<string> LoadedFiles = new List<string>();
        public List<Section> Sections = new List<Section>();
        public VariableAnalyzeInfo VariableAnalyzeResult = null;


        public AssemblyCode()
        {
        }

        public static bool TryParse(string source,string filePath,out AssemblyCode res,List<AssembleError> errorList)
        {
            res = new Assemble.AssemblyCode();
            return TryParseAndAdd(source,filePath,ref res,errorList);
        }

        public static bool TryParseAndAdd(string source,string filePath,ref AssemblyCode res,List<AssembleError> errorList)
        {
            //Convert filePath to absolute one
            filePath = System.IO.Path.GetFullPath(filePath);
            MessageManager.ShowLine($"Reading assembly file \"{ filePath }\" ...",enumMessageLevel.DetailProgressLog);

            //Parse text
            MessageManager.ShowLine($"Parsing text...",enumMessageLevel.DetailProgressLog);
            Grammar grammar = new Parsing.AssemblyGrammar();
            Parser parser = new Parser(grammar);
            ParseTree tree = parser.Parse(source);
            if (tree.HasErrors())
            {
                foreach (var e in tree.ParserMessages)
                {
                    errorList.Add(new Assemble.AssembleError()
                    {
                        Title = "Text parser",
                        Detail = e.Message + $" at { e.ParserState.Name } state",
                        Position = new Assemble.AssemblePosition(filePath,e.Location.Line,e.Location.Column)
                    });
                }
                res = null;
                return false;
            }

            //Parse tree
            MessageManager.ShowLine($"Parsing grammer tree...",enumMessageLevel.DetailProgressLog);
            ParseTreeNode root = tree.Root;
            Parsing.AssemblyParser asmParser = new Parsing.AssemblyParser();
            List<string> includeFiles;
            if (!asmParser.ParseAndAdd(root,filePath,res,out includeFiles))
                return false;
            res.LoadedFiles.Add(filePath);

            //Process include files
            MessageManager.ShowLine($"Analyze include files...",enumMessageLevel.DetailProgressLog);
            for (int idx = 0; idx < includeFiles.Count; idx++)
            {
                string includeFilePath = includeFiles[idx];

                //Convert filePath to absolute one
                includeFilePath = System.IO.Path.GetDirectoryName(filePath) + "/" + includeFilePath;
                includeFilePath = System.IO.Path.GetFullPath(includeFilePath);

                //Avoid include loop
                if (res.LoadedFiles.Contains(includeFilePath))
                    continue;

                if (!TryParseAndAddFromFile(includeFilePath,ref res,errorList))
                    return false;
                res.LoadedFiles.Add(includeFilePath);
            }

            return true;
        }

        public static bool TryParseFromFile(string filePath,out AssemblyCode res)
        {
            List<AssembleError> errorList = new List<Assemble.AssembleError>();
            if (!TryParseFromFile(filePath,out res,errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            return true;
        }

        public static bool TryParseFromFile(string filePath,out AssemblyCode res,List<AssembleError> errorList)
        {
            if (!System.IO.File.Exists(filePath))
            {
                errorList.Add(new AssembleError()
                {
                    Title = "Assemble",
                    Detail = $"Including file { filePath } not found.",
                    Position = new AssemblePosition() { FilePath = filePath }
                });
                res = null;
                return false;
            }
            string asmSource = System.IO.File.ReadAllText(filePath);
            if (!TryParse(asmSource,filePath,out res,errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            return true;
        }

        public static bool TryParseAndAddFromFile(string filePath,ref AssemblyCode res,List<AssembleError> errorList)
        {
            if (!System.IO.File.Exists(filePath))
            {
                errorList.Add(new AssembleError()
                {
                    Title = "Assemble",
                    Detail = $"Including file { filePath } not found.",
                    Position = new AssemblePosition() { FilePath = filePath }
                });
                return false;
            }
            string asmSource = System.IO.File.ReadAllText(filePath);
            return TryParseAndAdd(asmSource,filePath,ref res,errorList);
        }

        public override string ToString()
        {
            string res = "";
            foreach (var e in Sections)
            {
                res += e.ToString() + "\r\n";
            }

            return res;
        }

        public bool FindMacrodef(string name,out MacroDefinition res)
        {
            foreach (var sect in Sections)
            {
                if (sect.FindMacrodef(name,out res,false)) //Do not search at sections's children
                    return true;
            }
            res = null;
            return false;
        }
        
        public bool ProcessForAssemble()
        {
            List<AssembleError> errorList = new List<Assemble.AssembleError>();
            if (!ProcessForAssemble(errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            return true;
        }

        public bool ProcessForAssemble(List<AssembleError> errorList)
        {
            MessageManager.ShowLine("Expanding macros...",enumMessageLevel.DetailProgressLog);
            if (!this.ExpandAllMacros(errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            MessageManager.ShowLine("Solving references...",enumMessageLevel.DetailProgressLog);
            if (!this.SolveAllReferences(errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            MessageManager.ShowLine("Analyzing variables mapping...",enumMessageLevel.DetailProgressLog);
            if (!this.AnalyzeVariables(errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            MessageManager.ShowLine("Analyzing instructions mapping...",enumMessageLevel.DetailProgressLog);
            if (!this.AnalyzeInstructions(errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            return true;
        }

        private bool ExpandAllMacros(List<AssembleError> errorList)
        {
            for (int sectIdx = 0; sectIdx < this.Sections.Count; sectIdx++)
            {
                Section sect = this.Sections[sectIdx];

                if (!sect.ExpandAllMacros(errorList))
                    return false;
            }
            return true;
        }

        public bool FindIdentifier(string name,IdentifierType type,out IdentifierSearchResult res)
        {
            res = new Assemble.IdentifierSearchResult();
            foreach (var sect in Sections)
            {
                if (sect.FindIdentifier(name,type,out res,false)) //Do not re-search this root code
                    return true;
            }

            return false;
        }

        private bool SolveAllReferences(List<AssembleError> errorList)
        {
            foreach (var sect in Sections)
            {
                if (!sect.SolveAllReferences(errorList))
                    return false;
            }

            return true;
        }

        private bool AnalyzeVariables(List<AssembleError> errorList)
        {
            this.VariableAnalyzeResult = new Assemble.VariableAnalyzeInfo();

            //Without optimization
            foreach (var sect in Sections)
            {
                if (!sect.AnalyzeNotOptimizedArrayVariables(errorList))
                    return false;
            }
            foreach (var sect in Sections)
            {
                if (!sect.AnalyzeNotOptimizedSingleVariables(errorList))
                    return false;
            }

            //With optimization
            {
                foreach (var sect in Sections)
                {
                    //Count memory usage size
                    int size = sect.AnalyzeOptimizedVariableCount();

                    //Allocate memory region
                    int regionStart = VariableAnalyzeResult.RegisterReadwriteEmptyRange(size);

                    //Bind variables with the region
                    if (!sect.AnalyzeOptimizedVariables(errorList,regionStart))
                        return false;

                    if (!sect.AnalyzePositionSpecifiedVariables())
                        return false;
                }
            }

            VariableAnalyzeResult.FlushPositionSpecifiedElements();

            return true;
        }
        
        private bool AnalyzeInstructions(List<AssembleError> errorList)
        {
            foreach (var e in Sections)
            {
                if (!e.CollectAllInstructions(errorList))
                    return false;
            }

            return true;
        }

        public static string GetBlockPathPrefix(Block block)
        {
            if (block == null)
                return "";

            string res = block.Name;
            while ((block = block.ParentBlock) != null)
            {
                res = block.Name + "." + res;
            }

            if (res.Length > 0)
                res += ".";

            return res;
        }
    }
}
