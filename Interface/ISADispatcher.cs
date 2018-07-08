using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface
{
    public class ISADispatcher
    {
        public class Element
        {
            public string ISAName;
            public Func<Assemble.AssemblerBase> AssemblerInstansinater;
            public bool IsMemorySplittedWithLowHight;
            public Dictionary<string,Func<Execute.SimulatorModelBase>> SimulatorInstansinaters;
           
            public Element()
            {
                SimulatorInstansinaters = new Dictionary<string,Func<Execute.SimulatorModelBase>>();
            }

            public void Add(string name,Func<Execute.SimulatorModelBase> func)
            {
                this.SimulatorInstansinaters.Add(name,func);
            }
        }
        Dictionary<string,Element> Elements;

        public static readonly ISADispatcher DefaultDispatcher = new ISADispatcher();
        static ISADispatcher()
        {
            var sng4xElem = DefaultDispatcher.Add("sng4x",Assemble.SUBNEG4XAssembler.Instansinate, false);
            sng4xElem.Add("instr",Execute.Subneg4XInstructionModel.Instansinate);
            sng4xElem.Add("instruction",Execute.Subneg4XInstructionModel.Instansinate);
            sng4xElem.Add("cycle",Execute.Subneg4XCycleModel.Instansinate);

            var sng4x16Elem = DefaultDispatcher.Add("sng4x16", Assemble.SUBNEG4XAssembler.InstansinateBySixteen, false);
            //sng4x16Elem.Add("instr", Execute.Subneg4XInstructionModel.Instansinate);
            //sng4x16Elem.Add("instruction", Execute.Subneg4XInstructionModel.Instansinate);
            //sng4x16Elem.Add("cycle", Execute.Subneg4XCycleModel.Instansinate);

            var subRiscElem = DefaultDispatcher.Add("subrisc",Assemble.SubRiscAssembler.Instansinate, true);
            subRiscElem.Add("instr",Execute.SubRiscInstructionModel.InstansinateWithoutDelayBranch);
            subRiscElem.Add("instruction",Execute.SubRiscInstructionModel.InstansinateWithoutDelayBranch);
            subRiscElem.Add("cycle", Execute.SubRISCCycleModel.InstansinateWithoutDelayBranch);

            var subRiscDbElem = DefaultDispatcher.Add("subrisc-delaybranch", Assemble.SubRiscAssembler.Instansinate, true);
            subRiscDbElem.Add("instr", Execute.SubRiscInstructionModel.InstansinateWithDelayBranch);
            subRiscDbElem.Add("instruction", Execute.SubRiscInstructionModel.InstansinateWithDelayBranch);
            subRiscDbElem.Add("cycle", Execute.SubRISCCycleModel.InstansinateWithDelayBranch);
            
            var subRisc2DbElem = DefaultDispatcher.Add("subrisc2-delaybranch", Assemble.SubRisc2Assembler.Instansinate, true);
            subRisc2DbElem.Add("instr", Execute.SubRisc2InstructionModel.Instansinate);
            subRisc2DbElem.Add("instruction", Execute.SubRisc2InstructionModel.Instansinate);
            subRisc2DbElem.Add("cycle", Execute.SubRISC2CycleModel.InstansinateWithDelayBranch);
        }

        public ISADispatcher()
        {
            Elements = new Dictionary<string,Interface.ISADispatcher.Element>();
        }

        public Element Add(string isaname,Func<Assemble.AssemblerBase> func, bool memSplittedWithLowHigh)
        {
            Element e = new Element()
            {
                ISAName = isaname,
                AssemblerInstansinater = func,
                IsMemorySplittedWithLowHight = memSplittedWithLowHigh
            };
            this.Elements.Add(isaname,e);
            return e;
        }

        public bool CreateAssembler(string isaname,out Assemble.AssemblerBase res)
        {
            res = null;

            Element e;
            if (!Elements.TryGetValue(isaname,out e))
                return false;

            res = e.AssemblerInstansinater();
            return true;
        }

        public bool GetMemorySplittedWithLowHigh(string isaname)
        {
            Element e;
            if (!Elements.TryGetValue(isaname, out e))
                return false;

            return e.IsMemorySplittedWithLowHight;
        }

        public bool CreateSimulator(string isaname,string modelname,out Execute.SimulatorModelBase res)
        {
            res = null;

            Element e;
            if (!Elements.TryGetValue(isaname,out e))
                return false;

            Func<Execute.SimulatorModelBase> es;
            if (!e.SimulatorInstansinaters.TryGetValue(modelname,out es))
                return false;
            
            res = es();
            return true;
        }

        public string[] GetAvailableISA()
        {
            return Elements.Keys.ToArray();
        }
        public string[] GetAvailableSimulator(string isaname)
        {
            Element e;
            if (!Elements.TryGetValue(isaname,out e))
                return new string[0];

            return e.SimulatorInstansinaters.Keys.ToArray();
        }
    }
}
