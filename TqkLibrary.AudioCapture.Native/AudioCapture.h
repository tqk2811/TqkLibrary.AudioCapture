#pragma once
#include "pch.h"

struct CaptureContext {
    Microsoft::WRL::ComPtr<IAudioClient> pAudioClient;
    Microsoft::WRL::ComPtr<IAudioCaptureClient> pCaptureClient;
    WAVEFORMATEX* pFormat = nullptr;
    UINT32 frameCount = 0;
    bool isCapturing = false;

    ~CaptureContext() {
        if (pFormat) CoTaskMemFree(pFormat);
    }
};

extern "C" {
    __declspec(dllexport) void* Capture_StartEndpoint(const wchar_t* deviceId);
    __declspec(dllexport) void* Capture_StartProcess(int processId, int channels, int sampleRate, int bitsPerSample);
    __declspec(dllexport) bool  Capture_GetFormat(void* ctx, unsigned int* channels, unsigned int* sampleRate, unsigned int* bitsPerSample);
    __declspec(dllexport) int   Capture_Read(void* ctx, unsigned char* buffer, int bufferSize);
    __declspec(dllexport) void  Capture_Stop(void* ctx);
    __declspec(dllexport) void  Capture_Free(void** ctx);
}
