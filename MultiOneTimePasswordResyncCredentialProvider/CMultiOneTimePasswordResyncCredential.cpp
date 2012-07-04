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

#ifndef WIN32_NO_STATUS
#include <ntstatus.h>
#define WIN32_NO_STATUS
#endif
#include <unknwn.h>
#include "CMultiOneTimePasswordResyncCredential.h"
#include "guid.h"

// CMultiOneTimePasswordResyncCredential ////////////////////////////////////////////////////////

CMultiOneTimePasswordResyncCredential::CMultiOneTimePasswordResyncCredential():
    _cRef(1),
    _pCredProvCredentialEvents(NULL)
{
    DllAddRef();

    ZeroMemory(_rgCredProvFieldDescriptors, sizeof(_rgCredProvFieldDescriptors));
    ZeroMemory(_rgFieldStatePairs, sizeof(_rgFieldStatePairs));
    ZeroMemory(_rgFieldStrings, sizeof(_rgFieldStrings));
}

CMultiOneTimePasswordResyncCredential::~CMultiOneTimePasswordResyncCredential()
{
	if (_rgFieldStrings[SFI_USERNAME])
    {
        // CoTaskMemFree (below) deals with NULL, but StringCchLength does not.
        size_t lenUsername = lstrlen(_rgFieldStrings[SFI_USERNAME]);
        SecureZeroMemory(_rgFieldStrings[SFI_USERNAME], lenUsername * sizeof(*_rgFieldStrings[SFI_USERNAME]));
    }
    if (_rgFieldStrings[SFI_OTP_1])
    {
        // CoTaskMemFree (below) deals with NULL, but StringCchLength does not.
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_1]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_1], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_1]));
    }
	if (_rgFieldStrings[SFI_OTP_2])
    {
        // CoTaskMemFree (below) deals with NULL, but StringCchLength does not.
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_2]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_2], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_2]));
    }
    for (int i = 0; i < ARRAYSIZE(_rgFieldStrings); i++)
    {
        CoTaskMemFree(_rgFieldStrings[i]);
        CoTaskMemFree(_rgCredProvFieldDescriptors[i].pszLabel);
    }

    DllRelease();
}

// Initializes one credential with the field information passed in.
// Set the value of the SFI_USERNAME field to pwzUsername.
// Optionally takes a password for the SetSerialization case.
HRESULT CMultiOneTimePasswordResyncCredential::Initialize(
    __in const CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR* rgcpfd,
    __in const FIELD_STATE_PAIR* rgfsp
    )
{
    HRESULT hr = S_OK;

    // Copy the field descriptors for each field. This is useful if you want to vary the 
    // field descriptors based on what Usage scenario the credential was created for.
    for (DWORD i = 0; SUCCEEDED(hr) && i < ARRAYSIZE(_rgCredProvFieldDescriptors); i++)
    {
        _rgFieldStatePairs[i] = rgfsp[i];
        hr = FieldDescriptorCopy(rgcpfd[i], &_rgCredProvFieldDescriptors[i]);
    }

    // Initialize the String values of all the fields.
	if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(I18N_RESYNC_PROVIDER_NAME, &_rgFieldStrings[SFI_PROVNAME]);
    }
    if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_USERNAME]);
    }
    if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_1]);
    }
	if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_2]);
    }
    if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(L"Submit", &_rgFieldStrings[SFI_SUBMIT_BUTTON]);
    }

    return S_OK;
}

// LogonUI calls this in order to give us a callback in case we need to notify it of anything.
HRESULT CMultiOneTimePasswordResyncCredential::Advise(
    __in ICredentialProviderCredentialEvents* pcpce
    )
{
    if (_pCredProvCredentialEvents != NULL)
    {
        _pCredProvCredentialEvents->Release();
    }
    _pCredProvCredentialEvents = pcpce;
    _pCredProvCredentialEvents->AddRef();
    return S_OK;
}

// LogonUI calls this to tell us to release the callback.
HRESULT CMultiOneTimePasswordResyncCredential::UnAdvise()
{
    if (_pCredProvCredentialEvents)
    {
        _pCredProvCredentialEvents->Release();
    }
    _pCredProvCredentialEvents = NULL;
    return S_OK;
}

// LogonUI calls this function when our tile is selected (zoomed).
// If you simply want fields to show/hide based on the selected state,
// there's no need to do anything here - you can set that up in the 
// field definitions.  But if you want to do something
// more complicated, like change the contents of a field when the tile is
// selected, you would do it here.
HRESULT CMultiOneTimePasswordResyncCredential::SetSelected(__out BOOL* pbAutoLogon)  
{
    *pbAutoLogon = FALSE;  

	_pCredProvCredentialEvents->SetFieldInteractiveState(this, SFI_USERNAME, CPFIS_FOCUSED);
	_pCredProvCredentialEvents->SetFieldInteractiveState(this, SFI_OTP_1, CPFIS_NONE);

    return S_OK;
}

// Similarly to SetSelected, LogonUI calls this when your tile was selected
// and now no longer is. The most common thing to do here (which we do below)
// is to clear out the password field.
HRESULT CMultiOneTimePasswordResyncCredential::SetDeselected()
{
    HRESULT hr = S_OK;
	if (_rgFieldStrings[SFI_USERNAME])
    {
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_USERNAME]);
        SecureZeroMemory(_rgFieldStrings[SFI_USERNAME], lenPassword * sizeof(*_rgFieldStrings[SFI_USERNAME]));
    
        CoTaskMemFree(_rgFieldStrings[SFI_USERNAME]);
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_USERNAME]);
        if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_USERNAME, _rgFieldStrings[SFI_USERNAME]);
        }
    }
	if (_rgFieldStrings[SFI_OTP_1])
    {
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_1]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_1], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_1]));
    
        CoTaskMemFree(_rgFieldStrings[SFI_OTP_1]);
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_1]);
        if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_OTP_1, _rgFieldStrings[SFI_OTP_1]);
        }
    }
    if (_rgFieldStrings[SFI_OTP_2])
    {
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_2]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_2], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_2]));
    
        CoTaskMemFree(_rgFieldStrings[SFI_OTP_2]);
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_2]);
        if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_OTP_2, _rgFieldStrings[SFI_OTP_2]);
        }
    }

    return hr;
}

// Gets info for a particular field of a tile. Called by logonUI to get information to 
// display the tile.
HRESULT CMultiOneTimePasswordResyncCredential::GetFieldState(
    __in DWORD dwFieldID,
    __out CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
    __out CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis
    )
{
    HRESULT hr;

    // Validate paramters.
    if ((dwFieldID < ARRAYSIZE(_rgFieldStatePairs)) && pcpfs && pcpfis)
    {
        *pcpfs = _rgFieldStatePairs[dwFieldID].cpfs;
        *pcpfis = _rgFieldStatePairs[dwFieldID].cpfis;

        hr = S_OK;
    }
    else
    {
        hr = E_INVALIDARG;
    }
    return hr;
}

// Sets ppwsz to the string value of the field at the index dwFieldID.
HRESULT CMultiOneTimePasswordResyncCredential::GetStringValue(
    __in DWORD dwFieldID, 
    __deref_out PWSTR* ppwsz
    )
{
    HRESULT hr;

    // Check to make sure dwFieldID is a legitimate index.
    if (dwFieldID < ARRAYSIZE(_rgCredProvFieldDescriptors) && ppwsz) 
    {
        // Make a copy of the string and return that. The caller
        // is responsible for freeing it.
        hr = SHStrDupW(_rgFieldStrings[dwFieldID], ppwsz);
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

// Gets the image to show in the user tile.
HRESULT CMultiOneTimePasswordResyncCredential::GetBitmapValue(
    __in DWORD dwFieldID, 
    __out HBITMAP* phbmp
    )
{
    HRESULT hr;
    if ((SFI_TILEIMAGE == dwFieldID) && phbmp)
    {
        HBITMAP hbmp = LoadBitmap(HINST_THISDLL, MAKEINTRESOURCE(IDB_TILE_IMAGE));
        if (hbmp != NULL)
        {
            hr = S_OK;
            *phbmp = hbmp;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

// Sets pdwAdjacentTo to the index of the field the submit button should be 
// adjacent to. We recommend that the submit button is placed next to the last
// field which the user is required to enter information in. Optional fields
// should be below the submit button.
HRESULT CMultiOneTimePasswordResyncCredential::GetSubmitButtonValue(
    __in DWORD dwFieldID,
    __out DWORD* pdwAdjacentTo
    )
{
    HRESULT hr;

    // Validate parameters.
    if ((SFI_SUBMIT_BUTTON == dwFieldID) && pdwAdjacentTo)
    {
        // pdwAdjacentTo is a pointer to the fieldID you want the submit button to appear next to.
        *pdwAdjacentTo = SFI_OTP_2;
        hr = S_OK;
    }
    else
    {
        hr = E_INVALIDARG;
    }
    return hr;
}

// Sets the value of a field which can accept a string as a value.
// This is called on each keystroke when a user types into an edit field.
HRESULT CMultiOneTimePasswordResyncCredential::SetStringValue(
    __in DWORD dwFieldID, 
    __in PCWSTR pwz      
    )
{
    HRESULT hr;

    // Validate parameters.
    if (dwFieldID < ARRAYSIZE(_rgCredProvFieldDescriptors) && 
       (CPFT_EDIT_TEXT == _rgCredProvFieldDescriptors[dwFieldID].cpft || 
        CPFT_PASSWORD_TEXT == _rgCredProvFieldDescriptors[dwFieldID].cpft)) 
    {
        PWSTR* ppwszStored = &_rgFieldStrings[dwFieldID];
        CoTaskMemFree(*ppwszStored);
        hr = SHStrDupW(pwz, ppwszStored);
    }
    else
    {
        hr = E_INVALIDARG;
    }

    return hr;
}

//------------- 
// The following methods are for logonUI to get the values of various UI elements and then communicate
// to the credential about what the user did in that field.  However, these methods are not implemented
// because our tile doesn't contain these types of UI elements
HRESULT CMultiOneTimePasswordResyncCredential::GetCheckboxValue(
    __in DWORD dwFieldID, 
    __out BOOL* pbChecked,
    __deref_out PWSTR* ppwszLabel
    )
{
    UNREFERENCED_PARAMETER(dwFieldID);
    UNREFERENCED_PARAMETER(pbChecked);
    UNREFERENCED_PARAMETER(ppwszLabel);

    return E_NOTIMPL;
}

HRESULT CMultiOneTimePasswordResyncCredential::GetComboBoxValueCount(
    __in DWORD dwFieldID, 
    __out DWORD* pcItems, 
    __out_range(<,*pcItems) DWORD* pdwSelectedItem
    )
{
    UNREFERENCED_PARAMETER(dwFieldID);
    UNREFERENCED_PARAMETER(pcItems);
    UNREFERENCED_PARAMETER(pdwSelectedItem);
    return E_NOTIMPL;
}

HRESULT CMultiOneTimePasswordResyncCredential::GetComboBoxValueAt(
    __in DWORD dwFieldID, 
    __in DWORD dwItem,
    __deref_out PWSTR* ppwszItem
    )
{
    UNREFERENCED_PARAMETER(dwFieldID);
    UNREFERENCED_PARAMETER(dwItem);
    UNREFERENCED_PARAMETER(ppwszItem);
    return E_NOTIMPL;
}

HRESULT CMultiOneTimePasswordResyncCredential::SetCheckboxValue(
    __in DWORD dwFieldID, 
    __in BOOL bChecked
    )
{
    UNREFERENCED_PARAMETER(dwFieldID);
    UNREFERENCED_PARAMETER(bChecked);

    return E_NOTIMPL;
}

HRESULT CMultiOneTimePasswordResyncCredential::SetComboBoxSelectedValue(
    __in DWORD dwFieldId,
    __in DWORD dwSelectedItem
    )
{
    UNREFERENCED_PARAMETER(dwFieldId);
    UNREFERENCED_PARAMETER(dwSelectedItem);
    return E_NOTIMPL;
}

HRESULT CMultiOneTimePasswordResyncCredential::CommandLinkClicked(__in DWORD dwFieldID)
{
    UNREFERENCED_PARAMETER(dwFieldID);
    return E_NOTIMPL;
}
//------ end of methods for controls we don't have in our tile ----//

// Collect the username and password into a serialized credential for the correct usage scenario 
// (logon/unlock is what's demonstrated in this sample).  LogonUI then passes these credentials 
// back to the system to log on.
HRESULT CMultiOneTimePasswordResyncCredential::GetSerialization(
    __out CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
    __out CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs, 
    __deref_out_opt PWSTR* ppwszOptionalStatusText, 
    __out CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    )
{
	UNREFERENCED_PARAMETER(pcpcs);
    UNREFERENCED_PARAMETER(ppwszOptionalStatusText);
    UNREFERENCED_PARAMETER(pcpsiOptionalStatusIcon);

	CMultiOneTimePassword pMOTP;
	PWSTR uname, otp1, otp2;

	uname = (PWSTR) CoTaskMemAlloc( (lstrlen(_rgFieldStrings[SFI_USERNAME]) + 1) * sizeof(WCHAR) );
	otp1  = (PWSTR) CoTaskMemAlloc( (lstrlen(_rgFieldStrings[SFI_OTP_1])    + 1) * sizeof(WCHAR) );
	otp2  = (PWSTR) CoTaskMemAlloc( (lstrlen(_rgFieldStrings[SFI_OTP_2])    + 1) * sizeof(WCHAR) );

	SecureZeroMemory( uname, (lstrlen(_rgFieldStrings[SFI_USERNAME]) + 1) * sizeof(WCHAR) );
	SecureZeroMemory( otp1,  (lstrlen(_rgFieldStrings[SFI_OTP_1])    + 1) * sizeof(WCHAR) );
	SecureZeroMemory( otp2,  (lstrlen(_rgFieldStrings[SFI_OTP_2])    + 1) * sizeof(WCHAR) );

	SHStrDupW( _rgFieldStrings[SFI_USERNAME], &uname );	
	SHStrDupW( _rgFieldStrings[SFI_OTP_1],    &otp1  );	
	SHStrDupW( _rgFieldStrings[SFI_OTP_2],    &otp2  );

	HRESULT hr = pMOTP.OTPResync(uname, otp1, otp2);

	// Clean up
	SecureZeroMemory( uname, (lstrlen(uname) + 1) * sizeof(WCHAR) );
	SecureZeroMemory( otp1,  (lstrlen(otp1)  + 1) * sizeof(WCHAR) );
	SecureZeroMemory( otp1,  (lstrlen(otp2)  + 1) * sizeof(WCHAR) );

	CoTaskMemFree(uname);
	CoTaskMemFree(otp1);
	CoTaskMemFree(otp2);

	if (SUCCEEDED(hr)) 
	{
		SHStrDupW(I18N_RESYNC_SUCCEEDED, ppwszOptionalStatusText);
		*pcpsiOptionalStatusIcon = CPSI_SUCCESS;
		*pcpgsr = CPGSR_NO_CREDENTIAL_FINISHED;
	}
	else 
	{
		SHStrDupW(I18N_RESYNC_FAILED, ppwszOptionalStatusText);
		*pcpsiOptionalStatusIcon = CPSI_ERROR;
		*pcpgsr = CPGSR_NO_CREDENTIAL_NOT_FINISHED;
	}

	if (_rgFieldStrings[SFI_OTP_1])
    {
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_1]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_1], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_1]));
    
        CoTaskMemFree(_rgFieldStrings[SFI_OTP_1]);
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_1]);
        if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_OTP_1, _rgFieldStrings[SFI_OTP_1]);

			_pCredProvCredentialEvents->SetFieldInteractiveState(this, SFI_USERNAME, CPFIS_NONE);
			_pCredProvCredentialEvents->SetFieldInteractiveState(this, SFI_OTP_1, CPFIS_FOCUSED);
        }
    }
    if (_rgFieldStrings[SFI_OTP_2])
    {
        size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_2]);
        SecureZeroMemory(_rgFieldStrings[SFI_OTP_2], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_2]));
    
        CoTaskMemFree(_rgFieldStrings[SFI_OTP_2]);
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_2]);
        if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_OTP_2, _rgFieldStrings[SFI_OTP_2]);
        }
    }

    return S_FALSE;
}

/*
struct REPORT_RESULT_STATUS_INFO
{
    NTSTATUS ntsStatus;
    NTSTATUS ntsSubstatus;
    PWSTR     pwzMessage;
    CREDENTIAL_PROVIDER_STATUS_ICON cpsi;
};


static const REPORT_RESULT_STATUS_INFO s_rgLogonStatusInfo[] =
{
    { STATUS_LOGON_FAILURE, STATUS_SUCCESS, L"Incorrect password or username.", CPSI_ERROR, },
    { STATUS_ACCOUNT_RESTRICTION, STATUS_ACCOUNT_DISABLED, L"The account is disabled.", CPSI_WARNING },
};
//*/

// ReportResult is completely optional.  Its purpose is to allow a credential to customize the string
// and the icon displayed in the case of a logon failure.  For example, we have chosen to 
// customize the error shown in the case of bad username/password and in the case of the account
// being disabled.
HRESULT CMultiOneTimePasswordResyncCredential::ReportResult(
    __in NTSTATUS ntsStatus, 
    __in NTSTATUS ntsSubstatus,
    __deref_out_opt PWSTR* ppwszOptionalStatusText, 
    __out CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    )
{
	/*
    *ppwszOptionalStatusText = NULL;
    *pcpsiOptionalStatusIcon = CPSI_NONE;

    DWORD dwStatusInfo = (DWORD)-1;

    // Look for a match on status and substatus.
    for (DWORD i = 0; i < ARRAYSIZE(s_rgLogonStatusInfo); i++)
    {
        if (s_rgLogonStatusInfo[i].ntsStatus == ntsStatus && s_rgLogonStatusInfo[i].ntsSubstatus == ntsSubstatus)
        {
            dwStatusInfo = i;
            break;
        }
    }

    if ((DWORD)-1 != dwStatusInfo)
    {
        if (SUCCEEDED(SHStrDupW(s_rgLogonStatusInfo[dwStatusInfo].pwzMessage, ppwszOptionalStatusText)))
        {
            *pcpsiOptionalStatusIcon = s_rgLogonStatusInfo[dwStatusInfo].cpsi;
        }
    }

    // If we failed the logon, try to erase the password field.
    if (!SUCCEEDED(HRESULT_FROM_NT(ntsStatus)))
    {
        if (_pCredProvCredentialEvents)
        {
            _pCredProvCredentialEvents->SetFieldString(this, SFI_PASSWORD, L"");
        }
    }

    // Since NULL is a valid value for *ppwszOptionalStatusText and *pcpsiOptionalStatusIcon
    // this function can't fail.
    return S_OK;
	*/

	UNREFERENCED_PARAMETER(ntsStatus);
    UNREFERENCED_PARAMETER(ntsSubstatus);
	UNREFERENCED_PARAMETER(ppwszOptionalStatusText);
    UNREFERENCED_PARAMETER(pcpsiOptionalStatusIcon);
	return E_NOTIMPL;
}

