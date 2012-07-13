using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    [Serializable]
    class Message
    {
        public enum TYPE : int
        {
            NONE    = 0,

            HELLO   = 1,
            ADD     = 1, // Add partial data such as Username, OTP, Public Key etc.
            VERIFY  = 1,
            RESYNC  = 1,

            SUCCESS = 1,
            ERROR   = 1
        }

        public enum STATUS : int
        {
            NONE = 0,

            S_OK        = 200, // General success
            S_RESYNC_OK = 201, // Resynchronization successfull

            E_ERROR        = 400, // General error
            E_NOT_VERIFIED = 401, // OTP could not be verified / is wrong
            E_NOT_FOUND    = 404, // Data (e.g. user or pid) not found
            E_INCOMPLETE   = 480, // Incomplete data. E.g. verification: one or all of Username, OTP or Public Key are not set. comm: client didn't send a MAC
            E_UNKNOWN      = 500  // Unspecified error 
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

        public STATUS _StatusCode;
        public STATUS StatusCode
        {
            get { return this._StatusCode; }
            set { this._StatusCode = value; }
        }

        public string _MAC;
        public string MAC
        {
            get { return this._MAC; }
            set { this._MAC = value; }
        }

        public Message() 
        {
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            MAC = "";
        }

        ~Message() 
        {
            Content = null;
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            MAC = "";
        }

        private void CleanUp()
        {
            Content = null;
            Type = TYPE.NONE;
            StatusCode = STATUS.NONE;
            MAC = "";
        }

        public bool SetFromXMLReader(XmlTextReader xmlReader)
        {
            Content = xmlReader.ReadContentAsObject();
            bool success = parseAttributes(xmlReader);

            if (!success)
                CleanUp();

            return success;
        }

        private bool ParseAttributes(XmlTextReader xmlReader)
        {
            bool success = true;

            int attributeCount = xmlReader.AttributeCount;
            for (int i = 0; i < attributeCount; i++)
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
                    goto Return;
                }
                else if (xmlReader.Name.Equals("status"))
                {
                    StatusCode = (STATUS)XmlConvert.ToInt32(xmlReader.Value);
                    goto Return;
                }
                else if (xmlReader.Name.Equals("mac"))
                {
                    MAC = xmlReader.Value;
                    goto Return;
                }
            }

        Return:
            return success;
        }
    }
}
