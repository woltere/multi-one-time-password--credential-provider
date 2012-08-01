using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;

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
