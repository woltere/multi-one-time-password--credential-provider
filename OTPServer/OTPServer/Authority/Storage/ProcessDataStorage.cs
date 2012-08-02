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
using OTPHelpers.XML.OTPPacket;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace OTPServer.Authority.Storage
{
    class ProcessDataStorage
    {
        private static Dictionary<int, ProcessData> __ProcessIdentifierToProcessDataMap = new Dictionary<int,ProcessData>();
        private static LinkedList<ProcessAge> __TimeToProcessIdentifierMap = new LinkedList<ProcessAge>();

        public int ProcessExists(OTPPacket otpPacket)
        {
            if (otpPacket.ProcessIdentifier.ID <= ProcessIdentifier.NONE)
                return 0; // LOOK: The first valid pid is 1!

            return ProcessExists(otpPacket.ProcessIdentifier.ID);
        }

        public int ProcessExists(int pid)
        {
            bool processExists;

            lock (__ProcessIdentifierToProcessDataMap)
                processExists = __ProcessIdentifierToProcessDataMap.ContainsKey(pid);

            if (processExists)
                return pid;

            return 0;
        }

        public int CreateProcess(OTPPacket otpPacket)
        {
            int processIdentifier = ProcessIdentifier.NONE;
            if (otpPacket.ProcessIdentifier.ID != ProcessIdentifier.NONE && ProcessExists(otpPacket) <= ProcessIdentifier.NONE)
                processIdentifier = otpPacket.ProcessIdentifier.ID;     
            else
                processIdentifier = CreateProcessIdentifier();

            ProcessAge processAge = new ProcessAge(processIdentifier);
            ProcessData processData = new ProcessData(processAge);

            lock (__ProcessIdentifierToProcessDataMap)
                __ProcessIdentifierToProcessDataMap.Add(processIdentifier, processData);

            lock (__TimeToProcessIdentifierMap)
                __TimeToProcessIdentifierMap.AddLast(processAge);

            return processIdentifier;
        }

        private int CreateProcessIdentifier()
        {
            return ProcessAge.Now() - 123456789;
        }

        public ProcessData GetProcess(OTPPacket otpPacket)
        {
            if (otpPacket.ProcessIdentifier.ID <= ProcessIdentifier.NONE)
                return null;

            return GetProcess(otpPacket.ProcessIdentifier.ID);
        }

        public ProcessData GetProcess(int pid)
        {
            ProcessData processData;
            bool success;

            lock (__ProcessIdentifierToProcessDataMap)
                success = __ProcessIdentifierToProcessDataMap.TryGetValue(pid, out processData);

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
            if (__TimeToProcessIdentifierMap.Count == 0)
                return null;

            LinkedListNode<ProcessAge> oldestProcess;

            lock (__TimeToProcessIdentifierMap)
                oldestProcess = __TimeToProcessIdentifierMap.First;

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

            lock (__TimeToProcessIdentifierMap)
                __TimeToProcessIdentifierMap.Remove(processData.ProcessAgeRef);

            lock (__ProcessIdentifierToProcessDataMap)
                __ProcessIdentifierToProcessDataMap.Remove(processData.ProcessIdentifier);
        }

        public class ProcessData
        {
            private long _LastAuthedTimestamp = 0; // milliseconds !!!
            public long LastAuthedTimestamp
            {
                get { return this._LastAuthedTimestamp; }
                set { this._LastAuthedTimestamp = value; }
            }

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
                    oneTimePasswords[i] = this._OneTimePasswords[i];
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
                            if (oneTimePasswords[i] == String.Empty)
                            {
                                oneTimePasswords[i] = Clone<string>(item.OneTimePassword);
                                break;
                            }
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

        public class ProcessAge
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

            public static long NowMilli()
            {
                DateTime epochStart = new DateTime(1970, 1, 1);
                DateTime now = DateTime.Now;
                TimeSpan ts = new TimeSpan(now.Ticks - epochStart.Ticks);
                return (Convert.ToInt64(ts.TotalMilliseconds));
            }

            public int GetAge()
            {
                return Now() - this._ProcessStartedAt;
            }
        }
    }
}
