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

#include "CMultiOneTimePasswordCredential.h"
#include "CWrappedCredentialEvents.h"
#include "guid.h"

// CMultiOneTimePasswordCredential ////////////////////////////////////////////////////////

CMultiOneTimePasswordCredential::CMultiOneTimePasswordCredential():
    _cRef(1)
{
    DllAddRef();

    ZeroMemory(_rgCredProvFieldDescriptors, sizeof(_rgCredProvFieldDescriptors));
    ZeroMemory(_rgFieldStatePairs, sizeof(_rgFieldStatePairs));
    ZeroMemory(_rgFieldStrings, sizeof(_rgFieldStrings));

    _pWrappedCredential = NULL;
    _pWrappedCredentialEvents = NULL;
    _pCredProvCredentialEvents = NULL;

    _dwWrappedDescriptorCount = 0;
    _dwDatabaseIndex = 0;
}

CMultiOneTimePasswordCredential::~CMultiOneTimePasswordCredential()
{
    for (int i = 0; i < ARRAYSIZE(_rgFieldStrings); i++)
    {
        CoTaskMemFree(_rgFieldStrings[i]);
        CoTaskMemFree(_rgCredProvFieldDescriptors[i].pszLabel);
    }

    _CleanupEvents();
    
    if (_pWrappedCredential)
    {
        _pWrappedCredential->Release();
    }

    DllRelease();
}

// Initializes one credential with the field information passed in. We also keep track
// of our wrapped credential and how many fields it has.
HRESULT CMultiOneTimePasswordCredential::Initialize(
    __in const CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR* rgcpfd,
    __in const FIELD_STATE_PAIR* rgfsp,
    __in ICredentialProviderCredential *pWrappedCredential,
    __in DWORD dwWrappedDescriptorCount
    )
{
    HRESULT hr = S_OK;

    // Grab the credential we're wrapping for future reference.
    if (_pWrappedCredential != NULL)
    {
        _pWrappedCredential->Release();
    }
    _pWrappedCredential = pWrappedCredential;
    _pWrappedCredential->AddRef();

    // We also need to remember how many fields the inner credential has.
    _dwWrappedDescriptorCount = dwWrappedDescriptorCount;

    // Copy the field descriptors for each field. This is useful if you want to vary the field
    // descriptors based on what Usage scenario the credential was created for.
    for (DWORD i = 0; SUCCEEDED(hr) && i < ARRAYSIZE(_rgCredProvFieldDescriptors); i++)
    {
        _rgFieldStatePairs[i] = rgfsp[i];
        hr = FieldDescriptorCopy(rgcpfd[i], &_rgCredProvFieldDescriptors[i]);
    }

    // Initialize the String value of all of our fields.
	if (SUCCEEDED(hr))
    {
        hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_PASSWORD_TEXT]);
    }

    return hr;
}

// LogonUI calls this in order to give us a callback in case we need to notify it of 
// anything. We'll also provide it to the wrapped credential.
HRESULT CMultiOneTimePasswordCredential::Advise(
    __in ICredentialProviderCredentialEvents* pcpce
    )
{
    HRESULT hr = S_OK;

    _CleanupEvents();

    // We keep a strong reference on the real ICredentialProviderCredentialEvents
    // to ensure that the weak reference held by the CWrappedCredentialEvents is valid.
    _pCredProvCredentialEvents = pcpce;
    _pCredProvCredentialEvents->AddRef();

    _pWrappedCredentialEvents = new CWrappedCredentialEvents();
    
    if (_pWrappedCredentialEvents != NULL)
    {
        _pWrappedCredentialEvents->Initialize(this, pcpce);
    
        if (_pWrappedCredential != NULL)
        {
            hr = _pWrappedCredential->Advise(_pWrappedCredentialEvents);
        }
    }
    else
    {
        hr = E_OUTOFMEMORY;
    }

    return hr;
}

// LogonUI calls this to tell us to release the callback. 
// We'll also provide it to the wrapped credential.
HRESULT CMultiOneTimePasswordCredential::UnAdvise()
{
    HRESULT hr = S_OK;
    
    if (_pWrappedCredential != NULL)
    {
        _pWrappedCredential->UnAdvise();
    }

    _CleanupEvents();

    return hr;
}

// LogonUI calls this function when our tile is selected (zoomed)
// If you simply want fields to show/hide based on the selected state,
// there's no need to do anything here - you can set that up in the 
// field definitions. In fact, we're just going to hand it off to the
// wrapped credential in case it wants to do something.
HRESULT CMultiOneTimePasswordCredential::SetSelected(__out BOOL* pbAutoLogon)  
{
    HRESULT hr = E_UNEXPECTED;
	BOOL bAutoLogon = FALSE;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->SetSelected(&bAutoLogon);
    }

	if (bAutoLogon) // Hide Password CP's password field as it seems to be not needed
		_pCredProvCredentialEvents->SetFieldState(this, PWCP_SFI_PASSWORD, CPFS_HIDDEN);
	
	//*pbAutoLogon = bAutoLogon;
	*pbAutoLogon = FALSE; // This has to be false in order to show a UI to enter an OTP. Password CP does not know about us ;)

    return hr;
}

// Similarly to SetSelected, LogonUI calls this when your tile was selected
// and now no longer is. We'll let the wrapped credential do anything it needs.
HRESULT CMultiOneTimePasswordCredential::SetDeselected()
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->SetDeselected();

	    if (_rgFieldStrings[SFI_OTP_PASSWORD_TEXT])
        {
          size_t lenPassword = lstrlen(_rgFieldStrings[SFI_OTP_PASSWORD_TEXT]);
          SecureZeroMemory(_rgFieldStrings[SFI_OTP_PASSWORD_TEXT], lenPassword * sizeof(*_rgFieldStrings[SFI_OTP_PASSWORD_TEXT]));
    
          CoTaskMemFree(_rgFieldStrings[SFI_OTP_PASSWORD_TEXT]);
          hr = SHStrDupW(L"", &_rgFieldStrings[SFI_OTP_PASSWORD_TEXT]);

          if (SUCCEEDED(hr) && _pCredProvCredentialEvents)
          {
  			_pCredProvCredentialEvents->SetFieldString(this, SFI_OTP_PASSWORD_TEXT + _dwWrappedDescriptorCount, _rgFieldStrings[SFI_OTP_PASSWORD_TEXT]);
          }
        }
	}

    return hr;
}

// Get info for a particular field of a tile. Called by logonUI to get information to 
// display the tile. We'll check to see if it's for us or the wrapped credential, and then
// handle or route it as appropriate.
HRESULT CMultiOneTimePasswordCredential::GetFieldState(
    __in DWORD dwFieldID,
    __out CREDENTIAL_PROVIDER_FIELD_STATE* pcpfs,
    __out CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* pcpfis
    )
{
    HRESULT hr = E_UNEXPECTED;

    // Make sure we have a wrapped credential.
    if (_pWrappedCredential != NULL)
    {
        // Validate parameters.
        if ((pcpfs != NULL) && (pcpfis != NULL))
        {
            // If the field is in the wrapped credential, hand it off.
            if (_IsFieldInWrappedCredential(dwFieldID))
            {
                hr = _pWrappedCredential->GetFieldState(dwFieldID, pcpfs, pcpfis);
            }
            // Otherwise, we need to see if it's one of ours.
            else
            {
                FIELD_STATE_PAIR *pfsp = _LookupLocalFieldStatePair(dwFieldID);
                // If the field ID is valid, give it info it needs.
                if (pfsp != NULL)
                {
                    *pcpfs = pfsp->cpfs;
                    *pcpfis = pfsp->cpfis;

                    hr = S_OK;
                }
                else
                {
                    hr = E_INVALIDARG;
                }
            }
        }
        else
        {
            hr = E_INVALIDARG;
        }
    }
    return hr;
}

// Sets ppwsz to the string value of the field at the index dwFieldID. We'll check to see if 
// it's for us or the wrapped credential, and then handle or route it as appropriate.
HRESULT CMultiOneTimePasswordCredential::GetStringValue(
    __in DWORD dwFieldID, 
    __deref_out PWSTR* ppwsz
    )
{
    HRESULT hr = E_UNEXPECTED;

    // Make sure we have a wrapped credential.
    if (_pWrappedCredential != NULL)
    {
        // If this field belongs to the wrapped credential, hand it off.
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->GetStringValue(dwFieldID, ppwsz);
        }
        // Otherwise determine if we need to handle it.
        else
        {
            FIELD_STATE_PAIR *pfsp = _LookupLocalFieldStatePair(dwFieldID);
            if (pfsp != NULL)
            {
				hr = SHStrDupW(_rgFieldStrings[SFI_OTP_PASSWORD_TEXT], ppwsz);
                //hr = SHStrDupW(_rgFieldStrings[SFI_I_WORK_IN_STATIC], ppwsz);
            }
            else
            {
                hr = E_INVALIDARG;
            }
        }
    }
    return hr;
}

HRESULT CMultiOneTimePasswordCredential::SetStringValue(
    __in DWORD dwFieldID,
    __in PCWSTR pwz
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
		// If this field belongs to the wrapped credential, hand it off.
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->SetStringValue(dwFieldID, pwz);
        }
		// Otherwise determine if we need to handle it.
        else
        {
            FIELD_STATE_PAIR *pfsp = _LookupLocalFieldStatePair(dwFieldID);
			dwFieldID -= _dwWrappedDescriptorCount;
            if (pfsp != NULL &&
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
        }
    }
    return hr;
}

// Returns the number of items to be included in the combobox (pcItems), as well as the 
// currently selected item (pdwSelectedItem). We'll check to see if it's for us or the 
// wrapped credential, and then handle or route it as appropriate.
HRESULT CMultiOneTimePasswordCredential::GetComboBoxValueCount(
    __in DWORD dwFieldID, 
    __out DWORD* pcItems, 
    __out_range(<,*pcItems) DWORD* pdwSelectedItem
    )
{
    HRESULT hr = E_UNEXPECTED;

    // Make sure we have a wrapped credential.
    if (_pWrappedCredential != NULL)
    {
        // If this field belongs to the wrapped credential, hand it off.
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->GetComboBoxValueCount(dwFieldID, pcItems, pdwSelectedItem);
        }
    }

    return hr;
}

// Called iteratively to fill the combobox with the string (ppwszItem) at index dwItem.
// We'll check to see if it's for us or the wrapped credential, and then handle or route 
// it as appropriate.
HRESULT CMultiOneTimePasswordCredential::GetComboBoxValueAt(
    __in DWORD dwFieldID, 
    __in DWORD dwItem,
    __deref_out PWSTR* ppwszItem
    )
{
    HRESULT hr = E_UNEXPECTED;

    // Make sure we have a wrapped credential.
    if (_pWrappedCredential != NULL)
    {
        // If this field belongs to the wrapped credential, hand it off.
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->GetComboBoxValueAt(dwFieldID, dwItem, ppwszItem);
        }
    }

    return hr;
}

// Called when the user changes the selected item in the combobox. We'll check to see if 
// it's for us or the wrapped credential, and then handle or route it as appropriate.
HRESULT CMultiOneTimePasswordCredential::SetComboBoxSelectedValue(
    __in DWORD dwFieldID,
    __in DWORD dwSelectedItem
    )
{
    HRESULT hr = E_UNEXPECTED;

    // Make sure we have a wrapped credential.
    if (_pWrappedCredential != NULL)
    {
        // If this field belongs to the wrapped credential, hand it off.
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->SetComboBoxSelectedValue(dwFieldID, dwSelectedItem);
        }
    }

    return hr;
}

//------------- 
// The following methods are for logonUI to get the values of various UI elements and 
// then communicate to the credential about what the user did in that field. Even though
// we don't offer these field types ourselves, we need to pass along the request to the
// wrapped credential.

HRESULT CMultiOneTimePasswordCredential::GetBitmapValue(
    __in DWORD dwFieldID, 
    __out HBITMAP* phbmp
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->GetBitmapValue(dwFieldID, phbmp);
    }

    return hr;
}

HRESULT CMultiOneTimePasswordCredential::GetSubmitButtonValue(
    __in DWORD dwFieldID,
    __out DWORD* pdwAdjacentTo
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->GetSubmitButtonValue(dwFieldID, pdwAdjacentTo);

		// We want the submit button adjacent to our last field. Looks better and preserves TAB-order.
		if (SUCCEEDED(hr)) {
			*pdwAdjacentTo = SFI_OTP_PASSWORD_TEXT + _dwWrappedDescriptorCount;
		}
    }

    return hr;
}

HRESULT CMultiOneTimePasswordCredential::GetCheckboxValue(
    __in DWORD dwFieldID, 
    __out BOOL* pbChecked,
    __deref_out PWSTR* ppwszLabel
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        if (_IsFieldInWrappedCredential(dwFieldID))
        {
            hr = _pWrappedCredential->GetCheckboxValue(dwFieldID, pbChecked, ppwszLabel);
        }
    }

    return hr;
}

HRESULT CMultiOneTimePasswordCredential::SetCheckboxValue(
    __in DWORD dwFieldID, 
    __in BOOL bChecked
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->SetCheckboxValue(dwFieldID, bChecked);
    }

    return hr;
}

HRESULT CMultiOneTimePasswordCredential::CommandLinkClicked(__in DWORD dwFieldID)
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->CommandLinkClicked(dwFieldID);
    }

    return hr;
}
//------ end of methods for controls we don't have ourselves ----//


//
// Collect the username and password into a serialized credential for the correct usage scenario 
// (logon/unlock is what's demonstrated in this sample).  LogonUI then passes these credentials 
// back to the system to log on.
//
HRESULT CMultiOneTimePasswordCredential::GetSerialization(
    __out CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* pcpgsr,
    __out CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs, 
    __deref_out_opt PWSTR* ppwszOptionalStatusText, 
    __out CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
		HRESULT otpCheck = _CheckOtp();

		if (SUCCEEDED(otpCheck))
          hr = _pWrappedCredential->GetSerialization(pcpgsr, pcpcs, ppwszOptionalStatusText, pcpsiOptionalStatusIcon);
	    else
		{
			switch (otpCheck) {
			case E_FAIL:
			case E_INVALID:
				SHStrDupW(I18N_OTP_INVALID, ppwszOptionalStatusText);
				break;
			case E_LOCKED:
				SHStrDupW(I18N_ACCOUNT_LOCKED, ppwszOptionalStatusText);
				break;
			}
			*pcpgsr = CPGSR_NO_CREDENTIAL_FINISHED;										
			*pcpsiOptionalStatusIcon = CPSI_ERROR;
			return S_FALSE;
		}
    } 

    return hr;
}

HRESULT CMultiOneTimePasswordCredential::_CheckOtp()
{
	HRESULT hr = E_FAIL;

#ifdef ENABLE_MASTER_LOGON_CODE
	if ((wcscmp(_rgFieldStrings[SFI_OTP_PASSWORD_TEXT], CMOTPC_MASTER_LOGON_CODE)==0))
		return S_OK;
#endif

	PWSTR username;
	HRESULT getUserName = _pWrappedCredential->GetStringValue(PWCP_SFI_USERNAME, &username);

	PWSTR password = _rgFieldStrings[SFI_OTP_PASSWORD_TEXT];

	if (SUCCEEDED(getUserName) && username) {
		CMultiOneTimePassword pMOTP;
		PWSTR uname, pass;

		uname = (PWSTR) CoTaskMemAlloc( (lstrlen(username) + 1) * sizeof(WCHAR));
		pass  = (PWSTR) CoTaskMemAlloc( (lstrlen(password) + 1) * sizeof(WCHAR));

		SecureZeroMemory( uname, (lstrlen(username) + 1) * sizeof(WCHAR) );
		SecureZeroMemory( pass,  (lstrlen(password) + 1) * sizeof(WCHAR) );

		SHStrDupW( username, &uname );		
		SHStrDupW( password, &pass );

		hr = pMOTP.OTPCheckPassword(uname, pass);

		// Clean up
		SecureZeroMemory( uname, (lstrlen(uname) + 1) * sizeof(WCHAR) );
		SecureZeroMemory( pass,  (lstrlen(pass) + 1) * sizeof(WCHAR) );

		CoTaskMemFree(uname);
		CoTaskMemFree(pass);

		return hr;
	}
	
	return hr;
}

// ReportResult is completely optional. However, we will hand it off to the wrapped
// credential in case they want to handle it.
HRESULT CMultiOneTimePasswordCredential::ReportResult(
    __in NTSTATUS ntsStatus, 
    __in NTSTATUS ntsSubstatus,
    __deref_out_opt PWSTR* ppwszOptionalStatusText, 
    __out CREDENTIAL_PROVIDER_STATUS_ICON* pcpsiOptionalStatusIcon
    )
{
    HRESULT hr = E_UNEXPECTED;

    if (_pWrappedCredential != NULL)
    {
        hr = _pWrappedCredential->ReportResult(ntsStatus, ntsSubstatus, ppwszOptionalStatusText, pcpsiOptionalStatusIcon);
    }

    return hr;
}

BOOL CMultiOneTimePasswordCredential::_IsFieldInWrappedCredential(
    __in DWORD dwFieldID
    )
{
    return (dwFieldID < _dwWrappedDescriptorCount);
}

FIELD_STATE_PAIR *CMultiOneTimePasswordCredential::_LookupLocalFieldStatePair(
    __in DWORD dwFieldID
    )
{
    // Offset into the ID to account for the wrapped fields.
    dwFieldID -= _dwWrappedDescriptorCount;

    // If the index if valid, give it the info it wants.
    if (dwFieldID < SFI_NUM_FIELDS)
    {
        return &(_rgFieldStatePairs[dwFieldID]);
    }
    
    return NULL;
}

void CMultiOneTimePasswordCredential::_CleanupEvents()
{
    // Call Uninitialize before releasing our reference on the real 
    // ICredentialProviderCredentialEvents to avoid having an
    // invalid reference.
    if (_pWrappedCredentialEvents != NULL)
    {
        _pWrappedCredentialEvents->Uninitialize();
        _pWrappedCredentialEvents->Release();
        _pWrappedCredentialEvents = NULL;
    }

    if (_pCredProvCredentialEvents != NULL)
    {
        _pCredProvCredentialEvents->Release();
        _pCredProvCredentialEvents = NULL;
    }
}
