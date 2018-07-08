using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Interface.Misc
{
    public static class BinaryAccess
    {
        #region Read
        public static bool ReadBool(Stream s)
        {
            byte[] buffer = new byte[1];
            s.Read(buffer,0,1);
            return BitConverter.ToBoolean(buffer,0);
        }

        public static byte ReadByte(Stream s)
        {
            byte[] buffer = new byte[1];
            s.Read(buffer,0,1);
            return buffer[0];
        }

        public static short ReadShort(Stream s)
        {
            byte[] buffer = new byte[2];
            s.Read(buffer,0,2);
            return BitConverter.ToInt16(buffer,0);
        }

        public static ushort ReadUShort(Stream s)
        {
            byte[] buffer = new byte[2];
            s.Read(buffer,0,2);
            return BitConverter.ToUInt16(buffer,0);
        }

        public static int ReadInt(Stream s)
        {
            byte[] buffer = new byte[4];
            s.Read(buffer,0,4);
            return BitConverter.ToInt32(buffer,0);
        }

        public static uint ReadUInt(Stream s)
        {
            byte[] buffer = new byte[4];
            s.Read(buffer,0,4);
            return BitConverter.ToUInt32(buffer,0);
        }

        public static long ReadLong(Stream s)
        {
            byte[] buffer = new byte[8];
            s.Read(buffer,0,8);
            return BitConverter.ToInt64(buffer,0);
        }

        public static ulong ReadULong(Stream s)
        {
            byte[] buffer = new byte[8];
            s.Read(buffer,0,8);
            return BitConverter.ToUInt64(buffer,0);
        }

        public static float ReadFloat(Stream s)
        {
            byte[] buffer = new byte[4];
            s.Read(buffer,0,4);
            return BitConverter.ToSingle(buffer,0);
        }

        public static double ReadDouble(Stream s)
        {
            byte[] buffer = new byte[8];
            s.Read(buffer,0,8);
            return BitConverter.ToDouble(buffer,0);
        }

        public static string ReadString(Stream s)
        {
            byte[] buffer = new byte[4];
            s.Read(buffer,0,4);
            buffer = new byte[BitConverter.ToInt32(buffer,0)];
            s.Read(buffer,0,buffer.Length);
            return System.Text.Encoding.Default.GetString(buffer);
        }

        public static byte[] ReadBytes(Stream s)
        {
            byte[] buffer = new byte[4];
            s.Read(buffer,0,4);
            buffer = new byte[BitConverter.ToInt32(buffer,0)];
            s.Read(buffer,0,buffer.Length);
            return buffer;
        }
        #endregion
        #region Write
        public static void Write(Stream s,bool v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,byte v)
        {
            s.WriteByte(v);
        }

        public static void Write(Stream s,short v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,ushort v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,int v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,uint v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,long v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,ulong v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,float v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,double v)
        {
            byte[] buffer = BitConverter.GetBytes(v);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,string v)
        {
            byte[] buffer = System.Text.Encoding.Default.GetBytes(v);
            s.Write(BitConverter.GetBytes((int)buffer.Length),0,4);
            s.Write(buffer,0,buffer.Length);
        }

        public static void Write(Stream s,byte[] v)
        {
            s.Write(BitConverter.GetBytes((int)v.Length),0,4);
            s.Write(v,0,v.Length);
        }
        #endregion
    }
}
