using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class ValueRegister : ValueBase
    {
        public RegisterInfo RegisterInfo;
        public override ValueBaseType Type
        {
            get
            {
                return ValueBaseType.Register;
            }
        }

        public ValueRegister(RegisterInfo reginfo)
        {
            this.RegisterInfo = reginfo;
        }

        public override uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
        {
            throw new NotImplementedException();
        }
        
        public override RegisterInfo GetRegister()
        {
            return RegisterInfo;
        }

        public override ValueBase Clone(Block parentBlock)
        {
            return new ValueRegister(RegisterInfo);
        }

        public override string ToString()
        {
            return RegisterInfo.ToString();
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
            return false;
        }
        public override bool Equals(ValueChar val)
        {
            return false;
        }
        public override bool Equals(ValueReference val)
        {
            return false;
        }
        public override bool Equals(ValueRegister val)
        {
            return RegisterInfo.Equals(val.RegisterInfo);
        }
    }
}
