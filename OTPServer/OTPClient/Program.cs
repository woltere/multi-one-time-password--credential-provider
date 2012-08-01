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
        static int Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: OTPClient.exe <request-type> <server-ip> <username> <otp> { <otp> }");
                Console.WriteLine("       <request-type> := verify | resync");
                return -1;
            }

            int requestType = -1;
            if (args[1].Equals("verify"))
                requestType = 0;
            else if (args[1].Equals("resync"))
                requestType = 1;
            else
            {
                Console.WriteLine("Usage: OTPClient.exe <server-ip> <request-type> <username> <otp> { <otp> }");
                Console.WriteLine("       <request-type> := verify | resync");
                return -1;
            }

            string server = args[0];
            string userName = args[2];
            string otp1 = args[3];

            string otp2 = String.Empty;
            if (args.Length > 4)
                otp2 = args[4];

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
                    return -1;
                }

                OTPPacket request = new OTPPacket();
                OTPPacket response = new OTPPacket();
                bool success;
                Data data;

                /////////
                Console.WriteLine("\nSending HELLO packet");

                request = PacketHelper.CreateHelloPacket(0);

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());

                if (response.Message.Type == Message.TYPE.ERROR)
                    return 1;

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

                if (response.Message.Type == Message.TYPE.ERROR)
                    return 1;

                //////////
                Console.WriteLine("\nSending packet containing DATA-attributes USERNAME and OTP(s)");
                if (requestType == 0)
                    Console.WriteLine("Verify OTP...");
                else if (requestType == 1)
                    Console.WriteLine("Resync OTPs...");

                request = PacketHelper.CreatePacket(response.ProcessIdentifier.ID);
                request.Message.Type = (requestType == 0) ? Message.TYPE.VERIFY : Message.TYPE.RESYNC;
                request.Message.TimeStamp = NowMilli();
                request.Message.MAC = key.SignData(
                    Encoding.UTF8.GetBytes(response.ProcessIdentifier.ID.ToString() + request.Message.TimeStamp.ToString()),
                    new MD5CryptoServiceProvider());

                data = new Data();
                data.Username = userName;
                request.DataItems.Add(data);

                data = new Data();
                data.OneTimePassword = otp1;
                request.DataItems.Add(data);

                if (requestType == 1)
                {
                    data = new Data();
                    data.OneTimePassword = otp2;
                    request.DataItems.Add(data);
                }

                Console.WriteLine("REQ:  " + request.ToXMLString());
                WritePacketToStream(sslStream, request);

                response = new OTPPacket();
                success = response.SetFromXML(sslStream, true);
                Console.WriteLine("RESP: " + response.ToXMLString());

                if (response.Message.Type == Message.TYPE.ERROR)
                    return 1;

                return 0;
            }
            catch (SocketException e)
            {
                Console.WriteLine("A connection problem occured...");
                Console.WriteLine("  " + e.Message);
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

            return 1;
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
