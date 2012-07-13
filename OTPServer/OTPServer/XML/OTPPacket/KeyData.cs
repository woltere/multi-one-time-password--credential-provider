using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    class KeyData : ISerializable
    {
        public enum TYPE : int
        {
            NONE    = 0,
            RSA     = 1
        }

        private TYPE _Type;
        public TYPE Type
        {
            get { return this._Type; }
            set { this._Type = value; }
        }

        private int _Modulus;
        public int Modulus
        {
            get { return this._Modulus; }
            set { this._Modulus = value; }
        }

        private int _Exponent;
        public int Exponent
        {
            get { return this._Exponent; }
            set { this._Exponent = value; }
        }

        public KeyData() 
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        ~KeyData() 
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        private void CleanUp()
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        public bool SetFromXMLReader(XmlTextReader xmlReader)
        {
            bool success = true;
            while (xmlReader.Read() && success)
            {
                XmlNodeType nType = xmlReader.NodeType;
                if (nType == XmlNodeType.Element)
                {
                    success = parseElementNode(xmlReader);
                    continue;
                }
            }

            if (!success)
                CleanUp();

            return success;
        }

        private bool ParseElementNode(XmlTextReader xmlReader)
        {
            bool success = true;

            if (xmlReader.Name.Equals("RSAKeyValue"))
            {
                Type = TYPE.RSA;
                goto Return;
            }
            else if (xmlReader.Name.Equals("Exponent"))
            {
                Exponent = xmlReader.ReadContentAsInt();
                goto Return;
            }
            else if (xmlReader.Name.Equals("Modulus"))
            {
                Modulus = xmlReader.ReadContentAsInt();
                goto Return;
            }
            else
            {
                success = false;
                goto Return;
            }

        Return:
            return success;
        }
    }
}
