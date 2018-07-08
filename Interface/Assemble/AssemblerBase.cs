using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public abstract class AssemblerBase
    {
        public AssemblerBase()
        {
        }

        public bool Assemble(AssemblyCode code,out Execute.ExecuteSetupData res)
        {
            List<AssembleError> errorList = new List<Interface.Assemble.AssembleError>();
            if (!Assemble(code,out res,errorList))
            {
                MessageManager.ShowErrors(errorList);
                return false;
            }

            return true;
        }

        public abstract bool Assemble(AssemblyCode code,out Execute.ExecuteSetupData res,List<AssembleError> errorList);
    }
}
