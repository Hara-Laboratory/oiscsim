using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Execute
{
    /// <summary>
    /// 他の入力によって出力値を決定するモジュールのための出力窓口クラス
    /// </summary>
    public class AsyncModuleOutputface<ValueType> : ModuleOutputfaceBase<ValueType> where ValueType : struct
    {
        Func<ValueType> ValueGetFunc;
        public override ValueType Value
        {
            get { return ValueGetFunc(); }
            protected set { throw new NotImplementedException(); }
        }

        public AsyncModuleOutputface()
        {
        }

        public void SetFunc(Func<ValueType> func)
        {
            this.ValueGetFunc = func;
        }
    }
}
