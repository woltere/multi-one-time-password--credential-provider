﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace OTPServer
{
    partial class OneTimePasswordAgent : ServiceBase
    {
        public OneTimePasswordAgent()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Hier Code hinzufügen, um den Dienst zu starten.
        }

        protected override void OnStop()
        {
            // TODO: Hier Code zum Ausführen erforderlicher Löschvorgänge zum Anhalten des Dienstes hinzufügen.
        }
    }
}
