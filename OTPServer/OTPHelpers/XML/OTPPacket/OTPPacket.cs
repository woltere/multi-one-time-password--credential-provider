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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace OTPHelpers.XML.OTPPacket
{
    [Serializable]
    public class OTPPacket
    {
        public const int __PROTOCOL_VERSION = 1;
        private string _SchemaPath = "C:\\OTPPacketSchema.xsd";

        private bool _ProtocolVersionMismatch;
        public bool ProtocolVersionMismatch
        {
            get { return this._ProtocolVersionMismatch; }
        }

        private int _ProtocolVersion;
        public int ProtocolVersion
        {
            get { return this._ProtocolVersion; }
            set { this._ProtocolVersion = value; }
        }

        private Message _Message;
        public Message Message
        {
            get { return this._Message; }
            set { this._Message = value; }
        }

        private ProcessIdentifier _ProcessIdentifier;
        public ProcessIdentifier ProcessIdentifier
        {
            get { return this._ProcessIdentifier; }
            set { this._ProcessIdentifier = value; }
        }

        private List<Data> _DataItems;
        public List<Data> DataItems
        {
            get { return this._DataItems; }
        }

        private KeyData _KeyData;
        public KeyData KeyData
        {
            get { return this._KeyData; }
            set { this._KeyData = value; }
        }

        public OTPPacket()
        {
            this._ProtocolVersionMismatch = false;
            this._ProtocolVersion = __PROTOCOL_VERSION;
            this._ProcessIdentifier = new ProcessIdentifier();
            this._Message   = new Message();
            this._DataItems = new List<Data>();
            this._KeyData   = new KeyData();
        }

        public OTPPacket(string schemaPath) : this()
        {
            this._SchemaPath = schemaPath;
        }

        ~OTPPacket()
        {
            this._ProtocolVersionMismatch = false;
            this._ProtocolVersion = __PROTOCOL_VERSION;
            this._DataItems = null;
            this._Message   = null;
            this._KeyData   = null;
            this._ProcessIdentifier = null;
        }

        private void CleanUp()
        {
            this._ProtocolVersion = __PROTOCOL_VERSION;
            this._DataItems = new List<Data>();
            this._Message   = new Message();
            this._KeyData   = new KeyData();
            this._ProcessIdentifier = new ProcessIdentifier();
        }

        public int AddDataItem(Data item)
        {
            this._DataItems.Add(item);
            return _DataItems.Count - 1;
        }

        public Data GetDataItemAt(int index)
        {
            return this._DataItems.ElementAt(index);
        }

        public void RemoveDataItemAt(int index)
        {
            this._DataItems.RemoveAt(index);
        }

        public string ToXMLString()
        {
            string xmlString = String.Empty;
            using (StringWriter stringWriter = new StringWriter())
            {
                XmlWriter xmlWriter = XmlWriter.Create(stringWriter);

                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("OTPPacket");
                xmlWriter.WriteAttributeString("version", ProtocolVersion.ToString());

                this.ProcessIdentifier.ToXmlString(ref xmlWriter);
                this.Message.ToXmlString(ref xmlWriter);

                foreach (Data item in DataItems)
                    item.ToXmlString(ref xmlWriter);

                // Server should never send any KeyData
                this.KeyData.ToXmlString(ref xmlWriter);

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();

                xmlWriter.Close();
                xmlString = stringWriter.ToString();
            }
            return xmlString;
        }

        private XmlReaderSettings GetXMLReaderSettings(bool validateXml)
        {
            XmlReaderSettings xmlSettings = new XmlReaderSettings();

            xmlSettings.Schemas.Add("", this._SchemaPath);
            xmlSettings.ValidationType = (validateXml) ? ValidationType.Schema : ValidationType.None;
            xmlSettings.XmlResolver = null;

            return xmlSettings;
        }

        public bool SetFromXML(string xmlString, bool validateXml)
        {
            bool success = false;
            using (XmlTextReader xmlTextReader = new XmlTextReader(new StringReader(xmlString)))
                    success = SetFromXML(xmlTextReader, validateXml);
            return success;
        }

        public bool SetFromXML(Stream xmlStream, bool validateXml)
        {
            string packet = ReadOTPPacketFromStream(xmlStream);
            return SetFromXML(packet, validateXml);
        }

        public bool SetFromXML(XmlTextReader xmlTextReader, bool validateXml)
        {
            XmlReaderSettings xmlSettings = GetXMLReaderSettings(validateXml);
            XmlReader xmlReader = XmlReader.Create(xmlTextReader, xmlSettings);

            bool success = true;
            try
            {
                while (xmlReader.Read() && success)
                {
                    XmlNodeType nType = xmlReader.NodeType;
                    if (nType == XmlNodeType.Element)
                    {
                        success = ParseElementNode(xmlReader);
                        continue;
                    }
                }
            }
            catch (XmlException)
            {
                // Validation failed
                success = false;
            }

            if (!success)
                CleanUp();

            return success;
        }

        public static string ReadOTPPacketFromStream(Stream stream)
        {
            // Read the  message sent by the client.
            // The end of the message is signaled using the
            // "</OTPPacket>" marker.
            // The Read() will timeout after 5 seconds.
            stream.ReadTimeout = 10 * 1000;
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = stream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF.
                if (messageData.ToString().IndexOf("</OTPPacket>") != -1)
                {
                    break;
                }
            } while (bytes != 0);            

            return messageData.ToString();
        }

        private bool ParseElementNode(XmlReader xmlReader)
        {
            bool success = true;
            if (xmlReader.Name.Equals("OTPPacket"))
            {
                success = ParseAttributes(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("PID"))
            {
                success = this._ProcessIdentifier.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Message"))
            {
                success = this._Message.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Data"))
            {
                Data item = new Data();
                success = item.SetFromXMLReader(xmlReader);

                this._DataItems.Add(item);
                goto Return;
            }
            else if (xmlReader.Name.Equals("KeyData"))
            {
                success = this._KeyData.SetFromXMLReader(xmlReader);
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

        private bool ParseAttributes(XmlReader xmlReader)
        {
            bool success = true;

            if (xmlReader.HasAttributes && xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    if (xmlReader.Name.Equals("version"))
                    {
                        ProtocolVersion = XmlConvert.ToInt32(xmlReader.Value);
                        if (ProtocolVersion > __PROTOCOL_VERSION)
                        {
                            this._ProtocolVersionMismatch = true;
                            success = false;
                        }
                    }
                } while (success && xmlReader.MoveToNextAttribute());
            }
            else
            {
                success = false;
            }

            return success;
        }
    }
}
