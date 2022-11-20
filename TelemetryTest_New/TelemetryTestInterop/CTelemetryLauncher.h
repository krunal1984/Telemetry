#pragma once
#include "TelemetryLauncher.h"
#include "TelemetryTestInteropTrgt.h"

class EXPORT_TelemetryTestInteropTrgt CTelemetryLauncher : public ITelemetryLauncher
{
public:
	CTelemetryLauncher();

	virtual void Init() override;
	virtual void ExecuteCommand(int id, std::wstring appType) override;

// private:
// 	IAnalyticsManager^ mAnalyticsManager;
};

