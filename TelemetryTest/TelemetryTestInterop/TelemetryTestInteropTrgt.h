#pragma once

#ifdef _TelemetryTestInteropTrgt
#define EXPORT_TelemetryTestInteropTrgt __declspec(dllexport)
#else
#define EXPORT_TelemetryTestInteropTrgt __declspec(dllimport)
#pragma comment( lib, "TelemetryTestInterop.lib")

#endif

