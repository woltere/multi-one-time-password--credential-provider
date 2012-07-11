using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    class ProcessIdentifier
    {
        private int _ID = 0;
        public int ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }

        public bool setFromXMLReader(XmlTextReader xmlReader)
        {
            //if (xmlReader.HasAttributes || xmlReader.NodeType != XmlNodeType.Element)
            //    return false;

            bool success = true;

            this._ID = xmlReader.ReadContentAsInt();

            return success;
        }
    }
}
