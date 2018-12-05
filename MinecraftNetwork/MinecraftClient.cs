using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using MinecraftNetwork.Packets;
using MinecraftNetwork.Protocol;

namespace MinecraftNetwork
{
    public class MinecraftClient : IDisposable
    {
        public string Hostname { get; }
        public ushort Port { get; }

        public bool Connected => _client.Connected;

        private readonly TcpClient _client;
        private readonly NetworkStream _network;
        private readonly NotchianStream _notchian;

        public MinecraftClient(string hostname, ushort port)
        {
            Hostname = hostname;
            Port = port;

            Console.WriteLine("Connecting to client on {0}:{1}...", hostname, port);

            _client = new TcpClient(hostname, port);
            _network = _client.GetStream();
            _notchian = new NotchianStream(_network);

            Console.WriteLine("Connected!");
        }

        public void Login(string username)
        {
            Console.WriteLine("Logging in...");

            var pHandshake = new PHandshake
            {
                ProtocolVersion = 404,
                ServerAddress = Hostname,
                ServerPort = Port,
                NextState = 2
            };
            _notchian.FlushPacket(pHandshake);

            Header1("#>> Handshake packet <<#");
            PrintPacket(pHandshake);

            var pLoginStart = new PLoginStart
            {
                PlayerUsername = username
            };
            _notchian.FlushPacket(pLoginStart);

            Header1("#>> Login packet <<#");
            PrintPacket(pLoginStart);

            var pEncryptionRequest = _notchian.ReadPacket<PEncryptionRequest>();

            Header1("#>> Encryption response packet <<#");
            PrintPacket(pEncryptionRequest);
        }

        private static void PrintType(object value)
        {
        RedoSwitch:
            switch (value)
            {
                case string str:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("\"");
                    foreach (char c in str)
                    {
                        if (IsASCIIChar(c))
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write(c);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write("\\{0:x2}", (byte)c);
                        }
                    }
                    Console.Write("\"");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" (n={0})", str.Length);
                    break;

                case int _:
                case uint _:
                case long _:
                case ulong _:
                case short _:
                case ushort _:
                case byte _:
                case sbyte _:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(value);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  0x{0:x}", value);
                    break;

                case char c:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("'{0}'", c);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("  ({0})", (byte)c);
                    break;

                case VarInt varInt:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(varInt.Value);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("  0x{0:x} bytes:", varInt.Value);
                    value = varInt.Bytes;
                    goto RedoSwitch;

                case VarLong varLong:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write(varLong.Value);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("  0x{0:x} bytes:", varLong.Value);
                    value = varLong.Bytes;
                    goto RedoSwitch;

                case byte[] byteArray:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("[ ");
                    for (var index = 0; index < byteArray.Length; index++)
                    {
                        byte b = byteArray[index];
                        if (index > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write(", ");
                        }
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.Write("{0:x2}", b);
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" ] (n={0})", byteArray.Length);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(value);
                    break;
            }
        }

        private static void PrintPacket(IPacket packet)
        {
            using (var memory = new MemoryStream())
            using (var notchian = new NotchianStream(memory))
            {
                packet.Write(in notchian);

                Header2(">>Packet (properties):");
                var properties = new List<(string type, string name, object value)>();

                foreach (PropertyInfo property in packet.GetType().GetProperties())
                {
                    properties.Add((
                        type: property.PropertyType.Name,
                        name: property.Name,
                        value: property.GetValue(packet)
                    ));
                }

                int widthType = properties.Max(p => p.type.Length) + 2;
                int widthName = properties.Max(p => p.name.Length) + 2;
                foreach ((string type, string name, object value) in properties)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(type);

                    Console.Write(new string(' ', widthType - type.Length));

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(name);

                    Console.Write(new string(' ', widthName - name.Length));

                    PrintType(value);
                }
                Console.ResetColor();

                Header2(">>Packet (hex):");

                byte[] array = memory.ToArray();
                int arrayCeil = 8 * (int)Math.Ceiling(array.Length / 8d);
                for (var row = 0; row < arrayCeil; row += 8)
                {
                    for (int i = row; i < row + 8; i++)
                    {
                        if (i < array.Length)
                        {
                            Console.ForegroundColor = IsASCIIChar((char)array[i])
                                ? ConsoleColor.White : ConsoleColor.DarkGray;

                            Console.Write("{0:x2} ", array[i]);
                        }
                        else
                        {
                            Console.Write("   ");
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(" |  ");

                    for (int i = row; i < row + 8; i++)
                    {
                        if (i < array.Length)
                        {
                            Console.ForegroundColor = IsASCIIChar((char)array[i])
                                ? ConsoleColor.White : ConsoleColor.DarkGray;

                            Console.Write((char)array[i]);
                        }
                        else
                        {
                            Console.Write("   ");
                        }
                    }

                    Console.WriteLine();
                }
                Console.ResetColor();
            }
        }

        private static bool IsASCIIChar(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
        }

        public static void Header1(string header)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(header);
            Console.ResetColor();
        }

        public static void Header2(string header)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            Console.WriteLine(header);
            Console.ResetColor();
        }

        public void Dispose()
        {
            using (_client)
            using (_network)
            using (_notchian)
            { }
        }
    }
}
