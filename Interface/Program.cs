using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    class Program
    {
        static void Main(string[] args)
        {
/*
            args = new string[] {
               //@"sng4/motiondetect_woExt.S",
               //@"sbrsc/motiondetect.S",
               //@"sbrsc2/motiondetect.S",
               @"sbrsc/quick_pivot_dbrnch.S",
               // @"sbrsc2/quick_pivot_dbrnch.S",
                //@"yokota_qsort/quick-ref.S",
                //@"sng4/diffuse.S",
                "-l", "ipPe",
            // "-o", "a.hex" };
            "-e", "cycle",
            "-emv",
            "-emdump", @"aa.bin" };
*/
         
            //Parse arguments
            string inputPath = null, outputPath = null, executeMemoryDumpPath = null;
            bool executeRequest = false;
            bool commandLineMode = false;
            bool executeMemoryDump = false, executeMemoryShowCodeInstr = true, executeMemoryShowCodeVariable = false, executeMemoryShowStack = false;
            string executeMode = "instr";
            for (int argIdx = 0;argIdx < args.Length; argIdx++)
            {
                switch (args[argIdx])
                {
                    case "-C":
                    case "-c":
                        commandLineMode = true;
                        break;
                    case "-L":
                    case "-l":
                        //Log mode
                        if (argIdx + 1 >= args.Length)
                        {
                            Console.WriteLine("Specify arugment designating log level.");
                            Console.WriteLine();
                            return;
                        }
                        MessageManager.SetLevel(args[argIdx + 1]);
                        argIdx++;
                        break;
                    case "-o":
                        //Output path
                        if (argIdx + 1 >= args.Length)
                        {
                            Console.WriteLine("Specify arugment designating output file path.");
                            Console.WriteLine();
                            return;
                        }
                        outputPath = args[argIdx + 1];
                        argIdx++;
                        break;
                    case "-e":
                        //Execute request
                        executeRequest = true;
                        if (argIdx + 1 < args.Length && !args[argIdx + 1].StartsWith("-"))
                        {
                            executeMode = args[argIdx + 1];
                            argIdx++;
                        }
                        break;
                    case "-emi":
                        //Execute memory code
                        executeMemoryShowCodeInstr = true;
                        break;
                    case "-emv":
                        //Execute memory code
                        executeMemoryShowCodeVariable = true;
                        break;
                    case "-ems":
                        //Execute memory stack
                        executeMemoryShowStack = true;
                        break;
                    case "-emdump":
                        //Execute memory dump
                        if (argIdx + 1 >= args.Length)
                        {
                            Console.WriteLine("Specify arugment designating execute memory dump file path.");
                            Console.WriteLine();
                            return;
                        }
                        executeMemoryDump = true;
                        executeMemoryDumpPath = args[argIdx + 1];
                        argIdx++;
                        break;
                    default:
                        //Input path
                        inputPath = args[argIdx];
                        break;
                }
            }
            if (inputPath == null)
            {
                Console.WriteLine("No input assembler file specified.");
                Console.WriteLine("./oiscas <inputfile> [-L {pP: progress, eE: execution, iI: infomation} ] [-o <output-path>] [-e]");
                Console.WriteLine();
                return;
            }
            MessageManager.MessageLevel |= enumMessageLevel.ProgressLog;
                                        ;//|  enumMessageLevel.ExecutionLog;
            
            MessageManager.ShowLine("Preprocess assmebly file...", enumMessageLevel.ProgressLog);
            Assemble.AssemblyCode code;
            MessageManager.GoInnerTab();
            {
                MessageManager.ShowLine("Loading assembly...", enumMessageLevel.DetailProgressLog);
                if (!Assemble.AssemblyCode.TryParseFromFile(inputPath, out code))
                {
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }

                MessageManager.ShowLine("Pre analysis:", enumMessageLevel.InfomationDetailLog);
                MessageManager.GoInnerTab();
                //MessageManager.ShowLine(code.ToString(), enumMessageLevel.InfomationDetailLog);
                MessageManager.GoOuterTab();

                if (!code.ProcessForAssemble())
                {
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }

                MessageManager.ShowLine("Post analysis:", enumMessageLevel.InfomationDetailLog);
                MessageManager.GoInnerTab();
                //MessageManager.ShowLine(code.ToString(), enumMessageLevel.InfomationDetailLog);
                MessageManager.GoOuterTab();

                MessageManager.ShowLine("Variable analyze result:", enumMessageLevel.InfomationLog);
                MessageManager.GoInnerTab();
                code.VariableAnalyzeResult.ShowAnalyzeResult(enumMessageLevel.InfomationDetailLog);
                MessageManager.GoOuterTab();
            }
            MessageManager.GoOuterTab();

            MessageManager.ShowLine("Assemble for ISA...", enumMessageLevel.ProgressLog);
            Execute.ExecuteSetupData setupData;
            MessageManager.GoInnerTab();
            {
                Assemble.AssemblerBase assembler;
                if (!ISADispatcher.DefaultDispatcher.CreateAssembler(code.TargetISAName, out assembler))
                {
                    MessageManager.ShowLine($"Unknown ISA name '{code.TargetISAName}'!", enumMessageLevel.ProgressLog);
                    MessageManager.ShowLine($"(Available ISA: {string.Join(", ", ISADispatcher.DefaultDispatcher.GetAvailableISA())})", enumMessageLevel.ProgressLog);
                    Console.WriteLine();
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }
                if (!assembler.Assemble(code, out setupData))
                {
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }

                MessageManager.ShowLine("Execute setup data:", enumMessageLevel.InfomationDetailLog);
                MessageManager.GoInnerTab();
                setupData.ShowByMessage(enumMessageLevel.InfomationDetailLog);
                MessageManager.GoOuterTab();

                if (outputPath != null)
                {
                    MessageManager.ShowLine("Generating memory initial file", enumMessageLevel.ProgressLog);
                    MessageManager.GoInnerTab();
                    setupData.GenerateHexFile(outputPath, ISADispatcher.DefaultDispatcher.GetMemorySplittedWithLowHigh(code.TargetISAName));
                    MessageManager.GoOuterTab();
                }
            }
            MessageManager.GoOuterTab();

            if (!executeRequest)
            {
                MessageManager.ShowLine("", enumMessageLevel.ProgressLog);
                if (!commandLineMode)
                    Console.ReadLine();
                return;
            }

            MessageManager.ShowLine($"Starting '{executeMode}' level simulation...", enumMessageLevel.ProgressLog);
            Execute.SimulatorModelBase simulator;
            MessageManager.GoInnerTab();
            {
                if (!ISADispatcher.DefaultDispatcher.CreateSimulator(code.TargetISAName, executeMode, out simulator))
                {
                    MessageManager.ShowLine($"Unknown ISA simulator name '{executeMode}'!", enumMessageLevel.ProgressLog);
                    MessageManager.ShowLine($"(Available simulator: {string.Join(", ", ISADispatcher.DefaultDispatcher.GetAvailableSimulator(code.TargetISAName))})", enumMessageLevel.ProgressLog);
                    Console.WriteLine();
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }

                if (!simulator.SetupFromSetupData(setupData))
                {
                    Console.WriteLine();
                    if (!commandLineMode)
                        Console.ReadLine();
                    return;
                }
                
                System.Threading.Tasks.Task simThread = new Task(() =>
                {
                    while (!simulator.IsHalted)
                    {
                        if (!simulator.StepCycle())
                        {
                            MessageManager.ShowLine("EXECUTION FAILURE", enumMessageLevel.ProgressLog);
                            //simulator.ShowMemoryDumpByMessage(true, true, true, enumMessageLevel.ProgressLog);
                            MessageManager.ShowLine("EXECUTION FAILURE", enumMessageLevel.ProgressLog);
                            if (!commandLineMode)
                                Console.ReadLine();
                        }
                    }
                });
                simThread.Start();
                Console.WriteLine();
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                while (!simThread.Wait(150))
                {
                    if (!MessageManager.TestLevel(enumMessageLevel.ExecutionLog))
                        Console.Write($"\r    { simulator.CycleCount.ToString() } cycles, { sw.Elapsed.ToString("hh\\:mm\\:ss") } elapsed... [{ simulator.PrintExecutionTraceData(40) }]");
                }
                Console.WriteLine();
                MessageManager.ShowLine($"Finished by { simulator.CycleCount } cycles", enumMessageLevel.ProgressLog);

                if (executeMemoryDump)
                {
                    using (var fso = System.IO.File.Create(executeMemoryDumpPath))
                        simulator.SaveMemoryDump(fso);
                }

                {
                    if (executeMemoryShowCodeInstr || executeMemoryShowCodeVariable || executeMemoryShowStack)
                    {
                        MessageManager.ShowLine("Post execution memory dump:", enumMessageLevel.ProgressLog);
                        simulator.ShowMemoryDumpByMessage(executeMemoryShowCodeInstr, executeMemoryShowCodeVariable, executeMemoryShowStack, enumMessageLevel.ProgressLog);
                        MessageManager.ShowLine($"Finished by { simulator.CycleCount } cycles", enumMessageLevel.ProgressLog);
                    }
                }

                {
                    simulator.ShowExecutionInfo(enumMessageLevel.ProgressLog);
                }
            }
            MessageManager.GoOuterTab();


            MessageManager.ShowLine("", enumMessageLevel.ProgressLog);
            if (!commandLineMode)
                Console.ReadLine();
        }
    }
}
