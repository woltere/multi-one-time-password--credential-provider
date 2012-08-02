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

#include "CMultiOneTimePasswordImpl.h"


CMultiOneTimePassword::CMultiOneTimePassword(void)
	: i(0)
{
}

CMultiOneTimePassword::~CMultiOneTimePassword(void)
{
}

HRESULT __stdcall CMultiOneTimePassword::OTPCheckPassword(PWSTR username, PWSTR otp)
{
	const int argc = 4;	
	PWSTR *argv[argc];

	PWSTR server = OTPCLIENT_SERVER;
	PWSTR cmd    = OTPCLIENT_PARAM_VERIFY;

	argv[0] = &server;
	argv[1] = &cmd;
	argv[2] = &username;
	argv[3] = &otp;

	HRESULT hr = _ExitCodeToHRESULT( __CallOTPClientExe(argc, argv) );

	for (int i=0; i<argc; i++)
	{
		argv[i] = NULL;
		CoTaskMemFree(argv[i]);
	}

	return hr;
}

HRESULT __stdcall CMultiOneTimePassword::OTPResync(PWSTR username, PWSTR otp1, PWSTR otp2)
{
	const int argc = 5;	
	PWSTR *argv[argc];

	PWSTR server = OTPCLIENT_SERVER;
	PWSTR cmd    = OTPCLIENT_PARAM_RESYNC;

	argv[0] = &server;
	argv[1] = &cmd;
	argv[2] = &username;
	argv[3] = &otp1;
	argv[4] = &otp2;
	
	HRESULT hr = _ExitCodeToHRESULT( __CallOTPClientExe(argc, argv) );

	for (int i=0; i<argc; i++)
	{
		argv[i] = NULL;
		CoTaskMemFree(argv[i]);
	}

	return hr;
}


HRESULT CMultiOneTimePassword::_ExitCodeToHRESULT(DWORD exitCode)
{
	switch (exitCode) {
		case OTPCLIENT_EXIT_SUCCESS:
			return S_OK;
			break;
		case OTPCLIENT_EXIT_ERROR:
		case OTPCLIENT_EXIT_UNKNOWN_ERROR:
			return E_INVALID;
			break;
		default:
			return E_FAIL;
			break;
	}
}
