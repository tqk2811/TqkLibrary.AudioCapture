#include "pch.h"
#include "AudioCapture.h"
#include "COMHelper.h"
#include <avrt.h>

#include <mmdeviceapi.h>
#include <audioclient.h>
#include <audiopolicy.h>
#include <wrl/implements.h>
#include <wrl/client.h>
#include <ppltasks.h>

using namespace Microsoft::WRL;

class ActivateAudioInterfaceCompletionHandler :
	public RuntimeClass<RuntimeClassFlags<ClassicCom>, FtmBase, IActivateAudioInterfaceCompletionHandler>
{
public:
	ActivateAudioInterfaceCompletionHandler() : _hEvent(CreateEvent(NULL, FALSE, FALSE, NULL)) {}
	~ActivateAudioInterfaceCompletionHandler() { CloseHandle(_hEvent); }

	STDMETHOD(ActivateCompleted)(IActivateAudioInterfaceAsyncOperation* operation)
	{
		_operation = operation;
		SetEvent(_hEvent);
		return S_OK;
	}

	HANDLE GetEvent() { return _hEvent; }
	IActivateAudioInterfaceAsyncOperation* GetOperation() { return _operation.Get(); }

private:
	HANDLE _hEvent;
	ComPtr<IActivateAudioInterfaceAsyncOperation> _operation;
};

void* Capture_StartEndpoint(const wchar_t* deviceId, int formatTag, int channels, int sampleRate, int bitsPerSample) {
	if (FAILED(EnsureCoInitialized()))
		return nullptr;

	ComPtr<IMMDeviceEnumerator> pEnumerator;
	HRESULT hr = CoCreateInstance(__uuidof(MMDeviceEnumerator), NULL, CLSCTX_ALL, IID_PPV_ARGS(&pEnumerator));
	if (FAILED(hr))
		return nullptr;

	ComPtr<IMMDevice> pDevice;
	if (deviceId == nullptr)
	{
		hr = pEnumerator->GetDefaultAudioEndpoint(eRender, eMultimedia, &pDevice);
	}
	else
	{
		hr = pEnumerator->GetDevice(deviceId, &pDevice);
	}
	if (FAILED(hr))
		return nullptr;

	// Detect endpoint type (render vs capture)
	ComPtr<IMMEndpoint> pEndpoint;
	hr = pDevice->QueryInterface(IID_PPV_ARGS(&pEndpoint));
	if (FAILED(hr))
		return nullptr;

	EDataFlow dataFlow;
	hr = pEndpoint->GetDataFlow(&dataFlow);
	if (FAILED(hr))
		return nullptr;

	bool useCustomFormat = (channels > 0 && sampleRate > 0 && bitsPerSample > 0);
	auto ctx = new CaptureContext();

	hr = pDevice->Activate(__uuidof(IAudioClient), CLSCTX_ALL, NULL, &ctx->pAudioClient);
	if (FAILED(hr))
		goto end;

	if (useCustomFormat)
	{
		// Use explicit PCM format with AUTOCONVERTPCM for noise-free conversion
		ctx->pFormat = (WAVEFORMATEX*)CoTaskMemAlloc(sizeof(WAVEFORMATEX));
		if (!ctx->pFormat)
		{
			hr = E_OUTOFMEMORY;
			goto end;
		}
		ctx->pFormat->wFormatTag = (WORD)formatTag; // Usually WAVE_FORMAT_PCM (1) or WAVE_FORMAT_IEEE_FLOAT (3)
		ctx->pFormat->nChannels = (WORD)channels;
		ctx->pFormat->nSamplesPerSec = (DWORD)sampleRate;
		ctx->pFormat->wBitsPerSample = (WORD)bitsPerSample;
		ctx->pFormat->nBlockAlign = ctx->pFormat->nChannels * ctx->pFormat->wBitsPerSample / 8;
		ctx->pFormat->nAvgBytesPerSec = ctx->pFormat->nSamplesPerSec * ctx->pFormat->nBlockAlign;
		ctx->pFormat->cbSize = 0;
	}
	else
	{
		// Use device native format (may be IEEE Float 32-bit)
		hr = ctx->pAudioClient->GetMixFormat(&ctx->pFormat);
		if (FAILED(hr))
			goto end;
	}

	{
		// AUDCLNT_STREAMFLAGS_LOOPBACK is only valid for render endpoints (speakers/headphones)
		// For capture endpoints (microphones), no special flags are needed
		DWORD streamFlags = (dataFlow == eRender) ? AUDCLNT_STREAMFLAGS_LOOPBACK : 0;
		if (useCustomFormat)
		{
			// Auto convert from device native format to our requested PCM format
			streamFlags |= AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY;
		}
		REFERENCE_TIME hnsBufferDuration = 200000; // 20ms buffer
		hr = ctx->pAudioClient->Initialize(AUDCLNT_SHAREMODE_SHARED, streamFlags, hnsBufferDuration, 0, ctx->pFormat, NULL);
		if (FAILED(hr))
			goto end;
	}

	hr = ctx->pAudioClient->GetService(IID_PPV_ARGS(&ctx->pCaptureClient));
	if (FAILED(hr))
		goto end;

	hr = ctx->pAudioClient->Start();
end:
	if (FAILED(hr))
	{
		delete ctx;
		return nullptr;
	}
	else
	{
		ctx->isCapturing = true;
		return ctx;
	}
}

void* Capture_StartProcess(int processId, int formatTag, int channels, int sampleRate, int bitsPerSample) {
	if (FAILED(EnsureCoInitialized()))
		return nullptr;

	REFERENCE_TIME hnsBufferDuration = 200000; // 20ms buffer

	AUDIOCLIENT_ACTIVATION_PARAMS params = { };
	params.ActivationType = AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK;
	params.ProcessLoopbackParams.ProcessLoopbackMode = PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE;
	params.ProcessLoopbackParams.TargetProcessId = processId;

	PROPVARIANT prop;
	PropVariantInit(&prop);
	prop.vt = VT_BLOB;
	prop.blob.cbSize = sizeof(params);
	prop.blob.pBlobData = (BYTE*)&params;

	auto completionHandler = Make<ActivateAudioInterfaceCompletionHandler>();
	ComPtr<IActivateAudioInterfaceAsyncOperation> asyncOp;

	// VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK is for process loopback
	HRESULT hr = ActivateAudioInterfaceAsync(VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, __uuidof(IAudioClient), &prop, completionHandler.Get(), &asyncOp);
	if (FAILED(hr))
		return nullptr;

	WaitForSingleObject(completionHandler->GetEvent(), INFINITE);

	HRESULT hrActivate = S_OK;
	ComPtr<IUnknown> punkAudioInterface;
	hr = completionHandler->GetOperation()->GetActivateResult(&hrActivate, &punkAudioInterface);
	if (FAILED(hr) || FAILED(hrActivate))
		return nullptr;

	auto ctx = new CaptureContext();
	hr = punkAudioInterface.As(&ctx->pAudioClient);
	if (FAILED(hr))
		goto end;

	// Process loopback IAudioClient does not support GetMixFormat (returns E_NOTIMPL).
	// Manually specify desired capture format and use AUTOCONVERTPCM for automatic conversion.
	ctx->pFormat = (WAVEFORMATEX*)CoTaskMemAlloc(sizeof(WAVEFORMATEX));
	if (!ctx->pFormat)
	{
		hr = E_OUTOFMEMORY;
		goto end;
	}
	ctx->pFormat->wFormatTag = (WORD)formatTag; // Usually WAVE_FORMAT_PCM (1) or WAVE_FORMAT_IEEE_FLOAT (3)
	ctx->pFormat->nChannels = (WORD)channels;
	ctx->pFormat->nSamplesPerSec = (DWORD)sampleRate;
	ctx->pFormat->wBitsPerSample = (WORD)bitsPerSample;
	ctx->pFormat->nBlockAlign = ctx->pFormat->nChannels * ctx->pFormat->wBitsPerSample / 8;
	ctx->pFormat->nAvgBytesPerSec = ctx->pFormat->nSamplesPerSec * ctx->pFormat->nBlockAlign;
	ctx->pFormat->cbSize = 0;

	hr = ctx->pAudioClient->Initialize(
		AUDCLNT_SHAREMODE_SHARED,
		AUDCLNT_STREAMFLAGS_LOOPBACK | AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
		hnsBufferDuration, 
		0, 
		ctx->pFormat, 
		NULL
	);
	if (FAILED(hr))
		goto end;

	hr = ctx->pAudioClient->GetService(IID_PPV_ARGS(&ctx->pCaptureClient));
	if (FAILED(hr))
		goto end;

	hr = ctx->pAudioClient->Start();

end:
	if (FAILED(hr))
	{
		delete ctx;
		return nullptr;
	}
	else
	{
		ctx->isCapturing = true;
		return ctx;
	}
}

bool Capture_GetFormat(void* ctx, unsigned int* formatTag, unsigned int* channels, unsigned int* sampleRate, unsigned int* bitsPerSample) {
	if (!ctx)
		return false;
	auto c = static_cast<CaptureContext*>(ctx);
	if (!c->pFormat)
		return false;

	if (formatTag) *formatTag = c->pFormat->wFormatTag;
	if (channels) *channels = c->pFormat->nChannels;
	if (sampleRate) *sampleRate = c->pFormat->nSamplesPerSec;
	if (bitsPerSample) *bitsPerSample = c->pFormat->wBitsPerSample;

	return true;
}

int Capture_Read(void* ctx, unsigned char* buffer, int bufferSize) {
	if (!ctx || !buffer)
		return 0;
	auto c = static_cast<CaptureContext*>(ctx);
	if (!c->isCapturing)
		return 0;

	UINT32 nextPacketSize = 0;
	HRESULT hr = c->pCaptureClient->GetNextPacketSize(&nextPacketSize);
	if (FAILED(hr) || nextPacketSize == 0)
		return 0;

	BYTE* pData = nullptr;
	UINT32 numFramesAvailable = 0;
	DWORD flags = 0;

	hr = c->pCaptureClient->GetBuffer(&pData, &numFramesAvailable, &flags, NULL, NULL);
	if (FAILED(hr))
		return 0;

	int bytesAvailable = numFramesAvailable * c->pFormat->nBlockAlign;
	int bytesToCopy = (bytesAvailable < bufferSize) ? bytesAvailable : bufferSize;

	if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) || (flags & AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY))
	{
		// Silent or discontinuity -> return silence to avoid noise
		memset(buffer, 0, bytesToCopy);
	}
	else
	{
		memcpy(buffer, pData, bytesToCopy);
	}

	c->pCaptureClient->ReleaseBuffer(numFramesAvailable);

	return bytesToCopy;
}

void Capture_Stop(void* ctx) {
	if (ctx)
	{
		auto c = static_cast<CaptureContext*>(ctx);
		if (c->isCapturing)
		{
			c->pAudioClient->Stop();
			c->isCapturing = false;
		}
	}
}

void Capture_Free(void** ctx) {
	if (ctx && *ctx)
	{
		auto c = static_cast<CaptureContext*>(*ctx);
		Capture_Stop(c);
		delete c;
		*ctx = nullptr;
	}
}
