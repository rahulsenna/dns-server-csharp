using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static HexdumpUtil;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");


// Resolve UDP address
IPAddress Ipaddress = IPAddress.Parse("127.0.0.1");
int port = 2053;
IPEndPoint UdpEndPoint = new(Ipaddress, port);

// Create UDP socket
UdpClient UdpClient = new(UdpEndPoint);

while (true)
{
    // Receive data
    IPEndPoint SourceEndPoint = new(IPAddress.Any, 0);
    byte[] ReceivedData = UdpClient.Receive(ref SourceEndPoint);
    string ReceivedString = Encoding.Latin1.GetString(ReceivedData);
    Hexdump(ReceivedData);

    Console.WriteLine($"Received {ReceivedData.Length} bytes from {SourceEndPoint}: {ReceivedString}");

    // Create an empty response
    byte[] Response = Encoding.Latin1.GetBytes("");

    // Send Response
    UdpClient.Send(Response, Response.Length, SourceEndPoint);
}