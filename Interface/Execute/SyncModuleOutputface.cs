using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    /// <summary>
    /// サイクルに同期して出力値を決定するモジュールのための出力窓口クラス
    /// </summary>
    public class SyncModuleOutputface<ValueType> : ModuleOutputfaceBase<ValueType> where ValueType : struct
    {
        public bool IsChangedInThisCycle
        {
            get;
            protected set;
        }
        private ValueType NextValue
        {
            get;
            set;
        }
        public override ValueType Value
        {
            get;
            protected set;
        }
        private ModuleInputface<ValueType> Input
        {
            get;
            set;
        }


        public SyncModuleOutputface(ValueType initialValue = default(ValueType))
        {
            this.Value = initialValue;
            IsChangedInThisCycle = true;
            Input = new Execute.ModuleInputface<ValueType>();
        }

        public void AutoAssign(ModuleOutputfaceBase<ValueType> input)
        {
            this.Input.BindSource(input);
        }

        public void Assign(ValueType val)
        {
            IsChangedInThisCycle = true;
            NextValue = val;
        }

        public override void StepCycleLockPhase()
        {
            if (Input.SourceFace != null)
            {
                NextValue = Input.Get();
                IsChangedInThisCycle = true;
            }
        }

        public override void StepCycleApplyPhase()
        {
            if (IsChangedInThisCycle)
            {
                Value = NextValue;
                IsChangedInThisCycle = false;
            }
        }
    }
}
