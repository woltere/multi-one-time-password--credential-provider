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

VERSION 1.0.0 SERVER BETA


The OTPServer uses a SSL/TLS socket to verify or resnychronize one-time-passwords
that are managed by MultiOTP from SysCo. The communication is realized through the
OTPPacket protocol.

The MultiOneTimePassword Credential Provider is able to use the OTPServer as a
client.


See our project page: http://code.google.com/p/multi-one-time-password--credential-provider/


REQUIREMENTS:

  - Windows Vista (untested), Windows 7 (tested x64) or Windows Server 2008 R2 (untested)

  - A certificate and private key trusted by all clients of the OTPServer to authenticate
    the server while SSL-handshaking and to sign the communication

  - MultiOTP from SysCo

How-To:

  - Install MultiOTP from Sysco

  - Create user accounts in MultiOTP (see our Wiki)

  - Install a domain-trusted certificate and private key to your windows key store
    (Wiki page coming soon)

  - Install OTPServer

  - Start OTPServer Configuration Utility

  - Select your installed certificate ("Choose ...")

  - "Save configuration"

  - Start the OTPServer service


