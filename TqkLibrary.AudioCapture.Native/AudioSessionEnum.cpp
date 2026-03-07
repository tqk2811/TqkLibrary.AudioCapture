#include "pch.h"
#include "AudioSessionEnum.h"
#include "COMHelper.h"

using namespace Microsoft::WRL;

void* EnumSessions_Create(const wchar_t* deviceId) {
    if (FAILED(EnsureCoInitialized())) return nullptr;

    ComPtr<IMMDeviceEnumerator> pEnumerator;
    HRESULT hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, IID_PPV_ARGS(&pEnumerator));
    if (FAILED(hr)) return nullptr;

    ComPtr<IMMDevice> pDevice;
    if (deviceId == nullptr) {
        hr = pEnumerator->GetDefaultAudioEndpoint(eRender, eMultimedia, &pDevice);
    } else {
        hr = pEnumerator->GetDevice(deviceId, &pDevice);
    }
    if (FAILED(hr)) return nullptr;

    ComPtr<IAudioSessionManager2> pSessionManager;
    hr = pDevice->Activate(__uuidof(IAudioSessionManager2), CLSCTX_ALL, NULL, &pSessionManager);
    if (FAILED(hr)) return nullptr;

    ComPtr<IAudioSessionEnumerator> pSessionEnumerator;
    hr = pSessionManager->GetSessionEnumerator(&pSessionEnumerator);
    if (FAILED(hr)) return nullptr;

    int count = 0;
    pSessionEnumerator->GetCount(&count);

    auto ctx = new AudioSessionEnumeratorContext();
    for (int i = 0; i < count; i++) {
        ComPtr<IAudioSessionControl> pControl;
        if (SUCCEEDED(pSessionEnumerator->GetSession(i, &pControl))) {
            ComPtr<IAudioSessionControl2> pControl2;
            if (SUCCEEDED(pControl.As(&pControl2))) {
                AudioSessionInfo info;
                
                LPWSTR pwszSessionId = NULL;
                if (SUCCEEDED(pControl2->GetSessionIdentifier(&pwszSessionId))) {
                    info.sessionId = pwszSessionId;
                    CoTaskMemFree(pwszSessionId);
                }

                LPWSTR pwszDisplayName = NULL;
                if (SUCCEEDED(pControl2->GetDisplayName(&pwszDisplayName)) && pwszDisplayName && *pwszDisplayName) {
                    info.displayName = pwszDisplayName;
                    CoTaskMemFree(pwszDisplayName);
                }

                pControl2->GetProcessId(&info.processId);
                pControl2->GetState(&info.state);

                ctx->sessions.push_back(info);
            }
        }
    }

    return ctx;
}

int EnumSessions_GetCount(void* ctx) {
    if (!ctx) return 0;
    return (int)static_cast<AudioSessionEnumeratorContext*>(ctx)->sessions.size();
}

bool EnumSessions_GetSessionId(void* ctx, int idx, wchar_t* buf, int bufLen) {
    if (!ctx) return false;
    auto c = static_cast<AudioSessionEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->sessions.size()) return false;
    wcsncpy_s(buf, bufLen, c->sessions[idx].sessionId.c_str(), _TRUNCATE);
    return true;
}

bool EnumSessions_GetDisplayName(void* ctx, int idx, wchar_t* buf, int bufLen) {
    if (!ctx) return false;
    auto c = static_cast<AudioSessionEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->sessions.size()) return false;
    wcsncpy_s(buf, bufLen, c->sessions[idx].displayName.c_str(), _TRUNCATE);
    return true;
}

int EnumSessions_GetProcessId(void* ctx, int idx) {
    if (!ctx) return 0;
    auto c = static_cast<AudioSessionEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->sessions.size()) return 0;
    return (int)c->sessions[idx].processId;
}

int EnumSessions_GetState(void* ctx, int idx) {
    if (!ctx) return 0;
    auto c = static_cast<AudioSessionEnumeratorContext*>(ctx);
    if (idx < 0 || idx >= (int)c->sessions.size()) return 0;
    return (int)c->sessions[idx].state;
}

void EnumSessions_Free(void** ctx) {
    if (ctx && *ctx) {
        delete static_cast<AudioSessionEnumeratorContext*>(*ctx);
        *ctx = nullptr;
    }
}
