#pragma once
#include "pch.h"

struct AudioEndpointInfo {
    std::wstring deviceId;
    std::wstring friendlyName;
    DWORD state;
};

struct AudioEndpointEnumeratorContext {
    std::vector<AudioEndpointInfo> endpoints;
};

extern "C" {
    __declspec(dllexport) void* EnumEndpoints_Create(int dataFlow);
    __declspec(dllexport) int   EnumEndpoints_GetCount(void* ctx);
    __declspec(dllexport) bool  EnumEndpoints_GetDeviceId(void* ctx, int idx, wchar_t* buf, int bufLen);
    __declspec(dllexport) bool  EnumEndpoints_GetFriendlyName(void* ctx, int idx, wchar_t* buf, int bufLen);
    __declspec(dllexport) int   EnumEndpoints_GetState(void* ctx, int idx);
    __declspec(dllexport) void  EnumEndpoints_Free(void** ctx);
}
