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

#include "CallExternalMultiOTPExe.h"

DWORD __CallMultiOTPExe(int argc, PWSTR *argv[])
{
	DWORD exitCode = (DWORD)-1;

	const int SIZE = 200;
	PWSTR dir       = CEMOTP_DIR;
	WCHAR cmd[SIZE] = CEMOTP_EXE;
	WCHAR app[SIZE] = L"";

	// Append the EXE to the path
	wcscat_s( app, SIZE, dir ); wcscat_s( app, SIZE, DIR_SEP ); wcscat_s( app, SIZE, cmd );

	if (LOGGING) 
	{
		wcscat_s( cmd, SIZE, PARAM_SEP );
		wcscat_s( cmd, SIZE, CEMOTP_PARAM_LOG );
	}

	for (int i=0; i < argc; i++)
	{
		wcscat_s( cmd, SIZE, PARAM_SEP );
		wcscat_s( cmd, SIZE, *argv[i] );
	}

	STARTUPINFO si;
	PROCESS_INFORMATION pi;

	SecureZeroMemory( &si, sizeof(si) );
	SecureZeroMemory( &pi, sizeof(pi) );

	si.cb = sizeof(si);

	if( ::CreateProcessW( app, cmd, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, dir, &si, &pi ) ) 
	{
		WaitForSingleObject( pi.hProcess, (3 * 1000) );
		GetExitCodeProcess( pi.hProcess, &exitCode );

		CloseHandle( pi.hProcess );
		CloseHandle( pi.hThread );
	}

	// Clean up
	SecureZeroMemory( &si, sizeof(si) );
	SecureZeroMemory( &pi, sizeof(pi) );

	SecureZeroMemory((void*)app, SIZE);
	SecureZeroMemory((void*)cmd, SIZE);

	for (int i=0; i<SIZE; i++)
		CoTaskMemFree((void*)app[i]);
	for (int i=0; i<SIZE; i++)
		CoTaskMemFree((void*)cmd[i]);

	return exitCode;
}
