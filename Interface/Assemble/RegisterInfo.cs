using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public struct RegisterInfo : IEquatable<RegisterInfo>
    {
        public bool IsSpecifiedByName;
        public string Name;
        public int No;

        public RegisterInfo(string name)
        {
            IsSpecifiedByName = true;
            Name = name;
            No = 0;
        }

        public RegisterInfo(int no)
        {
            IsSpecifiedByName = false;
            No = no;
            Name = "";
        }

        public bool Equals(RegisterInfo other)
        {
            if (IsSpecifiedByName != other.IsSpecifiedByName)
                return false;

            if (IsSpecifiedByName)
            {
                return Name == other.Name;
            }
            else
            {
                return No == other.No;
            }
        }

        public override string ToString()
        {
            return "$" + (IsSpecifiedByName ? Name : No.ToString());
        }
    }
}
