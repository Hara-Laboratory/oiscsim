using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    public interface ISyncObject
    {
        void StepCycleLockPhase();
        void StepCycleApplyPhase();
    }
}
