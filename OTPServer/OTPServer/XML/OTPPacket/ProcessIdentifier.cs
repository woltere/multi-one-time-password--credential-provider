using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    class ProcessIdentifier
    {
        public readonly enum LEGAL_ATTRIBUTES
        {
        }

        public readonly enum LEGAL_ELEMENTS
        {
        }

        private int _ID = 0;
        public int ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }

        public bool setFromXML(XmlTextReader xmlReader)
        {
            bool success = true;

            if (xmlReader.HasAttributes || xmlReader.NodeType != XmlNodeType.Element)
                return false;

            this._ID = xmlReader.ReadContentAsInt();

            return success;
        }
    }
}
