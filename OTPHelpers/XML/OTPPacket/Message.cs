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
using System.Xml;

namespace OTPHelpers.XML.OTPPacket
{
    [Serializable]
    public class Message
    {
        public enum TYPE : int
        {
            NONE    = 0,

            SUCCESS = 1,
            ERROR   = 2,

            HELLO   = 3,
            ADD     = 4, // Add partial data such as Username, OTP, Public Key etc.
            VERIFY  = 5,
            RESYNC  = 6,

            PROTOCOL_VERSION_1_END = 7,
        }

        public enum STATUS : int
        {
            NONE = 0,

            S_OK           = 200, // General success
            S_RESYNC_OK    = 201, // Resynchronization successfull

            PR_SWITCH_REQ  = 303, // Requesting an other protocol version

            E_MALFORMED    = 400, // Malformed data
            E_NOT_VERIFIED = 401, // OTP could not be verified / is wrong
            E_NOT_AUTHED   = 403, // Not authorized, e.g. wrong mac
            E_NOT_FOUND    = 404, // Data (e.g. user or pid) not found
            E_LOCKED       = 423, // E.g. tried to overwrite an existing username-field
            E_INCOMPLETE   = 424, // E.g. tried to use an authorized request without providing a public key first
            E_UNKNOWN      = 500  // Unspecified/Unknown internal error 
        }

        private TYPE _Type;
        public TYPE Type
        {
            get { return this._Type; }
            set { this._Type = value; }
        }

        private Object _Content = null;
        public Object Content
        {
            get { return this._Content; }
            set { this._Content = value; }
        }

        public string TextMessage
        {
            get { return (string)Content; }
            set { Content = value; }
        }

        private STATUS _StatusCode;
        public STATUS StatusCode
        {
            get { return this._StatusCode; }
            set { this._StatusCode = value; }
        }

        private string _MAC;
        public byte[] MAC
        {
            get { return Convert.FromBase64String(this._MAC); }
            set { this._MAC = Convert.ToBase64String(value); }
        }

        private long _TimeStamp;
        public long TimeStamp
        {
            get { return this._TimeStamp; }
            set { this._TimeStamp = value; }
        }

        public Message() 
        {
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            TimeStamp = 0;
            this._MAC = String.Empty;
        }

        ~Message() 
        {
            Content = null;
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            TimeStamp = 0;
            this._MAC = String.Empty;
        }

        private void CleanUp()
        {
            Content = null;
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            TimeStamp = 0;
            this._MAC = String.Empty;
        }

        public bool SetFromXMLReader(XmlReader xmlReader)
        {
            bool success = ParseAttributes(xmlReader);

            xmlReader.MoveToElement();
            if (success && !xmlReader.IsEmptyElement)
            {
                xmlReader.MoveToContent();
                Content = xmlReader.ReadString();
            }

            if (!success)
                CleanUp();

            return success;
        }

        private bool ParseAttributes(XmlReader xmlReader)
        {
            bool success = true;

            for (int i = 0; i < xmlReader.AttributeCount; i++)
            {
                xmlReader.MoveToAttribute(i);
                if (xmlReader.Name.Equals("type"))
                {
                    if (xmlReader.Value.Equals("HELLO"))
                        Type = TYPE.HELLO;
                    else if (xmlReader.Value.Equals("ADD"))
                        Type = TYPE.ADD;
                    else if (xmlReader.Value.Equals("VERIFY"))
                        Type = TYPE.VERIFY;
                    else if (xmlReader.Value.Equals("RESYNC"))
                        Type = TYPE.RESYNC;
                    else if (xmlReader.Value.Equals("SUCCESS"))
                        Type = TYPE.SUCCESS;
                    else if (xmlReader.Value.Equals("ERROR"))
                        Type = TYPE.ERROR;
                    continue;
                }
                else if (xmlReader.Name.Equals("status"))
                {
                    StatusCode = (STATUS)XmlConvert.ToInt32(xmlReader.Value);
                    continue;
                }
                else if (xmlReader.Name.Equals("timestamp"))
                {
                    TimeStamp = XmlConvert.ToInt64(xmlReader.Value);
                    continue;
                }
                else if (xmlReader.Name.Equals("mac"))
                {
                    this._MAC = xmlReader.Value;
                    continue;
                }
            }

            return success;
        }

        public void ToXmlString(ref XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Message");

            xmlWriter.WriteAttributeString("type", Enum.GetName(typeof(TYPE), ((int)this._Type)));

            if (this._StatusCode != STATUS.NONE)
                xmlWriter.WriteAttributeString("status", ((int)this._StatusCode).ToString());

            if (this._TimeStamp != 0)
                xmlWriter.WriteAttributeString("timestamp", this._TimeStamp.ToString());

            if (this._MAC != String.Empty)
                xmlWriter.WriteAttributeString("mac", this._MAC);

            if (this.TextMessage != String.Empty)
                xmlWriter.WriteString(this.TextMessage);

            xmlWriter.WriteEndElement();
        }
    }
}
