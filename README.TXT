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

VERSION 1.1.0 CLIENT BETA

The MultiOneTimePassword? Credential Provider (mOTP-CP) aims to improve the overall security 
of the Windows logon process by adding an authenticator. The new authenticator is "Something That You Have" 
and consists of the validation of an One-Time-Password (OTP).

The CP wraps the standard Password Credential Provider, so that the standard authentication 
("Something That You Know", username and password) and new authentication ("Something That You Have", OTP) 
steps are fully indepent from each other, but both of them may be required.

You have to add an OTPServer instance to your network, which is reachable by your clients.
(Download OTPServer: http://code.google.com/p/multi-one-time-password--credential-provider)


Requirements:

  * knowledge of what you are doing and how One-Time-Passwords (OATH/HOTP, OATH/TOTP, ...) work (!)

  * Windows Vista (not tested, yet), Windows 7 (tested) or Windows Server 2008 R2 (Semi-tested)

  * an OTPServer instance running in your network or somewhere else, but reachable from your machine

  * configured user account(s) in OTPServer

  * configured device to (e.g. Smartphone) generate One-Time-Passwords for you

  * mOTP-CP will add a new virtual user account to your system called "Resync OTP". 
    In case that your can not logon to your account using OTPs, you can resynchronize them before logging in again.


Howto:

  1. Run Setup.exe and install MultiOneTimePassword Credential Provider

      - Set up your OTPServer's IP or DNS-name

      - if you set Standard-Provider to true, WinLogon will hide the standard Password Credential Provider

  5. Lock your workstation (WIN + L) or log your session off to see your beautiful new OTP logon screen


Account locking / OTPs out of sync:

mOTP-CP will add a virtual user account to your system called "Resync OTP". In case that you can not logon to your 
account using OTPs, they may be out of sync. You may resynchronize them before logging in again.

By default multiotp.exe from SysCo? locks accounts after six failed authorization requests. 
You may resychronize your OTPs and logon again.

mOTP-CP will add a new virtual user account to your system called "Resync OTP". 
In case that your can not logon to your account using OTPs, you can resynchronize them before logging in again.



CHANGELOG:

1.1.0:
  - support for server-client architecture
  - domain name part is removed from username (thanks to Matt Hanley)
  - provider filter improved (thanks to Matt Hanley)

1.0.1:
  - support for users that do not have a Windows account password set. An OTP will still be asked for.