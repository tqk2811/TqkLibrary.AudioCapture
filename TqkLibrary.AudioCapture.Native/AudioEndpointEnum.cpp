#include "pch.h"
#include "AudioEndpointEnum.h"
#include "COMHelper.h"
#include <propsys.h>
#include <functiondiscoverykeys_devpkey.h>

using namespace Microsoft::WRL;

void* EnumEndpoints_Create(int dataFlow) {
    if (FAILED(EnsureCoInitialized())) return nullptr;

    ComPtr<IMMDeviceEnumerator> pEnumerator;
    HRESULT hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, IID_PPV_ARGS(&pEnumerator));
    if (FAILED(hr)) return nullptr;

    ComPtr<IMMDeviceCollection> pCollection;
    hr = pEnumerator->EnumAudioEndpoints((EDataFlow)dataFlow, DEVICE_STATE_ACTIVE | DEVICE_STATE_DISABLED | DEVICE_STATE_UNPLUGGED, &pCollection);
    if (FAILED(hr)) return nullptr;

    UINT count = 0;
    pCollection->GetCount(&count);

    auto ctx = new AudioEndpointEnumeratorContext();
    for (UINT i = 0; i < count; i++) {
        ComPtr<IMMDevice> pDevice;
        if (SUCCEEDED(pCollection->Item(i, &pDevice))) {
            AudioEndpointInfo info;
            
            LPWSTR pwszID = NULL;
            if (SUCCEEDED(pDevice->GetId(&pwszID))) {
                info.deviceId = pwszID;
                CoTaskMemFree(pwszID);
            }

            pDevice->GetState(&info.state);

            ComPtr<IPropertyStore> pProps;
            if (SUCCEEDED(pDevice->OpenPropertyStore(STGM_READ, &pProps))) {
                PROPVARIANT varName;
                PropVariantInit(&varName);
                if (SUCCEEDED(pProps->GetValue(PKEY_Device_FriendlyName, &varName))) {
                    info.friendlyName = varName.pwszVal;
                    PropVariantClear(&varName);
                }
            }
            ctx->endpoints.push_back(info);
        }
    }

    return ctx;
}

int EnumEndpoints_GetCount(void* ctx) {
    if (!ctx) return 0;
    return (int)static_cast<AudioEndpointEnumeratorContext*>(ctx)->endpoints.size();
}

bool EnumEndpoints_GetDeviceId(void* ctx, int idx, wchar_t* buf, int bufLen) {
    if (!ctx) return false;
    auto c = static_cast<AudioEndpointEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->endpoints.size()) return false;
    
    wcsncpy_s(buf, bufLen, c->endpoints[idx].deviceId.c_str(), _TRUNCATE);
    return true;
}

bool EnumEndpoints_GetFriendlyName(void* ctx, int idx, wchar_t* buf, int bufLen) {
    if (!ctx) return false;
    auto c = static_cast<AudioEndpointEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->endpoints.size()) return false;

    wcsncpy_s(buf, bufLen, c->endpoints[idx].friendlyName.c_str(), _TRUNCATE);
    return true;
}

int EnumEndpoints_GetState(void* ctx, int idx) {
    if (!ctx) return 0;
    auto c = static_cast<AudioEndpointEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->endpoints.size()) return 0;
    return (int)c->endpoints[idx].state;
}

void EnumEndpoints_Free(void** ctx) {
    if (ctx && *ctx) {
        delete static_cast<AudioEndpointEnumeratorContext*>(*ctx);
        *ctx = nullptr;
    }
}
