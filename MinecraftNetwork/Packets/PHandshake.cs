using MinecraftNetwork.Protocol;

namespace MinecraftNetwork.Packets
{
    public struct PHandshake : IPacket
    {

        public VarInt PacketID => 0x00;

        /// <summary>
        /// See protocol version numbers (currently 404 in Minecraft 1.13.2)
        /// <see href="https://wiki.vg/Protocol_version_numbers"/>
        /// </summary>
        public int ProtocolVersion { get; set; }

        /// <summary>
        /// Hostname or IP, e.g. localhost or 127.0.0.1, that was used to connect. The Notchian server does not use this information.
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// Default is 25565. The Notchian server does not use this information.
        /// </summary>
        public ushort ServerPort { get; set; }

        /// <summary>
        /// 1 for status, 2 for login
        /// </summary>
        public VarInt NextState { get; set; }

        public void Write(in NotchianStream stream)
        {
            stream.WriteVarInt(ProtocolVersion);
            stream.WriteString(ServerAddress);
            stream.WriteUShort(ServerPort);
            stream.WriteVarInt(NextState);
        }

        public void Read(in NotchianStream stream)
        {
            ProtocolVersion = stream.ReadVarInt();
            ServerAddress = stream.ReadString();
            ServerPort = stream.ReadUShort();
            NextState = stream.ReadVarInt();
        }
    }
}