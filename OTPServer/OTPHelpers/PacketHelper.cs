using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OTPHelpers.XML.OTPPacket;

namespace OTPHelpers
{
    public static class PacketHelper
    {
        public static void SetMessageAttributes(ref OTPPacket otpPacket, Message.TYPE type, string textMessage, Message.STATUS statusCode)
        {
            otpPacket.Message.Type = type;
            otpPacket.Message.TextMessage = textMessage;
            otpPacket.Message.StatusCode = statusCode;
        }

        public static OTPPacket CreatePacket(int pid)
        {
            OTPPacket otpPacket = new OTPPacket();
            otpPacket.ProcessIdentifier.ID = pid;
            return otpPacket;
        }

        public static OTPPacket CreateErrorPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.ERROR, message, statusCode);

            return otpPacket;
        }

        public static OTPPacket CreateSuccessPacket(int pid, string message, Message.STATUS statusCode)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.SUCCESS, message, statusCode);

            return otpPacket;
        }

        public static OTPPacket CreateHelloPacket(int pid)
        {
            OTPPacket otpPacket = CreatePacket(pid);
            SetMessageAttributes(ref otpPacket, Message.TYPE.HELLO, String.Empty, Message.STATUS.NONE);

            return otpPacket;
        }
    }
}
