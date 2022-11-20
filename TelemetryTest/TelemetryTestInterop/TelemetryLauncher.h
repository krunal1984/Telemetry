#pragma once
#include <string>

class ITelemetryLauncher
{
public:
	virtual void Init() = 0;
	virtual void ExecuteCommand(int id, std::wstring appType) = 0;
};
