#pragma once
#include "pch.h"
#include <audiopolicy.h>

struct AudioSessionInfo {
    std::wstring sessionId;
    std::wstring displayName;
    DWORD processId;
    AudioSessionState state;
};

struct AudioSessionEnumeratorContext {
    std::vector<AudioSessionInfo> sessions;
};

extern "C" {
    __declspec(dllexport) void* EnumSessions_Create(const wchar_t* deviceId);
    __declspec(dllexport) int   EnumSessions_GetCount(void* ctx);
    __declspec(dllexport) bool  EnumSessions_GetSessionId(void* ctx, int idx, wchar_t* buf, int bufLen);
    __declspec(dllexport) bool  EnumSessions_GetDisplayName(void* ctx, int idx, wchar_t* buf, int bufLen);
    __declspec(dllexport) int   EnumSessions_GetProcessId(void* ctx, int idx);
    __declspec(dllexport) int   EnumSessions_GetState(void* ctx, int idx);
    __declspec(dllexport) void  EnumSessions_Free(void** ctx);
}
