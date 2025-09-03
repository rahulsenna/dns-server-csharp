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

    byte[] AnswerSection = [
            0xC0, 0x0C,             // Name: pointer to domain at offset 12
            0x00, 0x01,             // TYPE: A record (IPv4 address)
            0x00, 0x01,             // CLASS: Internet
            0x00, 0x00, 0x01, 0x2C, // TTL: 300 seconds
            0x00, 0x04,             // RDLENGTH: 4 bytes
            127, 0, 0, 1            // RDATA: 127.0.0.1 (localhost)
    ];

    byte[] Response = [
            // Header (12 bytes)
            ReceivedData[0], ReceivedData[1], // Transaction ID
            0x81, 0x80,           // Response flags (NOT 0x01, 0x00!)
            0x00, 0x01,           // Question Count: 1
            0x00, 0x01,           // Answer Count: 1 (NOT 0!)
            0x00, 0x00,           // Authority Count: 0
            0x00, 0x00,           // Additional Count: 0
            ..ReceivedData[12..], // Question Section
            ..AnswerSection,      // Answer Section
            ];
    

    // Send Response
    UdpClient.Send(Response, Response.Length, SourceEndPoint);
}