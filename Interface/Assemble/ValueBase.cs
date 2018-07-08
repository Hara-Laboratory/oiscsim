using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public abstract class ValueBase : IEquatable<ValueBase>
    {
        public abstract ValueBaseType Type
        {
            get;
        }
        public AssemblePosition AssemblePosition;

        public static ValueBase Zero
        {
            get
            {
                return new ValueInteger(0);
            }
        }

        public ValueBase()
        {
        }

        /// <summary>
        /// このシンボル値が示すデータを取得します
        /// </summary>
        /// <param name="bytesPerWord">1ワードあたりのバイト数。配列インデックスからのアドレス特定に使用。</param>
        /// <param name="addressTranslationDiv">アドレス変換のための除数。このシンボル値がアドレスである場合にこの数値で除算される。（バイトアドレスでアドレス割当をした際にワードアドレスで参照を取得したい時に利用）</param>
        public abstract uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1);
        public abstract RegisterInfo GetRegister();

        public abstract ValueBase Clone(Block parentBlock);

        public abstract bool MatchIdentifier(MacroDefinition.ArgumentElement identifier);
        public bool MatchIdentifier(MacroDefinition.ArgumentElement[] identifiers,out int index)
        {
            for (int i = 0; i < identifiers.Length; i++)
            {
                if (!MatchIdentifier(identifiers[i]))
                    continue;

                index = i;
                return true;
            }
            index = -1;
            return false;
        }

        public virtual bool SolveReferences(List<AssembleError> errorList)
        {
            return true;
        }

        public abstract bool Equals(ValueBase val);
        public abstract bool Equals(ValueInteger val);
        public abstract bool Equals(ValueChar val);
        public abstract bool Equals(ValueReference val);
        public abstract bool Equals(ValueRegister val);
    }
}
