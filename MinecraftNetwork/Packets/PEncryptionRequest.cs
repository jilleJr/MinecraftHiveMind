using System;
using MinecraftNetwork.Protocol;

namespace MinecraftNetwork.Packets
{
    public struct PEncryptionRequest : IPacket
    {
        public VarInt PacketID => 0x01;

        /// <summary>
        /// Appears to be empty
        /// </summary>
        public string ServerId { get; set; }

        public byte[] PublicKey { get; set; }

        public byte[] VerifyToken { get; set; }

        public void Write(in NotchianStream stream)
        {
            if (ServerId is null)
                throw new ArgumentNullException(nameof(ServerId), "ServerID must cannot be null.");

            if (ServerId.Length > 20)
                throw new ArgumentOutOfRangeException(nameof(ServerId), "ServerID may be max 16 characters long.");

            if (PublicKey is null)
                throw new ArgumentNullException(nameof(PublicKey), "PublicKey array cannot be null.");

            if (VerifyToken is null)
                throw new ArgumentNullException(nameof(VerifyToken), "VerifyToken array cannot be null.");

            stream.WriteString(ServerId);
            stream.WriteByteArray(PublicKey);
            stream.WriteByteArray(VerifyToken);
        }

        public void Read(in NotchianStream stream)
        {
            ServerId = stream.ReadString();
            PublicKey = stream.ReadByteArray();
            VerifyToken = stream.ReadByteArray();
        }
    }
}