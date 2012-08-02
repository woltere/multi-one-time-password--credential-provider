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
    public class ProcessIdentifier
    {
        public const int NONE = 0;

        private int _ID = 0;
        public int ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }

        public bool SetFromXMLReader(XmlReader xmlReader)
        {
            bool success = true;

            if (!xmlReader.IsEmptyElement)
            {
                xmlReader.MoveToContent();
                this._ID = XmlConvert.ToInt32(xmlReader.ReadString());
            }
            else
                success = false;

            return success;
        }

        public void ToXmlString(ref XmlWriter xmlWriter)
        {
            if (this._ID > NONE)
                xmlWriter.WriteElementString("PID", this._ID.ToString());
        }
    }
}
