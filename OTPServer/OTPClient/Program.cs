using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using OTPHelpers.XML.OTPPacket;
using System.Security.Cryptography;
using System.Threading;
using OTPHelpers;
using System.Security.Authentication;

namespace OTPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: OTPClient.exe server-ip");
                return;
            }

            string server = args[0];

            Console.WriteLine("Welcome...");

            RSACryptoServiceProvider key = null;
            TcpClient client = null;
            SslStream sslStream = null;

            try
            {
                key = new RSACryptoServiceProvider();

                Console.WriteLine("Establishing Connection...");

                client = new TcpClient(server, 16588);            
                sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                try
                {
                    Console.WriteLine("Authenticating as Client...");
                    sslStream.AuthenticateAsClient(server);
                }
                catch (AuthenticationException)
                {
                    Console.WriteLine("An error occured while authenticating the server's certificate. Aborting...");
                    return;
                }

                OTPPacket request = new OTPPacket();
                OTPPacket response = new OTPPacket();
                bool success;
                Data data;
                int protocolVersion = 2;

                /////////
                bool protocolMatched = false;
                while (!protocolMatched)
                {
                    Console.WriteLine("\nSending HELLO packet (starting with wrong protocol version)");

                    request = PacketHelper.CreateHelloPacket(0);
                    request.ProtocolVersion = protocolVersion;

                    Console.WriteLine("REQ:  " + request.ToXMLString());
                    WritePacketToStream(sslStream, request);

                    response = new OTPPacket();
                    success = response.SetFromXML(sslStream, true);
                    Console.WriteLine("RESP: " + response.ToXMLString());

                    if (response.Message.StatusCode == Message.STATUS.PR_SWITCH_REQ)
                        protocolVersion = response.ProtocolVersion;
                    else
                        protocolMatched = true;
                }

                //////////
                Console.WriteLine("\nSending ADD packet containing public RSA KeyData");

                request = PacketHelper.CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.SetFromXML("<KeyData>" + key.ToXmlString(false) + "</KeyData>", false);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());

                //////////
                Console.WriteLine("\nSending ADD packet containing DATA attribute USERNAME");

                request = PacketHelper.CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.Message.TimeStamp = NowMilli();
                request.Message.MAC = key.SignData(
                    Encoding.UTF8.GetBytes(response.ProcessIdentifier.ID.ToString() + request.Message.TimeStamp.ToString()),
                    new MD5CryptoServiceProvider());

                data = new Data();
                data.Username = "testUserName";
                request.DataItems.Add(data);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());

                //////////
                /*
                Console.WriteLine("\nSending ADD packet containing DATA attribute USERNAME (Wrong MAC test)");

                request = PacketHelper.CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = Message.TYPE.ADD;
                request.Message.TimeStamp = NowMilli();
                request.Message.MAC = key.SignData(
                    Encoding.UTF8.GetBytes(response.ProcessIdentifier.ID.ToString() + (request.Message.TimeStamp + 1).ToString()),
                    new MD5CryptoServiceProvider());

                data = new Data();
                data.Username = "testUserName";
                request.DataItems.Add(data);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());
                */
                //////////
                /*
                Console.WriteLine("\nTimeout test");

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());
                */
            }
            catch (SocketException)
            {
                Console.WriteLine("A connection problem occured...");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown error occured:");
                Console.WriteLine("  " + e.Message);
            }
            finally
            {
                if (sslStream != null)
                {
                    sslStream.Close();
                    sslStream.Dispose();
                }
                if (client != null)
                    client.Close();      
                if (key != null)
                    key.Dispose();
            }
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

        private static void WritePacketToStream(SslStream stream, OTPPacket otpPacket)
        {
            string otpPacketAsString = otpPacket.ToXMLString();
            byte[] otpPacketAsByteArray = Encoding.UTF8.GetBytes(otpPacketAsString);

            stream.Write(otpPacketAsByteArray);
            stream.Flush();
        }

        // MILLISECONDS
        private static long NowMilli()
        {
            DateTime epochStart = new DateTime(1970, 1, 1);
            DateTime now = DateTime.Now;
            TimeSpan ts = new TimeSpan(now.Ticks - epochStart.Ticks);
            return (Convert.ToInt64(ts.TotalMilliseconds));
        }
    }
}
