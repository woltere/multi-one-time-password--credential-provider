using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace OTPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome...");

            string server = "192.168.1.3";
            TcpClient client = new TcpClient(server, 16588);

            Console.WriteLine("Establishing Connection...");
            using (SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
            {
                Console.WriteLine("Authenticating as Client...");
                sslStream.AuthenticateAsClient(server);

                bool active = true;

                //while (active)
                //{
                    Console.WriteLine("What do you want to send? XML, end with a line '.end.', quit with '.stop.' :");
                    string sendToServer = String.Empty;
                    string buffer = String.Empty;
                    do
                    {
                        buffer = String.Empty;
                        buffer = Console.ReadLine();

                        if (!buffer.Contains(".end.") && !buffer.Contains(".stop."))
                            sendToServer += buffer;

                        if (buffer.Contains(".stop."))
                            active = false;
                    } while (!buffer.Contains(".end."));

                    if (!active)
                        goto Abort;

                    if (!sendToServer.Equals(String.Empty))
                    {
                        Console.WriteLine("Sending to server.....");
                        WritePacketToStream(sslStream, sendToServer);
                    }
                    else
                    {
                        Console.WriteLine("Waiting...");
                    }

                    Console.WriteLine("Reading from server...");
                    Console.WriteLine(ReadPacketFromStream(sslStream));
                //}
            }

        Abort:
            // Disconnect and close the client
            client.Close();
        }
        // The following method is invoked by the RemoteCertificateValidationDelegate.
        // This allows you to check the certificate and accept or reject it
        // return true will accept the certificate
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Console.WriteLine("Validating Server Certificate...");

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                Console.WriteLine("Validation OK...");
                return true;
            }

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public static void WritePacketToStream(SslStream stream, string str)
        {
            byte[] stringAsByteArray = Encoding.UTF8.GetBytes(str);

            stream.Write(stringAsByteArray);
            stream.Flush();
        }

        public static string ReadPacketFromStream(SslStream stream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "</OTPPacket>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("</OTPPacket>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
    }
}
