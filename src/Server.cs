using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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

    Console.WriteLine($"Received {ReceivedData.Length} bytes from {SourceEndPoint}: {ReceivedString}");

    byte[] rec = ReceivedData;
    int QuestionCount = BinaryPrimitives.ReadUInt16BigEndian(ReceivedData.AsSpan(4));

    byte[] AnswerSection = [
            0xC0, 0x0C,             // Name: pointer to domain at offset 12
            0x00, 0x01,             // TYPE: A record (IPv4 address)
            0x00, 0x01,             // CLASS: Internet
            0x00, 0x00, 0x01, 0x2C, // TTL: 300 seconds
            0x00, 0x04,             // RDLENGTH: 4 bytes
            127, 0, 0, 1            // RDATA: 127.0.0.1 (localhost)
    ];

    while (QuestionCount > 1)
    {
        QuestionCount--;
        UInt16 offset = BinaryPrimitives.ReadUInt16BigEndian(AnswerSection.AsSpan(AnswerSection.Length - 16));
        offset &= 0b0011_1111_1111_1111;

        while (rec[offset++] != 0) ;
        offset += 4;

        byte[] TempAnswerSection = [
            0xC0, (byte)offset,     // Name: pointer to domain at offset
            0x00, 0x01,             // TYPE: A record (IPv4 address)
            0x00, 0x01,             // CLASS: Internet
            0x00, 0x00, 0x01, 0x2C, // TTL: 300 seconds
            0x00, 0x04,             // RDLENGTH: 4 bytes
            127, 0, 0, 1            // RDATA: 127.0.0.1 (localhost)
        ];
        AnswerSection = [.. AnswerSection, .. TempAnswerSection];
    }


    ushort ResponseFlagsBits = (ushort)((rec[2] << 8) | (rec[3]));
    // ushort ResponseFlagsBits = BinaryPrimitives.ReadUInt16BigEndian(ReceivedData.AsSpan(2));

    ResponseFlagsBits |= 0b___1_____0000______0____0___0___0___000_____0100;
    /*                       QR    OPCODE    AA   TC  RD  RA   Z      RCODE      */
    byte[] ResponseFlags = new byte[2];
    BinaryPrimitives.WriteUInt16BigEndian(ResponseFlags, ResponseFlagsBits);
    // byte[] ResponseFlags = [..BitConverter.GetBytes(ResponseFlagsBits).Reverse()];

    byte[] Response = [
            // Header (12 bytes)
            rec[0], rec[1],       // Transaction ID
            ..ResponseFlags,      // Response flags
            rec[4], rec[5],       // Question Count: 1
            rec[4], rec[5],       // Answer Count: 1 (NOT 0!)
            0x00, 0x00,           // Authority Count: 0
            0x00, 0x00,           // Additional Count: 0
            ..ReceivedData[12..], // Question Section
            ..AnswerSection,      // Answer Section
            ];


    // Send Response
    UdpClient.Send(Response, Response.Length, SourceEndPoint);
    Hexdump(ReceivedData);
    bit_dump(ReceivedData);
}

/*

Idx       | Hex                                             | ASCII
----------+-------------------------------------------------+-----------------
00000000  c6 ff 01 00 00 02 00 00  00 00 00 00 03 61 62 63  |.............abc|
00000010  11 6c 6f 6e 67 61 73 73  64 6f 6d 61 69 6e 6e 61  |.longassdomainna|
00000020  6d 65 03 63 6f 6d 00 00  01 00 01 03 64 65 66 c0  |me.com......def.|
00000030  10 00 01 00 01                                    |.....|

11000110 11111111 00000001 00000000 00000000 00000010 00000000 00000000 00000000 00000000 00000000 00000000 00000011 01100001 01100010 01100011
00010001 01101100 01101111 01101110 01100111 01100001 01110011 01110011 01100100 01101111 01101101 01100001 01101001 01101110 01101110 01100001
01101101 01100101 00000011 01100011 01101111 01101101 00000000 00000000 00000001 00000000 00000001 00000011 01100100 01100101 01100110 11000000
00010000 00000000 00000001 00000000 00000001

ID:     0xC6FF
00000001 00000000  ->    0              0000     0     0     1     0  000     0000
Flags: 0x0100 →       QR=0 (query), OPCODE=0, AA=0, TC=0, RD=1, RA=0, Z=0, RCODE=0
QDCOUNT: 0x0002 → 2 
ANCOUNT: 0x0000 → 0 answers
NSCOUNT: 0x0000 → 0 authority records
ARCOUNT: 0x0000 → 0 additional rec

*/

void Log(string msg,
[CallerFilePath] string file = "",
[CallerLineNumber] int line = 0,
[CallerMemberName] string member = "")
{
    Console.WriteLine($"{System.IO.Path.GetFileName(file)}:{line} {member}: {msg}");
}