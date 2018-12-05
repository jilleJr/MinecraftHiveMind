using MinecraftNetwork.Protocol;

namespace MinecraftNetwork.Packets
{
    public interface IPacket
    {
        VarInt PacketID { get; }
        void Write(in NotchianStream stream);
        void Read(in NotchianStream stream);
    }
}