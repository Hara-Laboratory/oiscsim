using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    /// <summary>
    /// サイクルに同期して出力値を決定するモジュールのための入力窓口クラス
    /// </summary>
    public class ModuleInputface<ValueType> : ISyncObject where ValueType : struct
    {
        public ModuleOutputfaceBase<ValueType> SourceFace
        {
            get;
            protected set;
        }


        public ModuleInputface()
        {
            SourceFace = null;
        }

        public void BindSource(ModuleOutputfaceBase<ValueType> src)
        {
            SourceFace = src;
        }

        public ValueType Get()
        {
            if (SourceFace == null)
            { //入力がない場合
                MessageManager.ShowLine($"Inputface is not bind with outputface.",enumMessageLevel.ProgressLog);
                throw new Exception();
            }

            return SourceFace.Value;
        }
        
        public static implicit operator ValueType(ModuleInputface<ValueType> face)
        {
            return face.Get();
        }

        public virtual void StepCycleLockPhase()
        {
        }

        public virtual void StepCycleApplyPhase()
        {
        }
    }
}
