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
	const int argc = 2;	
	PWSTR *argv[argc];

	argv[0] = &username;
	argv[1] = &otp;

	HRESULT hr = _MultiOTPExitCodeToHRESULT( __CallMultiOTPExe(argc, argv) );

	for (int i=0; i<argc; i++)
	{
		argv[i] = NULL;
		CoTaskMemFree(argv[i]);
	}

	return hr;
}

HRESULT __stdcall CMultiOneTimePassword::OTPResync(PWSTR username, PWSTR otp1, PWSTR otp2)
{
	const int argc = 4;	
	PWSTR *argv[argc];

	PWSTR resync = CEMOTP_PARAM_RESYNC;

	argv[0] = &resync;
	argv[1] = &username;
	argv[2] = &otp1;
	argv[3] = &otp2;
	
	HRESULT hr = _MultiOTPExitCodeToHRESULT( __CallMultiOTPExe(argc, argv) );

	for (int i=0; i<argc; i++)
	{
		argv[i] = NULL;
		CoTaskMemFree(argv[i]);
	}

	return hr;
}


HRESULT CMultiOneTimePassword::_MultiOTPExitCodeToHRESULT(DWORD exitCode)
{
	switch (exitCode) {
		case CEMOTP_EXIT_SUCCESS:
		case CEMOTP_EXIT_RESYNC_OK:
			return S_OK;
			break;
		case CEMOTP_EXIT_ERROR_LOCKED:
			return E_LOCKED;
			break;
		case CEMOTP_EXIT_ERROR_AUTH:
			return E_INVALID;
			break;
		default:
			return E_FAIL;
			break;
	}
}
