using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OTPServer.XML.OTPPacket;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace OTPServer.Authority.Storage
{
    class ProcessDataStorage
    {
        private static Dictionary<int, ProcessData> __ProcessIdentifierProcessDataMap = new Dictionary<int,ProcessData>();
        private static LinkedList<ProcessAge> __TimeProcessIdentifierMap = new LinkedList<ProcessAge>();

        public int ProcessExists(OTPPacket otpPacket)
        {
            if (otpPacket.ProcessIdentifier.ID <= 0)
                return 0;

            return ProcessExists(otpPacket.ProcessIdentifier.ID);
        }

        public int ProcessExists(int pid)
        {
            if (__ProcessIdentifierProcessDataMap.ContainsKey(pid))
                return pid;
            return 0;
        }

        public int CreateProcess(OTPPacket otpPacket)
        {
            if (otpPacket.Message.Type != Message.TYPE.HELLO)
                return 0; // LOOK: The first valid pid is 1!

            int processIdentifier = CreateProcessIdentifier();            
            ProcessAge processAge = new ProcessAge(processIdentifier);
            ProcessData processData = new ProcessData(processAge);

            __ProcessIdentifierProcessDataMap.Add(processIdentifier, processData);
            __TimeProcessIdentifierMap.AddLast(processAge);

            return processIdentifier;
        }

        private int CreateProcessIdentifier()
        {
            return ProcessAge.Now();
        }

        public ProcessData GetProcess(OTPPacket otpPacket)
        {
            if (otpPacket.ProcessIdentifier.ID <= 0)
                return null;

            return GetProcess(otpPacket.ProcessIdentifier.ID);
        }

        public ProcessData GetProcess(int pid)
        {
            ProcessData processData;
            bool success = __ProcessIdentifierProcessDataMap.TryGetValue(pid, out processData);

            if (!success)
                return null;
            return processData;
        }

        public bool AddDataFromPacketToProcess(OTPPacket otpPacket)
        {
            ProcessData processData = GetProcess(otpPacket);

            if (processData == null)
                return false;

            return processData.AddData(otpPacket);
        }

        public ProcessAge GetOldestProcess()
        {
            LinkedListNode<ProcessAge> oldestProcess = __TimeProcessIdentifierMap.First;
            return oldestProcess.Value;
        }        

        public void RemoveProcess(int processIdentifier)
        {
            RemoveProcess(GetProcess(processIdentifier));
        }

        public void RemoveProcess(ProcessAge processAge)
        {
            RemoveProcess(processAge.ProcessIdentifier);
        }

        public void RemoveProcess(ProcessData processData)
        {
            if (processData == null)
                return;

            __TimeProcessIdentifierMap.Remove(processData.ProcessAgeRef);
            __ProcessIdentifierProcessDataMap.Remove(processData.ProcessIdentifier);
        }

        private class ProcessData
        {
            private ProcessAge _ProcessAgeRef;
            public ProcessAge ProcessAgeRef
            {
                get { return this._ProcessAgeRef; }
                set { this._ProcessAgeRef = value; }
            }

            private int _ProcessIdentifier;
            public int ProcessIdentifier
            {
                get { return this._ProcessIdentifier; }
                set { this._ProcessIdentifier = value; }
            }

            /*
            private Message _Message;
            public Message Message
            {
                get { return this._Message; }
                set { this._Message = value; }
            }
            */

            private KeyData _KeyData;
            public KeyData KeyData
            {
                get { return this._KeyData; }
                set { this._KeyData = value; }
            }

            private string _Username;
            public string Username
            {
                get { return this._Username; }
                set { this._Username = value; }
            }

            private string _Certificate;
            public string Certificate
            {
                get { return this._Certificate; }
                set { this._Certificate = value; }
            }

            private string[] _OneTimePasswords;
            public string[] OneTimePasswords
            {
                get { return this._OneTimePasswords; }
            }
            
            public bool AddOneTimePassword(string otp)
            {
                bool success = false;
                for (int i=0; i<this._OneTimePasswords.Length; i++)
                {
                    if (this._OneTimePasswords[i].Equals(String.Empty))
                    {
                        success = true;
                        this._OneTimePasswords[i] = otp;
                    }
                }
                return success;
            }

            public string GetOneTimePasswordAt(int index)
            {
                return this._OneTimePasswords[index];
            }

            public int GetOneTimePasswordsCount()
            {
                int cnt = 0;
                for (int i = 0; i < this._OneTimePasswords.Length; i++)
                {
                    if (!this._OneTimePasswords[i].Equals(String.Empty))
                    {
                        cnt += 1;
                    }
                }
                return cnt;
            }

            public ProcessData(ProcessAge processAge)
            {
                this._ProcessAgeRef = processAge;
                this._ProcessIdentifier = 0;
                this._KeyData = new KeyData();
                //this._Message = new Message();
                this._Certificate = String.Empty; 
                this._Username = String.Empty;

                this._OneTimePasswords = new string[2];
                for (int i = 0; i < this._OneTimePasswords.Length; i++)
                {
                    this._OneTimePasswords[i] = String.Empty;
                }
            }

            ~ProcessData()
            {
                this._ProcessAgeRef = null;
                this._ProcessIdentifier = 0;
                this._KeyData = null;
                //this._Message = null;
                this._Certificate = String.Empty; 
                this._Username = String.Empty;

                for (int i = 0; i < this._OneTimePasswords.Length; i++)
                {
                    this._OneTimePasswords[i] = String.Empty;
                }
                this._OneTimePasswords = null;
            }

            public bool AddData(OTPPacket otpPacket)
            {
                //OTPPacket dataPacket = Clone<OTPPacket>(otpPacket);
                return MergeData(otpPacket);
            }

            private bool MergeData(OTPPacket otpPacket)
            {
                bool success = true;

                bool keyDataChanged = false;
                KeyData keyData = new KeyData();

                bool certificateChanged = false;
                string certificate = String.Empty;

                bool usernameChanged = false;
                string username = String.Empty;

                bool oneTimePasswordsChanged = false;
                string[] oneTimePasswords = new string[this._OneTimePasswords.Length];
                for (int i = 0; i < this._OneTimePasswords.Length; i++)
                {
                    oneTimePasswords[i] = String.Empty;
                }

                if (success && otpPacket.KeyData.Type != KeyData.TYPE.NONE && this._KeyData.Type == KeyData.TYPE.NONE)
                {
                    keyDataChanged = true;
                    keyData = Clone<KeyData>(otpPacket.KeyData);
                }
                else if (otpPacket.KeyData.Type != KeyData.TYPE.NONE)
                    success = false;

                foreach (Data item in otpPacket.DataItems)
                {
                    if (success && item.Type == Data.TYPE.USERNAME && this._Username == String.Empty)
                    {
                        usernameChanged = true;
                        username = Clone<string>(item.Username);
                    }
                    else if (item.Type == Data.TYPE.USERNAME)
                        success = false;

                    if (success && item.Type == Data.TYPE.CERTIFICATE && this._Certificate == String.Empty)
                    {
                        certificateChanged = true;
                        certificate = Clone<string>(item.Certificate);
                    }
                    else if (item.Type == Data.TYPE.CERTIFICATE)
                        success = false;

                    if (success && item.Type == Data.TYPE.OTP && GetOneTimePasswordsCount() < this._OneTimePasswords.Length)
                    {
                        oneTimePasswordsChanged = true;
                        for (int i = 0; i < this._OneTimePasswords.Length; i++)
                        {
                            if (GetOneTimePasswordAt(i) != String.Empty)
                                oneTimePasswords[i] = GetOneTimePasswordAt(i);
                            else
                                oneTimePasswords[i] = Clone<string>(item.OneTimePassword);
                        }
                    }
                    else if (item.Type == Data.TYPE.OTP)
                        success = false;
                }

                if (success)
                {
                    if (keyDataChanged)
                        this._KeyData = keyData;

                    if (certificateChanged)
                        this._Certificate = certificate;

                    if (usernameChanged)
                        this._Username = username;

                    if (oneTimePasswordsChanged)
                        this._OneTimePasswords = oneTimePasswords;
                }

                return success;
            }

            private static T Clone<T>(T source)
            {
                if (!typeof(T).IsSerializable)
                {
                    throw new ArgumentException("The type must be serializable.", "source");
                }

                if (Object.ReferenceEquals(source, null))
                {
                    return default(T);
                }

                IFormatter formatter = new BinaryFormatter();
                Stream stream = new MemoryStream();
                using (stream)
                {
                    formatter.Serialize(stream, source);
                    stream.Seek(0, SeekOrigin.Begin);
                    return (T)formatter.Deserialize(stream);
                }
            }
        }

        private class ProcessAge
        {
            private int _ProcessStartedAt;
            public int ProcessStartedAt
            {
                get { return this._ProcessStartedAt; }
                set { this._ProcessStartedAt = value; }
            }

            private int _ProcessIdentifier;
            public int ProcessIdentifier
            {
                get { return this._ProcessIdentifier; }
                set { this._ProcessIdentifier = value; }
            }

            public ProcessAge(int pid)
            {
                this._ProcessIdentifier = pid;
                this._ProcessStartedAt = Now();
            }

            public static int Now()
            {
                DateTime epochStart = new DateTime(1970, 1, 1);
                DateTime now = DateTime.Now;
                TimeSpan ts = new TimeSpan(now.Ticks - epochStart.Ticks);
                return (Convert.ToInt32(ts.TotalSeconds));
            }

            public int GetAge()
            {
                return Now() - this._ProcessStartedAt;
            }
        }
    }
}
