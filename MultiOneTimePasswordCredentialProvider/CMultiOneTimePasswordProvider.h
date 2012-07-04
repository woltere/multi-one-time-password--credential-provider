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

#include <credentialprovider.h>
#include <windows.h>
#include <strsafe.h>

#include "CMultiOneTimePasswordCredential.h"
#include "helpers.h"

class CMultiOneTimePasswordProvider : public ICredentialProvider
{
  public:
    // IUnknown
    IFACEMETHODIMP_(ULONG) AddRef()
    {
        return ++_cRef;
    }
    
    IFACEMETHODIMP_(ULONG) Release()
    {
        LONG cRef = --_cRef;
        if (!cRef)
        {
            delete this;
        }
        return cRef;
    }

    IFACEMETHODIMP QueryInterface(__in REFIID riid, __deref_out void** ppv)
    {
        static const QITAB qit[] =
        {
            QITABENT(CMultiOneTimePasswordProvider, ICredentialProvider), // IID_ICredentialProvider
            {0},
        };
        return QISearch(this, qit, riid, ppv);
    }

  public:
    IFACEMETHODIMP SetUsageScenario(__in CREDENTIAL_PROVIDER_USAGE_SCENARIO cpus, __in DWORD dwFlags);
    IFACEMETHODIMP SetSerialization(__in const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* pcpcs);

    IFACEMETHODIMP Advise(__in ICredentialProviderEvents* pcpe, __in UINT_PTR upAdviseContext);
    IFACEMETHODIMP UnAdvise();

    IFACEMETHODIMP GetFieldDescriptorCount(__out DWORD* pdwCount);
    IFACEMETHODIMP GetFieldDescriptorAt(__in DWORD dwIndex,  __deref_out CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** ppcpfd);

    IFACEMETHODIMP GetCredentialCount(__out DWORD* pdwCount,
                                      __out_range(<,*pdwCount) DWORD* pdwDefault,
                                      __out BOOL* pbAutoLogonWithDefault);
    IFACEMETHODIMP GetCredentialAt(__in DWORD dwIndex, 
                                   __deref_out ICredentialProviderCredential** ppcpc);

    friend HRESULT CSample_CreateInstance(__in REFIID riid, __deref_out void** ppv);

  protected:
    CMultiOneTimePasswordProvider();
    __override ~CMultiOneTimePasswordProvider();
    
  private:
      void _CleanUpAllCredentials();
    
private:
    LONG                _cRef;
    CMultiOneTimePasswordCredential   **_rgpCredentials;          // Pointers to the credentials which will be enumerated by this 
                                                    // Provider.

    ICredentialProvider *_pWrappedProvider;         // Our wrapped provider.
    DWORD               _dwCredentialCount;         // The number of credentials provided by our wrapped provider.
    DWORD               _dwWrappedDescriptorCount;  // The number of fields on each tile of our wrapped provider's 
                                                    // credentials.
    bool                _bEnumeratedSetSerialization;
};
