using PacketDotNet;
using System;
using System.Text;

namespace SharpPcap
{
    class Program
    {
        static void Main(string[] args)
        {
            CaptureDeviceList devices = CaptureDeviceList.Instance;

            int readTimeoutMilliseconds = 1000;

            foreach (ICaptureDevice device in devices)
            {
                device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
                device.Filter = "ip and tcp";

                device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);

                device.StartCapture();

                Console.WriteLine(device.Name + " has started capturing...");
            }

            Console.ReadKey();

            foreach (ICaptureDevice device in devices)
            {
                device.StopCapture();
                device.Close();
            }

            Console.ReadKey();
        }

        private static void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            Packet packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            if (packet is UdpPacket)
            {
                UdpPacket udp = (UdpPacket)packet;
                IPv4Packet ip = (IPv4Packet)udp.PayloadPacket;

                string sourceMac = Fix("No MAC Address", 17);
                string sourceAddress = Fix(ip.SourceAddress.ToString(), 15);
                string destMac = Fix("No MAC Address", 17);
                string destAddress = Fix(ip.DestinationAddress.ToString(), 15);

                Console.WriteLine("UDP Packet: " + sourceMac + " " + sourceAddress + " => " + destMac + " " + destAddress);
            }
            else if (packet is TcpPacket)
            {
                TcpPacket tcp = (TcpPacket)packet;
                IPv4Packet ip = (IPv4Packet)tcp.PayloadPacket;

                string sourceMac = Fix("No MAC Address", 17);
                string sourceAddress = Fix(ip.SourceAddress + ":" + tcp.SourcePort, 20);
                string destMac = Fix("No MAC Address", 17);
                string destAddress = Fix(ip.DestinationAddress + ":" + tcp.DestinationPort, 20);

                Console.WriteLine("TCP Packet: " + sourceMac + " " + sourceAddress + " => " + destMac + " " + destAddress);
            }           
            else if (packet is EthernetPacket)
            {
                EthernetPacket eth = (EthernetPacket)packet;
                IPv4Packet ip = (IPv4Packet)eth.PayloadPacket;

                string sourceMac = FixMac(eth.SourceHwAddress.ToString());
                string sourceAddress = Fix(ip.SourceAddress.ToString(), 20);
                string destMac = FixMac(eth.DestinationHwAddress.ToString());
                string destAddress = Fix(ip.DestinationAddress.ToString(), 20);

                Console.WriteLine("ETH Packet: " + sourceMac + " " + sourceAddress + " => " + destMac + " " + destAddress);
            }
        }

        private static string Fix(string text, int length)
        {
            while (text.Length != length)
            {
                text = " " + text;
            }

            return text;
        }

        private static string FixMac(string mac)
        {
            string result = "";

            for (int i = 0; i < mac.Length; i++)
            {
                result += mac[i];

                if (i % 2 == 1)
                    result += "-";
            }

            return result.Substring(0, result.Length - 1);
        }
    }
}
