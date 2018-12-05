using System;
using MinecraftNetwork.Protocol;

namespace MinecraftNetwork.Packets
{
    public struct PLoginStart : IPacket
    {
        public VarInt PacketID => 0x00;

        /// <summary>
        /// Player's Username
        /// </summary>
        public string PlayerUsername { get; set; }

        public void Write(in NotchianStream stream)
        {
            if (string.IsNullOrEmpty(PlayerUsername))
                throw new ArgumentNullException(nameof(PlayerUsername), "Username must be at least 1 character long.");

            if (PlayerUsername.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(PlayerUsername), "Username may be max 16 characters long.");

            stream.WriteString(PlayerUsername);
        }

        public void Read(in NotchianStream stream)
        {
            PlayerUsername = stream.ReadString();
        }
    }
}