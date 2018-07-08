using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    /// <summary>
    /// 識別子で何らかを参照する値。参照している識別子が、ブロック・ラベル・変数等の確定値なのかSymbolなのか分からないためこうなっている
    /// </summary>
    public class ValueReference : ValueBase
    {
        public string Refername;
        public int Referindex;
        public Block UsedLocation;
        public ReferencerBase Referencer = null;

        public override ValueBaseType Type
        {
            get
            {
                return Referencer.Type;
            }
        }

        /// <summary>
        /// 参照先のSymbolを探索するクラス
        /// </summary>
        public abstract class ReferencerBase
        {
            protected readonly ValueReference Parent;
            public abstract ValueBaseType Type
            {
                get;
            }

            public ReferencerBase(ValueReference parent)
            {
                this.Parent = parent;
            }
            public abstract uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1);
            public abstract RegisterInfo GetRegister();
            public abstract ReferencerBase Clone(ValueReference parent);

            public abstract bool Equals(ReferencerBase val);
            public abstract bool Equals(ReferencerToAddress val);
            public abstract bool Equals(ReferencerToSymbol val);
        }

        /// <summary>
        /// 参照先がブロックやラベル、変数等のアドレス値である直接参照の場合のクラス
        /// </summary>
        public class ReferencerToAddress : ReferencerBase
        {
            public AddressSymbolInfo ReferenceTarget = null;
            public override ValueBaseType Type
            {
                get
                {
                    return ValueBaseType.Immediate;
                }
            }

            public ReferencerToAddress(ValueReference parent,AddressSymbolInfo referenceTarget)
                :base (parent)
            {
                this.ReferenceTarget = referenceTarget;
            }

            public override ReferencerBase Clone(ValueReference parent)
            {
                return new ReferencerToAddress(parent,this.ReferenceTarget)
                {
                    ReferenceTarget = this.ReferenceTarget
                };
            }

            public override bool Equals(ReferencerToAddress val)
            {
                return val.ReferenceTarget == this.ReferenceTarget;
            }
            public override bool Equals(ReferencerToSymbol val)
            {
                return false;
            }
            public override bool Equals(ReferencerBase val)
            {
                return val.Equals(this);
            }

            public override uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
            {
                uint res = (ReferenceTarget.MemoryNumber != 0 ? 0x80000000 : 0x00000000) | (uint)((int)ReferenceTarget.Address.From + Parent.Referindex * bytesPerWord);
                res /= (uint)addressTranslationDiv;
                return res;
            }
            public override RegisterInfo GetRegister()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 参照先がSymbolになっている間接参照の場合のクラス
        /// </summary>
        public class ReferencerToSymbol : ReferencerBase
        {
            public Symbol ReferenceTarget = null;
            public override ValueBaseType Type
            {
                get
                {
                    return ReferenceTarget.Content.Type;
                }
            }

            public ReferencerToSymbol(ValueReference parent, Symbol referenceTarget)
                : base(parent)
            {
                ReferenceTarget = referenceTarget;
            }

            public override ReferencerBase Clone(ValueReference parent)
            {
                return new ReferencerToSymbol(parent, ReferenceTarget)
                {
                    ReferenceTarget = this.ReferenceTarget
                };
            }

            public override bool Equals(ReferencerToAddress val)
            {
                return false;
            }
            public override bool Equals(ReferencerToSymbol val)
            {
                return val.ReferenceTarget == this.ReferenceTarget;
            }
            public override bool Equals(ReferencerBase val)
            {
                return val.Equals(this);
            }

            public override uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
            {
                return ReferenceTarget.Content.GetValue(bytesPerWord, addressTranslationDiv);
            }
            public override RegisterInfo GetRegister()
            {
                return ReferenceTarget.Content.GetRegister();
            }
        }

        public ValueReference(string refername,Block usedLocation)
        {
            this.Refername = refername;
            this.Referindex = 0;
            this.UsedLocation = usedLocation;
        }
        public ValueReference(string refername,int referindex,Block usedLocation)
        {
            this.Refername = refername;
            this.Referindex = referindex;
            this.UsedLocation = usedLocation;
        }

        public override uint GetValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
        {
            if (Referencer == null)
            {
                Console.WriteLine("Exception occurs for '" + Refername + "':");
                Console.WriteLine("Value-reference is not solved.");
                return 0;
            }

            return Referencer.GetValue(bytesPerWord, addressTranslationDiv);
        }
        
        public override ValueBase Clone(Block parentBlock)
        {
            return new ValueReference(this.Refername,this.Referindex,parentBlock)
            {
                AssemblePosition = this.AssemblePosition,
                Referencer = this.Referencer?.Clone(this)
            };
        }

        public override string ToString()
        {
            string res =  "&" + Refername;
            if (Referindex != 0)
            {
                res += $"[{ Referindex }]";
            }
            return res;
        }

        public override bool MatchIdentifier(MacroDefinition.ArgumentElement identifier)
        {
            if (this.Refername != identifier.Name)
                return false;

            return true;
        }

        public override bool SolveReferences(List<AssembleError> errorList)
        {
            if (UsedLocation == null)
            {
                errorList.Add(new Assemble.AssembleError()
                {
                    Title = "Refersolving",
                    Detail = "Value description refering memory isn't bind by Block location",
                    Position = this.AssemblePosition
                });
                return false;
            }

            IdentifierSearchResult searchRes;
            if (!UsedLocation.FindIdentifier(this.Refername,
                IdentifierType.ReferencingAddress | IdentifierType.ReferencingSymbol,
                out searchRes))
            {
                errorList.Add(new Assemble.AssembleError()
                {
                    Title = "Refersolving",
                    Detail = "Symbol not found in or out of the block",
                    Position = this.AssemblePosition
                });
                return false;
            }

            switch (searchRes.Type)
            {
                case IdentifierType.ReferencingAddress:
                    if (searchRes.AddressIdentifier != null)
                    {
                        this.Referencer = new ReferencerToAddress(this,searchRes.AddressIdentifier);
                    }
                    break;
                case IdentifierType.ReferencingSymbol:
                    if (searchRes.ImmediateIdentifier != null)
                    {
                        this.Referencer = new ReferencerToSymbol(this,searchRes.ImmediateIdentifier);
                    }
                    break;
            }

            if (this.Referencer == null)
            {
                errorList.Add(new Assemble.AssembleError()
                {
                    Title = "Refersolving",
                    Detail = "Reference target has been found,but that symbol has null info (maybe bug)",
                    Position = this.AssemblePosition
                });
                return false;
            }

            return true;
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
            return val.Referencer.Equals(this.Referencer);
        }
        public override bool Equals(ValueRegister val)
        {
            return false;
        }

        public override RegisterInfo GetRegister()
        {
            return Referencer.GetRegister();
        }

    }
}
