using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Interface.Execute.CommonModule;

namespace Interface.Execute.Subneg4X
{
    public class Subneg4XCircuitGroup : SyncModuleBase
    {
        public RAM Memory; //Slot.0
        public Alu Alu;
        public SyncRAMInterfaceRW1R1 SyncMemory;
        public AsyncModuleOutputface<SyncRAMInterfaceRW1R1.ReadCommand> SyncMemoryRead1;
        public AsyncModuleOutputface<SyncRAMInterfaceRW1R1.ReadCommand> SyncMemoryRead2;
        public AsyncModuleOutputface<SyncRAMInterfaceRW1R1.WriteCommand> SyncMemoryWrite;
        public SyncModuleOutputface<uint> ProgramCounter;
        public AsyncModuleOutputface<uint> ProgramCounterInput;
        public SyncModuleOutputface<uint> MemOpA;
        public AsyncModuleOutputface<uint> MemOpAInput;
        public SyncModuleOutputface<uint> MemOpB;
        public AsyncModuleOutputface<uint> MemOpBInput;
        public SyncModuleOutputface<int> State;
        public AsyncModuleOutputface<int> StateInput;


        public Subneg4XCircuitGroup(RAM ram,uint startupAddr)
        {
            Memory = ram;

            SyncMemory = new SyncRAMInterfaceRW1R1(this.Memory);
            RegisterSubModule(SyncMemory);

            Alu = new Subneg4X.Alu();
            RegisterSubModule(Alu);

            {
                SyncMemoryRead1 = CreateAsyncOutputface<SyncRAMInterfaceRW1R1.ReadCommand>();
                SyncMemoryRead2 = CreateAsyncOutputface<SyncRAMInterfaceRW1R1.ReadCommand>();
                SyncMemoryWrite = CreateAsyncOutputface<SyncRAMInterfaceRW1R1.WriteCommand>();
                MemOpA = CreateSyncOutputface<uint>();
                MemOpB = CreateSyncOutputface<uint>();
                MemOpAInput = CreateAsyncOutputface<uint>();
                MemOpBInput = CreateAsyncOutputface<uint>();
                State = CreateSyncOutputface<int>(-1);
                StateInput = CreateAsyncOutputface<int>();
                ProgramCounter = CreateSyncOutputface<uint>(startupAddr);
                ProgramCounterInput = CreateAsyncOutputface<uint>();

                SyncMemoryRead1.SetFunc(() =>
                {
                    switch (State.Value)
                    {
                        case -1:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.ProgramCounter.Value + 0,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 0:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.SyncMemory.ReadValue1_OFace.Value,
                                AccessType = EnumMemorymAccessType.Data
                            };
                        case 1:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.ProgramCounter.Value + 2,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 2:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = false //Writing to operand C
                            };
                        case 3:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = (Alu.BranchCond_OFace.Value) ? (SyncMemory.ReadValue2_OFace.Value & 0x7FFFFFFF)
                                                                       : ProgramCounter.Value + 4,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 4:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = false
                            };
                    }

                    throw new Exception();
                });
                SyncMemoryRead2.SetFunc(() =>
                {
                    switch (State.Value)
                    {
                        case -1:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.ProgramCounter.Value + 1,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 0:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.SyncMemory.ReadValue2_OFace.Value,
                                AccessType = EnumMemorymAccessType.Data
                            };
                        case 1:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = this.ProgramCounter.Value + 3,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 2:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = false,
                                Address = this.ProgramCounter.Value + 3
                            };
                        case 3:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = true,
                                Address = (Alu.BranchCond_OFace.Value) ? ((SyncMemory.ReadValue2_OFace.Value & 0x7FFFFFFF) + 1)
                                                                       : ProgramCounter.Value + 4 + 1,
                                AccessType = EnumMemorymAccessType.Instruction
                            };
                        case 4:
                            return new SyncRAMInterfaceRW1R1.ReadCommand()
                            {
                                Enabled = false
                            };
                    }

                    throw new Exception();
                });
                SyncMemoryWrite.SetFunc(() =>
                {
                    switch (State.Value)
                    {
                        case 2:
                            return new SyncRAMInterfaceRW1R1.WriteCommand()
                            {
                                Enabled = true,
                                Address = this.SyncMemory.ReadValue1_OFace.Value,
                                Value = Alu.AluResult_OFace.Value
                            };
                    }

                    return new SyncRAMInterfaceRW1R1.WriteCommand()
                    {
                        Enabled = false
                    };
                });
                SyncMemory.ReadCmd1_IFace.BindSource(this.SyncMemoryRead1);
                SyncMemory.ReadCmd2_IFace.BindSource(this.SyncMemoryRead2);
                SyncMemory.WriteCmd_IFace.BindSource(this.SyncMemoryWrite);

                MemOpAInput.SetFunc(() =>
                {
                    return (State.Value == 1) ? SyncMemory.ReadValue1_OFace.Value
                                              : MemOpA.Value;
                });
                MemOpBInput.SetFunc(() =>
                {
                    return (State.Value == 1) ? SyncMemory.ReadValue2_OFace.Value
                                              : MemOpB.Value;
                });
                MemOpA.AutoAssign(MemOpAInput);
                MemOpB.AutoAssign(MemOpBInput);

                Alu.OperandA_IFace.BindSource(MemOpA);
                Alu.OperandB_IFace.BindSource(MemOpB);
                Alu.OperandD_IFace.BindSource(SyncMemory.ReadValue2_OFace);

                StateInput.SetFunc(() =>
                {
                    switch (State.Value)
                    {
                        case -1:
                            return 0;
                        case 0:
                            return 1;
                        case 1:
                            return 2;
                        case 2:
                            return 3;
                        case 3:
                            return 0;
                        case 4:
                            return 4;
                    }
                    return -1;
                });
                State.AutoAssign(StateInput);

                ProgramCounterInput.SetFunc(() =>
                {
                    if (State.Value == 3)
                    {
                        if (Alu.BranchCond_OFace.Value)
                        {
                            return SyncMemory.ReadValue2_OFace.Value & 0x7FFFFFFF;
                        }
                        else
                        {
                            return ProgramCounter.Value + 4;
                        }
                    }
                    return ProgramCounter.Value;
                });
                ProgramCounter.AutoAssign(ProgramCounterInput);
            }
        }

        protected override void UpdateModuleCycle()
        {
            base.UpdateModuleCycle();
        }
    }
}
