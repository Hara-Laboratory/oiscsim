using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble.SubRisc
{
    class InstructionAssembler
    {
        const int BytesPerWord = 4;
        abstract class InstructionTypeBase
        {
            public abstract bool IsMatchMnemonic(string mnemonic);
            protected virtual bool IsMatchMnemonicByPatterns(string mnemonic, string[] patterns)
            {
                for (int i = 0; i < patterns.Length; i++)
                {
                    if (patterns[i].ToUpper() == mnemonic.ToUpper())
                        return true;
                }
                return false;
            }

            protected virtual bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                return true;
            }
            protected static bool CheckImmediateOperand(Instruction.OperandElement operand, int rangeMin, int rangeMax)
            {
                if (operand.Immediate.Type != ValueBaseType.Immediate)
                    return false;

                int imm = (int)operand.Immediate.GetValue(BytesPerWord);
                if (imm < rangeMin || imm > rangeMax)
                    return false;

                return true;
            }
            protected static bool ConvertRegisterName(Instruction.OperandElement operand, int operandIndex, List<AssembleError> errorList)
            {
                if (operand.Immediate.Type != ValueBaseType.Register)
                    return true;
                RegisterInfo reginfo = operand.Immediate.GetRegister();
                if (!reginfo.IsSpecifiedByName)
                {
                    operand.RegisterNo = reginfo.No; //Copy
                    return true;
                }

                RegisterMapping.RegisterElement e;
                if (!SubRiscAssembler.RegisterMapping.SearchByName(reginfo.Name, out e))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = $"Invalid register name '{e.Name}'",
                        Position = operand.AssemblePosition
                    });
                    return false;
                }

                if (e.GetRegisterNumber(operandIndex) < 0)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = $"Register ${e.Name} cannot be used for {operandIndex+1}th operand",
                        Position = operand.AssemblePosition
                    });
                    return false;
                }

                operand.RegisterNo = e.GetRegisterNumber(operandIndex);
                return true;
            }
            protected static bool CheckRegisterOperand(Instruction.OperandElement operand, int rangeMin, int rangeMax)
            {
                if (operand.Immediate.Type != ValueBaseType.Register)
                    return false;

                if (operand.RegisterNo < rangeMin || operand.RegisterNo > rangeMax)
                    return false;

                return true;
            }
            public abstract bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList);
            public abstract int EstimateLength(Instruction instr, out bool needToAlign);
            protected virtual bool AssembleJumpBlock(Instruction instr, ref ushort buf, List<AssembleError> errorList)
            {
                //[15]: by $ra
                //[14]: cond neg flag
                //[13]: cond lsb flag
                //[12]: cond carry flag
                //[11-0]: relative target address
                Instruction.JumpAttribute jumpSpecifier = instr.JumpAttributeInfo;
                buf = 0;

                { //Set condition flag
                    if (jumpSpecifier.Nimonic.StartsWith("jneg"))
                        buf |= 0x4000;
                    else if (jumpSpecifier.Nimonic.StartsWith("jlsb"))
                        buf |= 0x2000;
                    else if (jumpSpecifier.Nimonic.StartsWith("jcarry"))
                        buf |= 0x1000;
                    else
                    {
                        errorList.Add(new Interface.Assemble.AssembleError()
                        {
                            Title = "Assmeble",
                            Detail = $"Unknown jump condition specifier {jumpSpecifier.Nimonic}",
                            Position = jumpSpecifier.AssemblePosition
                        });
                        return false;
                    }
                }

                if (jumpSpecifier.Immediate.Type == ValueBaseType.Register)
                { //Register mode
                    //Set ra flag
                    buf |= 0x8000;
                }
                else
                { //Relative address mode
                    //Check border
                    //We must be careful that address is specified by byte address
                    int relAddr = (int)jumpSpecifier.Immediate.GetValue(BytesPerWord) / 2 - (int)instr.PlacedInfo.Address.From / 2;
                    relAddr -= 1;

                    ushort maskedAddr = (ushort)(relAddr & 0x0FFF);
                    if ((short)(maskedAddr | (((maskedAddr >> 11) % 2 != 0) ? 0xF000 : 0)) != relAddr)
                    {
                        errorList.Add(new AssembleError()
                        {
                            Title = "Assmble",
                            Detail = $"Cannot convert specified address { relAddr } to relative 12 bits address",
                            Position = jumpSpecifier.AssemblePosition
                        });
                        return false;
                    }

                    //Write address
                    buf |= maskedAddr;
                }
                return true;
            }
        }
        class SubtractInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "sub", "sng4" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.Operands.Length != 3)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Sub Instruction must be along the following style: sub $(in-a) $(in-b) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                if (!ConvertRegisterName(instr.Operands[0], 0, errorList) ||
                    !ConvertRegisterName(instr.Operands[1], 1, errorList) ||
                    !ConvertRegisterName(instr.Operands[2], 2, errorList))
                    return false;

                if (!CheckRegisterOperand(instr.Operands[0], 0, 15) ||
                    !CheckRegisterOperand(instr.Operands[1], 0, 31) ||
                    !CheckRegisterOperand(instr.Operands[2], 0, 15))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Sub Instruction must be along the following style: sub $(in-a) $(in-b) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                bool withJump = instr.JumpAttributeInfo != null;
                needToAlign = false;
                return withJump ? 2 : 1;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = false;
                bool withJump = instr.JumpAttributeInfo != null;
                
                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;

                //Convert register name
                ConvertRegisterName(instr.Operands[0], 0, errorList);
                ConvertRegisterName(instr.Operands[1], 1, errorList);
                ConvertRegisterName(instr.Operands[2], 2, errorList);

                //Process instruction
                {
                    buffer[0] = 0;
                    buffer[0] |= 0 << 14; //Opcode
                    buffer[0] |= (ushort)((instr.Operands[0].RegisterNo) << 9); //Register-A
                    buffer[0] |= (ushort)((instr.Operands[1].RegisterNo) << 4); //Register-B
                    buffer[0] |= (ushort)((instr.Operands[2].RegisterNo) << 0); //Register-D
                    if (withJump)
                        buffer[0] |= 1 << 13; //Jump flag
                }
                length = 1;

                //Process jump block
                if (withJump)
                {
                    if (!base.AssembleJumpBlock(instr, ref buffer[length], errorList))
                        return false;

                    length += 1;
                }
                return true;
            }
        }
        class XandrshiftInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "xan", "sng4x" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.Operands.Length != 3)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Xan Instruction must be along the following style: sub $(in-a) $(in-b) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                if (!ConvertRegisterName(instr.Operands[0], 0, errorList) ||
                    !ConvertRegisterName(instr.Operands[1], 1, errorList) ||
                    !ConvertRegisterName(instr.Operands[2], 2, errorList))
                    return false;

                if (!CheckRegisterOperand(instr.Operands[0], 0, 15) ||
                    !CheckRegisterOperand(instr.Operands[1], 0, 31) ||
                    !CheckRegisterOperand(instr.Operands[2], 0, 15))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Xan Instruction must be along the following style: sub $(in-a) $(in-b) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                bool withJump = instr.JumpAttributeInfo != null;
                needToAlign = false;
                return withJump ? 2 : 1;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = false;
                bool withJump = instr.JumpAttributeInfo != null;

                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;

                //Convert register name
                ConvertRegisterName(instr.Operands[0], 0, errorList);
                ConvertRegisterName(instr.Operands[1], 1, errorList);
                ConvertRegisterName(instr.Operands[2], 2, errorList);

                //Process instruction
                {
                    buffer[0] = 0;
                    buffer[0] |= 1 << 14; //Opcode
                    buffer[0] |= (ushort)((instr.Operands[0].RegisterNo) << 9); //Register-A
                    buffer[0] |= (ushort)((instr.Operands[1].RegisterNo) << 4); //Register-B
                    buffer[0] |= (ushort)((instr.Operands[2].RegisterNo) << 0); //Register-D
                    if (withJump)
                        buffer[0] |= 1 << 13; //Jump flag
                }
                length = 1;

                //Process jump block
                if (withJump)
                {
                    if (!base.AssembleJumpBlock(instr, ref buffer[length], errorList))
                        return false;

                    length += 1;
                }
                return true;
            }
        }
        class MemoryreadInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "mr", "ml", "lw" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.JumpAttributeInfo != null)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Cannot attach jump attribute to mr instruction",
                        Position = instr.JumpAttributeInfo.AssemblePosition
                    });
                    return false;
                }

                const int offsetAddrMax = 0x8; //4bit
                if (instr.Operands.Length != 3 ||
                    !CheckImmediateOperand(instr.Operands[0], -offsetAddrMax, offsetAddrMax - 1)
                    )
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Mr Instruction must be along the following style: sub $(4bit-offset) $(address) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                if (!ConvertRegisterName(instr.Operands[1], 1, errorList) ||
                    !ConvertRegisterName(instr.Operands[2], 2, errorList))
                    return false;

                if (!CheckRegisterOperand(instr.Operands[1], 0, 31) ||
                    !CheckRegisterOperand(instr.Operands[2], 0, 15))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Mr Instruction must be along the following style: mr $(4bit-offset) $(address) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }
                
                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                needToAlign = false;
                return 1;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = false;

                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;

                //Convert register name
                ConvertRegisterName(instr.Operands[1], 1, errorList);
                ConvertRegisterName(instr.Operands[2], 2, errorList);

                int regNumAt1_pc;
                {
                    RegisterMapping.RegisterElement r;
                    SubRiscAssembler.RegisterMapping.SearchByName("PC", out r);
                    regNumAt1_pc = r.OperandNumbers[1];
                }
                uint imm = instr.Operands[0].Immediate.GetValue(BytesPerWord);
                if (instr.Operands[1].RegisterNo == regNumAt1_pc && (int)imm < 0)
                { //PC Relative & Refering afterside
                    imm += 1; //ComputeStageからはPCは1halfword分進んで見える
                    if (instr.Operands[0].PlacedInfo.Address.From / 2 % 2 ==0)
                    {
                        imm -= 1;
                    } 
                }

                //Process instruction
                {
                    buffer[0] = 0;
                    buffer[0] |= 2 << 14; //Opcode
                    buffer[0] |= (ushort)((imm & 0xF) << 9); //Offset immediate
                    buffer[0] |= (ushort)((instr.Operands[1].RegisterNo) << 4); //Register-B (containing address)
                    buffer[0] |= (ushort)((instr.Operands[2].RegisterNo) << 0); //Register-D (to be written)
                    buffer[0] |= 0 << 13; //Read flag
                }
                length = 1;
                return true;
            }
        }
        class MemorywriteInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "mw", "ms", "sw" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.JumpAttributeInfo != null)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Cannot attach jump attribute to mr instruction",
                        Position = instr.JumpAttributeInfo.AssemblePosition
                    });
                    return false;
                }

                const int offsetAddrMax = 0x8; //4bit
                if (instr.Operands.Length != 3 ||
                    !CheckImmediateOperand(instr.Operands[0], -offsetAddrMax, offsetAddrMax - 1)
                    )
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Mw Instruction must be along the following style: mw $(4bit-offset) $(address) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                if (!ConvertRegisterName(instr.Operands[1], 1, errorList) ||
                    !ConvertRegisterName(instr.Operands[2], 2, errorList))
                    return false;

                if (!CheckRegisterOperand(instr.Operands[1], 0, 31) ||
                    !CheckRegisterOperand(instr.Operands[2], 0, 15))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Mw Instruction must be along the following style: mw $(4bit-offset) $(address) $(out-d);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                needToAlign = false;
                return 1;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = false;

                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;
                
                //Convert register name
                ConvertRegisterName(instr.Operands[1], 1, errorList);
                ConvertRegisterName(instr.Operands[2], 2, errorList);

                //Process instruction
                {
                    buffer[0] = 0;
                    buffer[0] |= 2 << 14; //Opcode
                    buffer[0] |= (ushort)((instr.Operands[0].Immediate.GetValue(BytesPerWord) & 0xF) << 9); //Offset immediate
                    buffer[0] |= (ushort)((instr.Operands[1].RegisterNo) << 4); //Register-B (containing address)
                    buffer[0] |= (ushort)((instr.Operands[2].RegisterNo) << 0); //Register-D (to be written)
                    buffer[0] |= 1 << 13; //Write flag
                }
                length = 1;
                return true;
            }
        }
        class ShiftInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "shl", "shr" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.JumpAttributeInfo != null)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Cannot attach jump attribute to shr instruction",
                        Position = instr.JumpAttributeInfo.AssemblePosition
                    });
                    return false;
                }

                //Check operand count
                if (instr.Operands.Length != 3)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Shr Instruction must be along the following style: shr $(src) $(dest) (amount);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                //Check source and destination operands
                if (!ConvertRegisterName(instr.Operands[0], 2, errorList) ||
                    !ConvertRegisterName(instr.Operands[1], 2, errorList))
                    return false;
                if (!CheckRegisterOperand(instr.Operands[0], 0, 31) ||
                    (!CheckRegisterOperand(instr.Operands[1], 0, 15) && !CheckImmediateOperand(instr.Operands[1], 0, 15)))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Register(s) which cannot be used for Shr instruction is specified at $(src) or $(dest)",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }
                
                //Check arbitary amount or instruction specified
                if (instr.Operands[2].Immediate.Type == ValueBaseType.Immediate)
                { //Instruction specified amount
                    const int amountMax = 14;
                    int amount = (int)instr.Operands[2].Immediate.GetValue(BytesPerWord);
                    if (amount % 2 != 0 || amount < 1 || amount > amountMax)
                    {
                        errorList.Add(new Interface.Assemble.AssembleError()
                        {
                            Title = "Assemble",
                            Detail = "Shr Instruction allows even number in the range 2..14 for the operand (amount)",
                            Position = instr.AssemblePosition
                        });
                        return false;
                    }
                }
                else
                { //Arbitary amount
                    if (!ConvertRegisterName(instr.Operands[2], 1, errorList))
                        return false;
                    if (!CheckRegisterOperand(instr.Operands[2], 2, 15))
                    {
                        errorList.Add(new Interface.Assemble.AssembleError()
                        {
                            Title = "Assemble",
                            Detail = "Register(s) which cannot be used for Shr instruction is specified at $(amount)",
                            Position = instr.AssemblePosition
                        });
                        return false;
                    }
                }

                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                needToAlign = false;
                return 1;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = false;

                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;
                bool isArbitaryAmount = instr.Operands[1].Immediate.Type != ValueBaseType.Immediate;
                bool isRightShifting = instr.Nimonic.EndsWith("r");

                //Convert register name
                ConvertRegisterName(instr.Operands[0], 2, errorList);
                ConvertRegisterName(instr.Operands[1], 2, errorList);
                if (isArbitaryAmount)
                    ConvertRegisterName(instr.Operands[2], 2, errorList);
                
                //Process instruction
                {
                    buffer[0] = 0;
                    buffer[0] |= 3 << 14; //Opcode
                    buffer[0] |= (ushort)((instr.Operands[0].RegisterNo) << 9); //Register-Src
                    if (isArbitaryAmount)
                        buffer[0] |= (ushort)((instr.Operands[1].RegisterNo & 0xF) << 4); //0_(Register number containing amount)
                    else
                        buffer[0] |= (ushort)((0x10 | ((instr.Operands[1].Immediate.GetValue(BytesPerWord) / 2) & 0xF)) << 4); //1_(4bit immediate amount)
                    buffer[0] |= (ushort)((instr.Operands[2].RegisterNo) << 0); //Register-D (to be written)
                    buffer[0] |= (ushort)((isRightShifting ? 1 : 0) << 13); //Direction flag
                }
                length = 1;
                return true;
            }
        }
        class ImmediateSpaceInstruction : InstructionTypeBase
        {
            static readonly string[] MnemonicPatterns = new string[] { "imm" };
            public override bool IsMatchMnemonic(string mnemonic)
            {
                return IsMatchMnemonicByPatterns(mnemonic, MnemonicPatterns);
            }

            protected override bool CheckValidness(Instruction instr, List<AssembleError> errorList)
            {
                if (instr.JumpAttributeInfo != null)
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Cannot attach jump attribute to immediate space instruction",
                        Position = instr.JumpAttributeInfo.AssemblePosition
                    });
                    return false;
                }
                
                if (instr.Operands.Length != 1 ||
                    !CheckImmediateOperand(instr.Operands[0], int.MinValue, int.MaxValue))
                {
                    errorList.Add(new Interface.Assemble.AssembleError()
                    {
                        Title = "Assemble",
                        Detail = "Immediate-Space Instruction must be along the following style: imm $(32bit-imm);",
                        Position = instr.AssemblePosition
                    });
                    return false;
                }

                return true;
            }
            public override int EstimateLength(Instruction instr, out bool needToAlign)
            {
                needToAlign = true;
                return 2;
            }
            public override bool Assemble(Instruction instr, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
            {
                length = 0;
                needToAlign = true;

                //Check description
                if (!CheckValidness(instr, errorList))
                    return false;

                //Process instruction
                {
                    uint imm = instr.Operands[0].Immediate.GetValue(BytesPerWord);
                    buffer[0] = (ushort)((imm >> 16) & 0xFFFF);
                    buffer[1] = (ushort)(imm & 0xFFFF);
                }
                length = 2;
                needToAlign = true;
                return true;
            }
        }

        public const int MaxLengthPerInstruction = 2;
        static ushort[] dumpBuffer = new ushort[MaxLengthPerInstruction];
        static readonly InstructionTypeBase[] AvailableTypes;
        static InstructionAssembler()
        {
            List<InstructionTypeBase> availableTypeList = new List<InstructionTypeBase>();
            foreach (var subType in typeof(InstructionAssembler).GetNestedTypes(System.Reflection.BindingFlags.NonPublic))
            {
                if (!subType.IsClass || subType.BaseType != typeof(InstructionTypeBase))
                    continue;

                availableTypeList.Add((InstructionTypeBase)Activator.CreateInstance(subType, null));
            }
            AvailableTypes = availableTypeList.ToArray();
        }
        
        public static bool SearchAssemblerType(Instruction instr, out int index, List<AssembleError> errorList)
        {
            index = -1;
            for (int i = 0; i < AvailableTypes.Length; i++)
            {
                if (!AvailableTypes[i].IsMatchMnemonic(instr.Nimonic))
                    continue;

                index = i;
                return true;
            }

            errorList.Add(new Assemble.AssembleError()
            {
                Title = "Assemble",
                Detail = $"Unknown mnemonic { instr.Nimonic }",
                Position = instr.AssemblePosition
            });
            return false;
        }

        public static bool EstimateInstructionLength(Instruction instr, int typeIndex, out int length, out bool needToAlign, List<AssembleError> errorList)
        {
            length = AvailableTypes[typeIndex].EstimateLength(instr, out needToAlign);
            return true;
        }

        public static bool AssembleInstruction(Instruction instr, int typeIndex, ushort[] buffer, out int length, out bool needToAlign, List<AssembleError> errorList)
        {
            return AvailableTypes[typeIndex].Assemble(instr, buffer, out length, out needToAlign, errorList);
        }
    }
}
