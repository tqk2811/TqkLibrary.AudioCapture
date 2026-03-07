#pragma once
#include "pch.h"

inline HRESULT EnsureCoInitialized()
{
    HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
    if (hr == RPC_E_CHANGED_MODE)
    {
        return S_OK;
    }
    return hr;
}

inline void CoUninitializeIfStarted(HRESULT hr)
{
    if (SUCCEEDED(hr) && hr != S_FALSE)
    {
        CoUninitialize();
    }
}
