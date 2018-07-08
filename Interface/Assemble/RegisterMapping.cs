using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class RegisterMapping
    {
        public struct RegisterElement
        {
            public readonly string Name;
            public readonly int[] OperandNumbers;

            public RegisterElement(string name,params int[] operandNums)
            {
                this.Name = name;
                this.OperandNumbers = operandNums;
            }

            public int GetRegisterNumber(int index)
            {
                if (index >= this.OperandNumbers.Length)
                    return -1;
                return this.OperandNumbers[index];
            }

            public bool IsMatchByName(string name)
            {
                return this.Name == name;
            }
            public bool IsMatchByOperand(int num,int index)
            {
                if (index >= this.OperandNumbers.Length)
                    return false;
                return this.OperandNumbers[index] == num;
            }
        }
        public readonly RegisterElement[] Registers;

        public RegisterMapping(RegisterElement[] regs)
        {
            this.Registers = regs;
        }

        public bool SearchByName(string name,out RegisterElement res)
        {
            res = Array.Find(Registers,(e) => e.IsMatchByName(name));
            return res.Name != null;
        }
        public bool SearchByOperand(int num,int index,out RegisterElement res)
        {
            res = Array.Find(Registers,(e) => e.IsMatchByOperand(num,index));
            return res.Name != null;
        }
    }
}
