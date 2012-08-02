/* * * * * * * * * * * * * * * * * * * * *
**
** Copyright 2012 Dominik Pretzsch
** 
**    Licensed under the Apache License, Version 2.0 (the "License");
**    you may not use this file except in compliance with the License.
**    You may obtain a copy of the License at
** 
**        http://www.apache.org/licenses/LICENSE-2.0
** 
**    Unless required by applicable law or agreed to in writing, software
**    distributed under the License is distributed on an "AS IS" BASIS,
**    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
**    See the License for the specific language governing permissions and
**    limitations under the License.
**
** * * * * * * * * * * * * * * * * * * */

using System;
using OTPHelpers.XML.OTPPacket;

namespace OTPHelpers
{
    public static class PacketHelper
    {
        public static string SchemaPath = String.Empty;

        public static void SetMessageAttributes(ref OTPPacket otpPacket, Message.TYPE type, string textMessage, Message.STATUS statusCode)
        {
            otpPacket.Message.Type = type;
            otpPacket.Message.TextMessage = textMessage;
            otpPacket.Message.StatusCode = statusCode;
        }

        public static OTPPacket CreatePacket(int pid)
        {
            OTPPacket otpPacket;
            if (SchemaPath != String.Empty)
                otpPacket = new OTPPacket(SchemaPath);
            else
                otpPacket = new OTPPacket();

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
