using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Interface.Misc
{
    [Serializable]
    public class VariousTable
    {
        public enum enumVariousElement
        {
            Table,
            Bool,
            Byte,
            Short,
            UShort,
            Int,
            UInt,
            Long,
            ULong,
            Float,
            Double,
            String,
            Bytes
        }

        [Serializable]
        public class Key
        {
            public string Name;

            public enumVariousElement Type;


            public Key()
            {
                this.Name = "";
                this.Type = enumVariousElement.Bool;
            }

            public Key(string name,enumVariousElement type)
            {
                this.Name = name;
                this.Type = type;
            }

            public override bool Equals(object v)
            {
                Key other = (Key)v;

                if (this.Name != other.Name)
                    return false;

                if (this.Type != other.Type)
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                int res = Name.GetHashCode();
                res ^= Type.GetHashCode();
                return res;
            }

            public Key Clone()
            {
                return new Key(this.Name,this.Type);
            }

            #region BinarySerialize
            public void Read(Stream s)
            {
                Name = BinaryAccess.ReadString(s);
                Type = (enumVariousElement)BinaryAccess.ReadInt(s);
            }

            public void Write(Stream s)
            {
                BinaryAccess.Write(s,Name);
                BinaryAccess.Write(s,(int)Type);
            }
            #endregion
        }

        [Serializable]
        public class Element
        {
            [System.Xml.Serialization.XmlIgnore]
            public Key Key;

            #region aaa
            [System.Xml.Serialization.XmlAttribute("key")]
            public string KeyName
            {
                get
                {
                    return Key.Name;
                }
                set
                {
                    Key.Name = value;
                }
            }

            [System.Xml.Serialization.XmlAttribute("type")]
            public enumVariousElement KeyType
            {
                get
                {
                    return Key.Type;
                }
                set
                {
                    Key.Type = value;
                }
            }
            #endregion

            //[System.Xml.Serialization.XmlText]
            [System.Xml.Serialization.XmlElement("Value")]
            public object Value;

            public Element()
            {
                Key = new Key();
                Value = null;
            }

            #region BinarySerialize
            public void Read(Stream s)
            {
                Key = new Key();
                Key.Read(s);

                switch (Key.Type)
                {
                    case enumVariousElement.Bool:
                        Value = BinaryAccess.ReadBool(s);
                        break;
                    case enumVariousElement.Byte:
                        Value = BinaryAccess.ReadByte(s);
                        break;
                    case enumVariousElement.Short:
                        Value = BinaryAccess.ReadShort(s);
                        break;
                    case enumVariousElement.UShort:
                        Value = BinaryAccess.ReadUShort(s);
                        break;
                    case enumVariousElement.Int:
                        Value = BinaryAccess.ReadInt(s);
                        break;
                    case enumVariousElement.UInt:
                        Value = BinaryAccess.ReadUInt(s);
                        break;
                    case enumVariousElement.Long:
                        Value = BinaryAccess.ReadLong(s);
                        break;
                    case enumVariousElement.ULong:
                        Value = BinaryAccess.ReadULong(s);
                        break;
                    case enumVariousElement.Float:
                        Value = BinaryAccess.ReadFloat(s);
                        break;
                    case enumVariousElement.Double:
                        Value = BinaryAccess.ReadDouble(s);
                        break;
                    case enumVariousElement.String:
                        Value = BinaryAccess.ReadString(s);
                        break;
                    case enumVariousElement.Bytes:
                        Value = BinaryAccess.ReadBytes(s);
                        break;
                    case enumVariousElement.Table:
                        Value = new VariousTable();
                        ((VariousTable)Value).Read(s);
                        break;
                }
            }

            public void Write(Stream s)
            {
                Key.Write(s);

                switch (Key.Type)
                {
                    case enumVariousElement.Bool:
                        BinaryAccess.Write(s,(bool)Value);
                        break;
                    case enumVariousElement.Byte:
                        BinaryAccess.Write(s,(byte)Value);
                        break;
                    case enumVariousElement.Short:
                        BinaryAccess.Write(s,(short)Value);
                        break;
                    case enumVariousElement.UShort:
                        BinaryAccess.Write(s,(ushort)Value);
                        break;
                    case enumVariousElement.Int:
                        BinaryAccess.Write(s,(int)Value);
                        break;
                    case enumVariousElement.UInt:
                        BinaryAccess.Write(s,(uint)Value);
                        break;
                    case enumVariousElement.Long:
                        BinaryAccess.Write(s,(long)Value);
                        break;
                    case enumVariousElement.ULong:
                        BinaryAccess.Write(s,(ulong)Value);
                        break;
                    case enumVariousElement.Float:
                        BinaryAccess.Write(s,(float)Value);
                        break;
                    case enumVariousElement.Double:
                        BinaryAccess.Write(s,(double)Value);
                        break;
                    case enumVariousElement.String:
                        BinaryAccess.Write(s,(string)Value);
                        break;
                    case enumVariousElement.Bytes:
                        BinaryAccess.Write(s,(byte[])Value);
                        break;
                    case enumVariousElement.Table:
                        ((VariousTable)Value).Write(s);
                        break;
                }
            }
            #endregion
        }

        private Key Accessor;
        protected bool AccessElement(string name,enumVariousElement type,out Element e)
        {
            e = null;
            Accessor.Name = name;
            Accessor.Type = type;
            if (!TableList.ContainsKey(Accessor))
                return false;

            e = TableList[Accessor];
            return true;
        }

        protected void CreateElement(string name,enumVariousElement type,out Element e)
        {
            e = null;
            Accessor.Name = name;
            Accessor.Type = type;
            if (!TableList.ContainsKey(Accessor))
            {
                e = new Element();
                e.Key = Accessor.Clone();
                e.Value = null;

                TableList.Add(e.Key,e);
            }
            else
            {
                e = TableList[Accessor];
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        Dictionary<Key,Element> TableList;

        [System.Xml.Serialization.XmlIgnore]
        public Key[] Keys
        {
            get
            {
                List<Key> res = new List<Key>();
                foreach (var e in TableList.Keys)
                {
                    res.Add(e);
                }
                return res.ToArray();
            }
        }

        [System.Xml.Serialization.XmlArrayItem("Element",typeof(Element)),
        System.Xml.Serialization.XmlArray("Datas")]
        public Element[] TableListSerializer
        {
            get
            {
                List<Element> res = new List<Element>();
                foreach (Element e in TableList.Values)
                    res.Add(e);

                return res.ToArray();
            }
            set
            {
                TableList = new Dictionary<Key,Element>();
                foreach (Element e in value)
                    TableList.Add(e.Key,e);
            }
        }

        static System.Xml.Serialization.XmlSerializer XMLSerializer = new System.Xml.Serialization.XmlSerializer(typeof(VariousTable));

        public VariousTable()
        {
            TableList = new Dictionary<Key,Element>();
            Accessor = new Key();
        }

        public void Clear()
        {
            TableList = new Dictionary<Key,Element>();
        }

        public VariousTable Clone()
        {
            using (System.IO.MemoryStream mem = new MemoryStream())
            {
                this.Write(mem);
                mem.Seek(0,SeekOrigin.Begin);

                VariousTable vt = new VariousTable();
                vt.Read(mem);

                return vt;
            }
        }

        #region TryGet
        public bool TryGetBool(string id,out bool res)
        {
            res = false;
            Element e;
            if (!AccessElement(id,enumVariousElement.Bool,out e))
                return false;
            res = (bool)e.Value;
            return true;
        }

        public bool TryGetByte(string id,out byte res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Byte,out e))
                return false;
            res = (byte)e.Value;
            return true;
        }

        public bool TryGetShort(string id,out short res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Short,out e))
                return false;
            res = (short)e.Value;
            return true;
        }

        public bool TryGetUShort(string id,out ushort res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.UShort,out e))
                return false;
            res = (ushort)e.Value;
            return true;
        }

        public bool TryGetInt(string id,out int res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Int,out e))
                return false;
            res = (int)e.Value;
            return true;
        }

        public bool TryGetUInt(string id,out uint res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.UInt,out e))
                return false;
            res = (uint)e.Value;
            return true;
        }

        public bool TryGetLong(string id,out long res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Long,out e))
                return false;
            res = (long)e.Value;
            return true;
        }

        public bool TryGetULong(string id,out ulong res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.ULong,out e))
                return false;
            res = (ulong)e.Value;
            return true;
        }

        public bool TryGetFloat(string id,out float res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Float,out e))
                return false;
            res = (float)e.Value;
            return true;
        }

        public bool TryGetDouble(string id,out double res)
        {
            res = 0;
            Element e;
            if (!AccessElement(id,enumVariousElement.Double,out e))
                return false;
            res = (double)e.Value;
            return true;
        }

        public bool TryGetString(string id,out string res)
        {
            res = "";
            Element e;
            if (!AccessElement(id,enumVariousElement.String,out e))
                return false;
            res = (string)e.Value;
            return true;
        }

        public bool TryGetTable(string id,out VariousTable res)
        {
            res = null;
            Element e;
            if (!AccessElement(id,enumVariousElement.Table,out e))
                return false;
            res = (VariousTable)e.Value;
            return true;
        }

        public bool TryGetBytes(string id,out byte[] res)
        {
            res = new byte[0];
            Element e;
            if (!AccessElement(id,enumVariousElement.Bytes,out e))
                return false;
            res = (byte[])e.Value;
            return true;
        }

        public bool TryGetClass<T>(string id,out T res) where T : class,IVariousTableElement,new()
        {
            res = null;
            Element e;
            if (!AccessElement(id,enumVariousElement.Table,out e))
                return false;

            res = new T();
            res.Read((VariousTable)e.Value);
            return true;
        }

        public bool TryGetClassArray<T>(string id,out T[] res) where T : class,IVariousTableElement,new()
        {
            res = null;
            Element e;
            if (!AccessElement(id,enumVariousElement.Table,out e))
                return false;

            VariousTable t = (VariousTable)e.Value;
            int count;
            if (!t.TryGetInt("Count",out count))
                return false;
            res = new T[count];
            for (int i = 0; i < count; i++)
            {
                T v;
                if (!t.TryGetClass(i.ToString(),out v))
                    return false;
                res[i] = v;
            }
            return true;
        }

        public bool TryGetClassList<T>(string id,out List<T> res) where T : class,IVariousTableElement,new()
        {
            res = null;
            Element e;
            if (!AccessElement(id,enumVariousElement.Table,out e))
                return false;

            VariousTable t = (VariousTable)e.Value;
            int count;
            if (!t.TryGetInt("Count",out count))
                return false;
            res = new List<T>();
            for (int i = 0; i < count; i++)
            {
                T v;
                if (!t.TryGetClass(i.ToString(),out v))
                    return false;
                res.Add(v);
            }
            return true;
        }

        public bool TryGetListString(string id,out List<string> res)
        {
            res = null;
            Element e;
            if (!AccessElement(id,enumVariousElement.Table,out e))
                return false;

            VariousTable t = (VariousTable)e.Value;
            int count;
            if (!t.TryGetInt("Count",out count))
                return false;
            res = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string v;
                if (!t.TryGetString(i.ToString(),out v))
                    return false;
                res.Add(v);
            }
            return true;
        }
        #endregion

        #region Set
        public void Set(string id,bool val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Bool,out e);
            e.Value = val;
        }

        public void Set(string id,byte val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Byte,out e);
            e.Value = val;
        }

        public void Set(string id,short val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Short,out e);
            e.Value = val;
        }

        public void Set(string id,ushort val)
        {
            Element e;
            CreateElement(id,enumVariousElement.UShort,out e);
            e.Value = val;
        }

        public void Set(string id,int val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Int,out e);
            e.Value = val;
        }

        public void Set(string id,uint val)
        {
            Element e;
            CreateElement(id,enumVariousElement.UInt,out e);
            e.Value = val;
        }

        public void Set(string id,long val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Long,out e);
            e.Value = val;
        }

        public void Set(string id,ulong val)
        {
            Element e;
            CreateElement(id,enumVariousElement.ULong,out e);
            e.Value = val;
        }

        public void Set(string id,float val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Float,out e);
            e.Value = val;
        }

        public void Set(string id,double val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Double,out e);
            e.Value = val;
        }

        public void Set(string id,string val)
        {
            Element e;
            CreateElement(id,enumVariousElement.String,out e);
            e.Value = val;
        }

        public void Set(string id,enumVariousElement type,object val)
        {
            Element e;
            CreateElement(id,type,out e);
            e.Value = val;
        }

        public void Set(string id,VariousTable val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Table,out e);
            e.Value = val;
        }

        public void Set(string id,byte[] val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Bytes,out e);
            e.Value = val;
        }

        public void Set<T>(string id,T val) where T : class,IVariousTableElement,new()
        {
            Element e;
            CreateElement(id,enumVariousElement.Table,out e);
            e.Value = new VariousTable();
            val.Write((VariousTable)e.Value);
        }

        public void SetList(string id,IEnumerable<string> val)
        {
            Element e;
            CreateElement(id,enumVariousElement.Table,out e);
            VariousTable subTable = new VariousTable();
            e.Value = subTable;

            int c = 0;
            foreach (var el in val)
            {
                subTable.Set(c.ToString(),el);
                c++;
            }
            subTable.Set("Count",c);
        }
        public void SetList<T>(string id,IEnumerable<T> val) where T : class,IVariousTableElement,new()
        {
            Element e;
            CreateElement(id,enumVariousElement.Table,out e);
            VariousTable subTable = new VariousTable();
            e.Value = subTable;

            int c = 0;
            foreach (var el in val)
            {
                subTable.Set(c.ToString(),el);
                c++;
            }
            subTable.Set("Count",c);
        }
        #endregion

        #region BinarySerialize
        public void Read(Stream s)
        {
            int count;
            Element e;

            this.Clear();

            count = BinaryAccess.ReadInt(s);

            for (int i = 0; i < count; i++)
            {
                e = new Element();
                e.Read(s);

                TableList.Add(e.Key,e);
            }
        }

        public void Write(Stream s)
        {
            BinaryAccess.Write(s,(int)TableList.Count);

            foreach (Element e in TableList.Values)
            {
                e.Write(s);
            }
        }
        #endregion

        public string Serialize()
        {
            System.IO.MemoryStream mem = new System.IO.MemoryStream();
            XMLSerializer.Serialize(mem,this);

            return System.Text.Encoding.UTF8.GetString(mem.ToArray());
        }
        public void Serialize(Stream s)
        {
            XMLSerializer.Serialize(s,this);
        }
        public static VariousTable DeSerialize(string xml)
        {
            using (System.IO.StringReader mem = new System.IO.StringReader(xml))
            {
                return (VariousTable)XMLSerializer.Deserialize(mem);
            }
        }
        public static VariousTable DeSerializeFromPath(string path)
        {
            using (System.IO.Stream s = System.IO.File.OpenRead(path))
            {
                return (VariousTable)XMLSerializer.Deserialize(s);
            }
        }
        public static VariousTable DeSerialize(Stream s)
        {
            return (VariousTable)XMLSerializer.Deserialize(s);
        }
    }
}
