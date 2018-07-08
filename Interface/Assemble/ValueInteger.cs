using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class ValueInteger : ValueBase
    {
        public int Value;
        public override ValueBaseType Type
        {
            get
            {
                return ValueBaseType.Immediate;
            }
        }

        public ValueInteger(int value)
        {
            this.Value = value;
        }
        
        public override uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
        {
            return (uint)Value;
        }

        public override ValueBase Clone(Block parentBlock)
        {
            return new ValueInteger(this.Value)
            {
                AssemblePosition = this.AssemblePosition
            };
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool MatchIdentifier(MacroDefinition.ArgumentElement identifier)
        {
            return false;
        }
        
        public override bool Equals(ValueBase val)
        {
            return val.Equals(this);
        }
        public override bool Equals(ValueInteger val)
        {
            return val.Value == this.Value;
        }
        public override bool Equals(ValueChar val)
        {
            return (uint)val.Value == this.Value;
        }
        public override bool Equals(ValueReference val)
        {
            return false;
        }

        public override RegisterInfo GetRegister()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(ValueRegister val)
        {
            throw new NotImplementedException();
        }
    }
}
