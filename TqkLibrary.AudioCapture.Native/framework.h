#pragma once

// Require Windows 10 Build 20348+ for process loopback capture APIs
// (AUDIOCLIENT_ACTIVATION_PARAMS, VIRTUAL_AUDIO_DEVICE_PROCESS_LOOPBACK, etc.)
#include <sdkddkver.h>
#undef NTDDI_VERSION
#define NTDDI_VERSION 0x0A00000A // NTDDI_WIN10_FE

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
