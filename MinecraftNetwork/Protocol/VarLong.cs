using System;
using System.IO;

namespace MinecraftNetwork.Protocol
{
    public struct VarLong : IEquatable<VarLong>
    {
        public long Value { get; private set; }
        public byte[] Bytes { get; private set; }

        public VarLong(long value)
        {
            Value = value;
            Bytes = GetBytes((ulong) value, 10);
        }

        /// <summary>
        /// Reads from stream. Expecting little-endian format.
        /// </summary>
        public static VarLong FromStream(in Stream stream)
        {
            return FromStream(stream, 10);
        }

        /// <summary>
        /// Reads from stream. Expecting little-endian format.
        /// </summary>
        internal static VarLong FromStream(in Stream stream, in byte maxNumOfBytes)
        {
            var i = 0;
            var result = 0;
            var bytes = new byte[maxNumOfBytes];
            do
            {
                if (i >= maxNumOfBytes)
                {
                    throw new OverflowException($"VarLong stream contains too many bytes. Expected {maxNumOfBytes}.");
                }

                bytes[i] = (byte)stream.ReadByte();
                ref byte read = ref bytes[i];

                int value;

                if (i == maxNumOfBytes - 1)
                {
                    value = read & 0b0000_1111;
                }
                else
                {
                    value = read & 0b0111_1111;
                }

                result |= value << (7 * i);
            } while ((bytes[i++] & 0b10000000) != 0);

            var resultBytes = new byte[i];
            Array.Copy(bytes, resultBytes, i);
            return new VarLong
            {
                Value = result,
                Bytes = resultBytes
            };
        }

        internal static byte[] GetBytes(ulong value, in byte maxNumOfBytes)
        {
            var buffer = new byte[maxNumOfBytes];
            var pos = 0;

            do
            {
                if (pos >= maxNumOfBytes)
                {
                    throw new OverflowException($"Value is too big and will contain too many bytes. Byte limit: {maxNumOfBytes}");
                }

                var b = (byte)(value & 0b01111111);

                value >>= 7;

                if (value != 0)
                {
                    b |= 0b10000000;
                }

                buffer[pos++] = b;

            } while (value != 0 && pos < maxNumOfBytes);

            var result = new byte[pos];
            Buffer.BlockCopy(buffer, 0, result, 0, pos);

            return result;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is long i)
                return Equals(i);

            return obj is VarLong vi && Equals(vi);
        }

        public bool Equals(VarLong other)
        {
            return Value == other.Value;
        }

        public bool Equals(long other)
        {
            return Value == other;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }

        public static bool operator ==(VarLong long1, VarLong long2)
        {
            return long1.Equals(long2);
        }

        public static bool operator !=(VarLong long1, VarLong long2)
        {
            return !(long1 == long2);
        }

        public static bool operator ==(VarLong long1, long long2)
        {
            return long1.Equals(long2);
        }

        public static bool operator !=(VarLong long1, long long2)
        {
            return !(long1 == long2);
        }

        public static bool operator ==(long long1, VarLong long2)
        {
            return long2.Equals(long1);
        }

        public static bool operator !=(long long1, VarLong long2)
        {
            return !(long1 == long2);
        }

        public static implicit operator long(VarLong varInt)
        {
            return varInt.Value;
        }

        public static implicit operator VarLong(long value)
        {
            return new VarLong(value);
        }
    }
}