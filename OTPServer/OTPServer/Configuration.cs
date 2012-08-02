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
using Microsoft.Win32;

namespace OTPServer
{
    class Configuration : IDisposable
    {
        private RegistryKey _BaseKey = null;

        private bool _RegistryAvailable;
        public bool RegistryAvailable
        {
            get { return this._RegistryAvailable; }
        }

        private static Configuration __Instance = null;
        public static Configuration Instance
        {
            get
            {
                if (__Instance == null)
                    __Instance = new Configuration();
                return __Instance;
            }
        }

        private Configuration()
        {
            try
            {
                _BaseKey = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("blacksheep").OpenSubKey("OTPServer", true);
                this._RegistryAvailable = true;
            }
            catch (Exception)
            {
                _BaseKey = null;
                this._RegistryAvailable = false;
            }
        }

        ~Configuration()
        {
            _BaseKey = null;
        }

        public string GetStringValue(string valueName)
        {
            try
            {
                return (string)_BaseKey.GetValue(valueName);
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public bool SetValue(string valueName, object value)
        {
            try
            {
                _BaseKey.SetValue(valueName, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _BaseKey.Dispose();
        }
    }
}
