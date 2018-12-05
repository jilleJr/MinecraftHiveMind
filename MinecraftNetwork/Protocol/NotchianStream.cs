using System;
using System.IO;
using System.Text;
using MinecraftNetwork.Packets;

namespace MinecraftNetwork.Protocol
{
    public class NotchianStream : Stream
    {
        private readonly Stream _stream;

        public NotchianStream(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Writes packet size, id, and content using <see cref="IPacket.Write"/> and flushes this stream.
        /// </summary>
        public TPacket ReadPacket<TPacket>() where TPacket : struct, IPacket
        {
            TPacket packet;

            lock (_stream)
            {
                int length = ReadVarInt();
                VarInt packetId = ReadVarInt();

                packet = new TPacket();

                if (packetId != packet.PacketID)
                {
                    var discardedRestOfPacket = new byte[length - packetId.Bytes.Length];
                    _stream.Read(discardedRestOfPacket, 0, discardedRestOfPacket.Length);

                    throw new InvalidDataException($"Expected packet 0x{packet.PacketID.Value:x2}, got 0x{packetId.Value:x2}.");
                }

                packet.Read(this);
            }

            return packet;
        }

        /// <summary>
        /// Writes packet size, id, and content using <see cref="IPacket.Write"/> and flushes this stream.
        /// </summary>
        public void FlushPacket(IPacket packet)
        {
            using (var memory = new MemoryStream())
            using (var notchian = new NotchianStream(memory))
            {
                notchian.WriteVarInt(packet.PacketID);
                packet.Write(in notchian);

                lock (_stream)
                {
                    WriteVarInt((VarInt)memory.Length);
                    memory.WriteTo(this);
                    Flush();
                }
            }
        }

        #region Reader/writers for the MC protocol

        public VarInt ReadVarInt()
        {
            return VarInt.FromStream(in _stream);
        }

        public void WriteVarInt(in VarInt value)
        {
            Write(value.Bytes, 0, value.Bytes.Length);
        }

        public VarLong ReadVarLong()
        {
            return VarLong.FromStream(in _stream);
        }

        public void WriteVarLong(in VarLong value)
        {
            Write(value.Bytes, 0, value.Bytes.Length);
        }

        public string ReadString()
        {
            int length = ReadVarInt();
            var buffer = new byte[length];
            Read(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public void WriteString(in string value)
        {
            //if (string.IsNullOrEmpty(value))
            //    throw new ArgumentNullException(nameof(value), "String must be at least 1 character.");
            if (value is null)
                throw new ArgumentNullException(nameof(value), "String cannot be null.");

            int length = value.Length;
            if (length > 32767)
                throw new ArgumentOutOfRangeException(nameof(value), "String can be at max 32767 characters.");
            
            WriteByteArray(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Read byte array, prefixed with a <see cref="VarInt"/> to determine length.
        /// </summary>
        public byte[] ReadByteArray()
        {
            int length = ReadVarInt();
            var bytes = new byte[length];
            Read(bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Writes a byte array, prefixed with a <see cref="VarInt"/> to determine length.
        /// </summary>
        public void WriteByteArray(in byte[] value)
        {
            WriteVarInt(value.Length);
            Write(value, 0, value.Length);
        }

        #region Primitives integrals

        public int ReadInt()
        {
            var b = new byte[4];
            _stream.Read(b, 0, 4);
            return b[0] << 0x18 | b[1] << 0x10 | b[2] << 0x8 | b[3];
        }

        public void WriteInt(in int value)
        {
            _stream.Write(new[] {
                (byte)(value >> 0x18),
                (byte)(value >> 0x10),
                (byte)(value >> 0x8),
                (byte)value,
            }, 0, 4);
        }

        public long ReadLong()
        {
            int lower = ReadInt();
            int upper = ReadInt();
            return (long)(upper << 0x20) | (uint)lower;
        }

        public void WriteLong(in long value)
        {
            _stream.Write(new[]
            {
                (byte) (value >> 0x38),
                (byte) (value >> 0x30),
                (byte) (value >> 0x28),
                (byte) (value >> 0x20),
                (byte) (value >> 0x18),
                (byte) (value >> 0x10),
                (byte) (value >> 0x8),
                (byte) value,
            }, 0, 8);
        }

        public short ReadShort()
        {
            var b = new byte[2];
            _stream.Read(b, 0, 2);
            return (short)(b[0] << 0x8 | b[1]);
        }

        public void WriteShort(in short value)
        {
            _stream.Write(new[] {
                (byte)(value >> 0x8),
                (byte)value,
            }, 0, 2);
        }

        public ushort ReadUShort()
        {
            var b = new byte[2];
            _stream.Read(b, 0, 2);
            return (ushort)(b[0] << 0x8 | b[1]);
        }

        public void WriteUShort(in ushort value)
        {
            _stream.Write(new[] {
                (byte)(value >> 0x8),
                (byte)value,
            }, 0, 2);
        }

        public new byte ReadByte()
        {
            return (byte)_stream.ReadByte();
        }

        public sbyte ReadSByte()
        {
            return (sbyte)_stream.ReadByte();
        }

        public void WriteSByte(in sbyte value)
        {
            _stream.WriteByte((byte)value);
        }

        public bool ReadBool()
        {
            return (ReadByte() | 0x01) == 0x01;
        }

        public void WriteBool(in bool value)
        {
            _stream.WriteByte(value ? (byte)0x01 : (byte) 0x00);
        }

        #endregion

        #endregion

        #region Default stream implementation

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;

        public override long Position {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}