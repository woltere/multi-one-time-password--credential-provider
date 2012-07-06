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

VERSION 1.0.0

The MultiOneTimePassword Credential Provider (mOTP-CP) improves 
the overall security of the Windows logon process by adding an 
authorization step. The authorization includes the validation of 
a One-Time-Password (OTP).

The CP wraps the standard Password Credential Provider, so that 
the authentication (username and password) and authorization (OTP) 
steps are fully indepent from each other.

Currently, the mOTP-CP requires multiotp to be available on 
the target machine. The multiotp.exe has to be placed in C:\multiotp\multiotp.exe. 
You can download multiotp at http://www.multiotp.net .

You have to add an account to multiotp first, before installing mOTP-CP.

This behaviour can be changed by providing another MultiOneTimePassword.dll. 
You can develop your own based on the specifications given in 
the Visual Studio poject file in the repository.


Requirements:

  * knowledge of what you are doing and how One-Time-Passwords (OATH/HOTP, OATH/TOTP, ...) work (!)

  * Windows Vista (not tested, yet) or Windows 7 (tested)

  * multiotp (http://www.multiotp.net) in C:\multiotp\multiotp.exe

  * configured user account(s) in multiotp (use your Windows account name!)

  * configured device to (e.g. Smartphone) generate One-Time-Passwords for you

  * mOTP-CP will add a new virtual user account to your system called "Resync OTP". 
    In case that your can not logon to your account using OTPs, you can resynchronize them before logging in again.


Howto:

  1. Download multiotp (www.multiotp.net)

  2. Extract multiotp.exe and copy it to C:\multiotp\

  3. For EACH user that you want to logon later you have to:

      - create a user with multiopt (see multiotp README) using the Windows account name as username
      - configure a device (e.g. a Smartphone) to generate OTPs for that user

      - TEST ON COMMANDLINE AND MAKE SURE YOURE TOKENS ARE ACCEPTED !!!

  4. Run Setup.exe and install MultiOneTimePassword Credential Provider

      - if you set Standard-Provider to true, WinLogon will hide the standard Password Credential Provider

  5. Lock your workstation (WIN + L) or log your session off to see your beautiful new OTP logon screen


Account locking / OTPs out of sync:

mOTP-CP will add a virtual user account to your system called "Resync OTP". In case that you can not logon to your account using OTPs, they may be out of sync. You may resynchronize them before logging in again.

By default multiotp.exe from SysCo? locks accounts after six failed authorization requests. You may resychronize your OTPs and logon again.

mOTP-CP will add a new virtual user account to your system called "Resync OTP". 
In case that your can not logon to your account using OTPs, you can resynchronize them before logging in again.



CHANGELOG:

1.0.1:
  - support for users that do not have a Windows account password set. An OTP will still be asked for.