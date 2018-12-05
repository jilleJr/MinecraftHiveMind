using System;
using System.IO;

namespace MinecraftNetwork.Protocol
{
    public struct VarInt : IEquatable<VarInt>
    {
        public int Value { get; private set; }
        public byte[] Bytes { get; private set; }

        public VarInt(int value)
        {
            Value = value;
            Bytes = VarLong.GetBytes((uint)value, 5);
        }

        /// <summary>
        /// Reads from stream. Expecting little-endian format.
        /// </summary>
        public static VarInt FromStream(in Stream stream)
        {
            VarLong fromStream = VarLong.FromStream(in stream, 5);
            return new VarInt {
                Value = (int) fromStream.Value,
                Bytes = fromStream.Bytes
            };
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is int i)
                return Equals(i);

            return obj is VarInt vi && Equals(vi);
        }

        public bool Equals(VarInt other)
        {
            return Value == other.Value;
        }

        public bool Equals(int other)
        {
            return Value == other;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public static bool operator ==(VarInt int1, VarInt int2)
        {
            return int1.Equals(int2);
        }

        public static bool operator !=(VarInt int1, VarInt int2)
        {
            return !(int1 == int2);
        }

        public static bool operator ==(VarInt int1, int int2)
        {
            return int1.Equals(int2);
        }

        public static bool operator !=(VarInt int1, int int2)
        {
            return !(int1 == int2);
        }

        public static bool operator ==(int int1, VarInt int2)
        {
            return int2.Equals(int1);
        }

        public static bool operator !=(int int1, VarInt int2)
        {
            return !(int1 == int2);
        }

        public static implicit operator int(VarInt varInt)
        {
            return varInt.Value;
        }

        public static implicit operator VarInt(int value)
        {
            return new VarInt(value);
        }
    }
}