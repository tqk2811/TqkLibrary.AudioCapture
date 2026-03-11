#pragma once
#include "pch.h"

enum class AudioFormatTag : int {
    PCM = 1,          // WAVE_FORMAT_PCM
    IEEE_FLOAT = 3    // WAVE_FORMAT_IEEE_FLOAT
};

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
    __declspec(dllexport) void* Capture_StartEndpoint(const wchar_t* deviceId, int formatTag, int channels, int sampleRate, int bitsPerSample);
    __declspec(dllexport) void* Capture_StartProcess(int processId, int formatTag, int channels, int sampleRate, int bitsPerSample);
    __declspec(dllexport) bool  Capture_GetFormat(void* ctx, unsigned int* channels, unsigned int* sampleRate, unsigned int* bitsPerSample);
    __declspec(dllexport) int   Capture_Read(void* ctx, unsigned char* buffer, int bufferSize);
    __declspec(dllexport) void  Capture_Stop(void* ctx);
    __declspec(dllexport) void  Capture_Free(void** ctx);
}
