using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    /// <summary>
    /// モジュールの出力値を決定するための出力窓口クラス
    /// </summary>
    public abstract class ModuleOutputfaceBase<ValueType> : ISyncObject where ValueType : struct
    {
        public abstract ValueType Value
        {
            get;
            protected set;
        }
        

        public ModuleOutputfaceBase()
        {
        }

        public virtual void StepCycleLockPhase()
        {
        }

        public virtual void StepCycleApplyPhase()
        {
        }
    }
}
