using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum LengthType
{
    Byte,
    Int16,
    Int32,
}
namespace QTool.BinaryStream
{
    public class BinaryReader
    {
        public void Reset(byte[] bytes)
        {
            this.bytes = bytes;
            index = 0;
        }
        public byte[] bytes { protected set; get; }
        public int index { protected set; get; }
        public byte ReadByte()
        {
            var value = bytes[index];
            index++;
            return value;
        }
        public int ReadInt32()
        {
            var value = bytes.GetInt32(index);
            index += 4;
            return value;
        }
        public Int64 ReadInt64()
        {
            var value = bytes.GetInt64(index);
            index += 8;
            return value;
        }

        public bool ReadBoolean()
        {
            var value = bytes.GetBoolean(index);
            index += 1;
            return value;
        }

        public char ReadChar()
        {
            var value = bytes.GetChar(index);
            index += 1;
            return value;
        }

        public double ReadDouble()
        {
            var value = bytes.GetDouble(index);
            index += 8;
            return value;
        }

        public Int16 ReadInt16()
        {
            var value = bytes.GetInt16(index);
            index += 2;
            return value;
        }

        public SByte ReadSByte()
        {
            return (sbyte)ReadByte();
        }

        public float ReadSingle()
        {
            var value = bytes.GetSingle(index);
            index += 4;
            return value;
        }

        public UInt16 ReadUInt16()
        {
            var value = bytes.GetUInt16(index);
            index += 2;
            return value;
        }

        public UInt32 ReadUInt32()
        {
            var value = bytes.GetUInt32(index);
            index += 4;
            return value;
        }

        public object ReadUInt64()
        {
            var value = bytes.GetUInt64(index);
            index += 5;
            return value;
        }
        protected int ReadLength(LengthType lengthType)
        {
            switch (lengthType)
            {
                case LengthType.Byte:
                    return ReadByte();
                case LengthType.Int16:
                    return ReadInt16();
                case LengthType.Int32:
                    return ReadInt32();
                default:
                    return 0;
            }
        }
        public byte[] ReadBytes(LengthType lengthType = LengthType.Int32)
        {
            var length = ReadLength(lengthType);
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = ReadByte();
            }
            return bytes;
        }
        public string ReadString(LengthType lengthType= LengthType.Int32)
        {
            var length = ReadLength(lengthType);
            var value = bytes.GetString(index, length);
            index +=  length;
            return value;
        }
    }
    public class BinaryWriter
    {
        public List<byte> byteList { protected set; get; } = new List<byte>();
        public void Clear()
        {
            byteList.Clear();
        }
        public byte[] ToArray()
        {
            return byteList.ToArray();
        }
        public void Write(byte value)
        {
            byteList.Add(value);
        }
        public void Write(Int16 value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(Int32 value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(Int64 value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(UInt16 value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(UInt32 value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(UInt64 value)
        {
            byteList.AddRange(value.GetBytes());
        }

        public void Write(bool value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(char value)
        {
            byteList.AddRange(value.GetBytes());
        }
        public void Write(double value)
        {
            byteList.AddRange(value.GetBytes());
        }

        public void Write(sbyte value)
        {
            byteList.Add(((byte)value));
        }
        public void Write(float value)
        {
            byteList.AddRange(value.GetBytes());
        }
        protected void WriteLengh(int length, LengthType lengthType= LengthType.Int32)
        {
            switch (lengthType)
            {
                case LengthType.Byte:
                    if (length > byte.MaxValue)
                    {
                        Debug.LogError("长度[" + length + "]大于" + byte.MaxValue);
                    }
                    Write((byte)length);
                    break;
                case LengthType.Int16:
                    if (length > Int16.MaxValue)
                    {
                        Debug.LogError("长度[" + length + "]大于" + Int16.MaxValue);
                    }
                    Write((Int16)length);
                    break;
                case LengthType.Int32:
                    if (length > Int32.MaxValue)
                    {
                        Debug.LogError("长度[" + length + "]大于" + Int32.MaxValue);
                    }
                    Write((Int32)length);
                    break;
                default:
                    break;
            }
        }
        public void Write(byte[] bytes, LengthType lengthType = LengthType.Int32)
        {
            var length = bytes.Length;
            WriteLengh(length, lengthType);
            byteList.AddRange(bytes);
        }
        public void Write(string value, LengthType lengthType = LengthType.Int32)
        {
            var bytes = value.GetBytes();
            var length = bytes.Length;
            WriteLengh(length, lengthType);
            byteList.AddRange(bytes);
        }
    }
    public static class QBinaryExtends
    {
        public static byte[] GetBytes(this string value)
        {
            if (value == null)
            {
                return new byte[0];
            }
            return System.Text.Encoding.Unicode.GetBytes(value);
        }
        public static string GetString(this byte[] value,int start,int length)
        {
            if (value == null)
            {
                return "";
            }
            return System.Text.Encoding.Unicode.GetString(value,start,length);
        }
        public static string GetString(this byte[] value)
        {
            if (value == null)
            {
                return "";
            }
            return System.Text.Encoding.Unicode.GetString(value);
        }
        public static byte[] GetBytes(this Boolean value)
        {
            return BitConverter.GetBytes(value);
        }
        public static bool GetBoolean(this byte[] value, int start = 0)
        {
            return BitConverter.ToBoolean(value, start);
        }



        public static byte[] GetBytes(this char value)
        {
            return BitConverter.GetBytes(value);
        }
        public static char GetChar(this byte[] value, int start = 0)
        {
            return BitConverter.ToChar(value, start);
        }

     

        public static byte[] GetBytes(this Int16 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static Int16 GetInt16(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt16(value, start);
        }

        public static byte[] GetBytes(this UInt16 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt16 GetUInt16(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt16(value, start);
        }

        public static byte[] GetBytes(this int value)
        {
            return BitConverter.GetBytes(value);
        }
        public static int GetInt32(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt32(value, start);
        }

        public static byte[] GetBytes(this UInt32 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt32 GetUInt32(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt32(value, start);
        }

        public static byte[] GetBytes(this long value)
        {
            return BitConverter.GetBytes(value);
        }
        public static long GetInt64(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt64(value, start);
        }

        public static byte[] GetBytes(this UInt64 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt64 GetUInt64(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt64(value, start);
        }


        public static byte[] GetBytes(this float value)
        {
            return BitConverter.GetBytes(value);
        }
        public static float GetSingle(this byte[] value, int start = 0)
        {
            return BitConverter.ToSingle(value, start);
        }


        public static byte[] GetBytes(this double value)
        {
            return BitConverter.GetBytes(value);
        }
        public static Double GetDouble(this byte[] value, int start = 0)
        {
            return BitConverter.ToDouble(value, start);
        }
        public static string ComputeScale(this string array)
        {
            return array.Length.ComputeScale();
        }
        public static string ComputeScale(this IList array)
        {
            return array.Count.ComputeScale();
        }

        public static string ComputeScale(this int byteLength)
        {
            return ComputeScale((long)byteLength);
        }

        public static string ComputeScale(this long byteLength)
        {
            if (byteLength < 1024)
            {
                return byteLength + " byte ";
            }
            else if (byteLength < 1048576)
            {
                return byteLength / 1024f + " Kb ";
            }
            else if (byteLength < 1048576 * 1024)
            {
                return byteLength / 1048576f + " Mb ";
            }
            else
            {
                return byteLength / (1048576f * 1024) + " Gb ";
            }
        }
    }
}
