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

#pragma once

#include <Windows.h>

//#define LOGGING true

#define DIR_SEP   L"\\"
#define PARAM_SEP L" "

#define OTPCLIENT_EXE L"OTPClient.exe"

// TODO: Load from config
#define OTPCLIENT_SERVER L"0.0.0.0"

#define OTPCLIENT_PARAM_VERIFY L"verify"
#define OTPCLIENT_PARAM_RESYNC L"resync"

#define OTPCLIENT_EXIT_SUCCESS        0
#define OTPCLIENT_EXIT_ERROR          1
#define OTPCLIENT_EXIT_UNKNOWN_ERROR -1

DWORD __CallOTPClientExe(int argc, PWSTR *argv[]);
