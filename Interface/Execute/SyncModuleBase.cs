using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    public abstract class SyncModuleBase
    {
        #region IO関連
        public List<ISyncObject> IOFaces = new List<ISyncObject>();
        public ModuleInputface<T> CreateInputface<T>() where T : struct
        {
            var res = new ModuleInputface<T>();
            this.IOFaces.Add(res);
            return res;
        }
        public SyncModuleOutputface<T> CreateSyncOutputface<T>(T initialValue = default(T)) where T : struct
        {
            var res = new SyncModuleOutputface<T>(initialValue);
            this.IOFaces.Add(res);
            return res;
        }
        public AsyncModuleOutputface<T> CreateAsyncOutputface<T>() where T : struct
        {
            var res = new AsyncModuleOutputface<T>();
            this.IOFaces.Add(res);
            return res;
        }

        protected void StepCycleLockPhase()
        {
            foreach (var e in IOFaces)
            {
                e.StepCycleLockPhase();
            }
            foreach (var sm in SubModules)
            {
                sm.StepCycleLockPhase();
            }
        }
        protected void StepCycleApplyPhase()
        {
            foreach (var e in IOFaces)
            {
                e.StepCycleApplyPhase();
            }
            foreach (var sm in SubModules)
            {
                sm.StepCycleApplyPhase();
            }
        }
        #endregion
        private List<SyncModuleBase> SubModules = new List<SyncModuleBase>();
        public void RegisterSubModule(SyncModuleBase module)
        {
            this.SubModules.Add(module);
        }


        public SyncModuleBase()
        {
        }

        /// <summary>
        /// このモジュール自体を1サイクル分更新し結果を出力オブジェクトに反映させます
        /// </summary>
        protected virtual void UpdateModuleCycle()
        {
            foreach (var sm in SubModules)
            {
                sm.UpdateModuleCycle();
            }
        }

        public void UpdateCycle()
        {
            this.UpdateModuleCycle();

            this.StepCycleLockPhase();
            this.StepCycleApplyPhase();
        }

        public static bool TestBit(uint value, int bitpos)
        {
            uint bit = (uint)1 << bitpos;
            return (value & bit) != 0;
        }
    }
}
