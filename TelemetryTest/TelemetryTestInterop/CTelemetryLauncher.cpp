#include "pch.h"
#include "CTelemetryLauncher.h"

using namespace System;
using namespace System::Windows;

using namespace TelemetryLib;

// #pragma warning( disable: 4691 )

void CTelemetryLauncher::Init()
{
	IAnalyticsManager^ analyticsManager = AnalyticsFactory::getInstance(nullptr, false);
	analyticsManager->logSystemEnvironmentDetails(true);
}

void CTelemetryLauncher::ExecuteCommand(int id, std::wstring appType)
{
	try
	{
		IAnalyticsManager^ analyticsManager = AnalyticsFactory::getInstance(nullptr, false);
		AnalyticsEvent^ event = analyticsManager->createEvent("CommandPressed", nullptr);
		String^ appTypeStr = gcnew String(appType.c_str());
		event->add("id", id);
		event->add("Invoked Application", appTypeStr);
		analyticsManager->logEvent(event);
	}
	catch (System::OutOfMemoryException^ e)
	{
	}
	catch (System::Exception^ e)
	{
		String^ msg = e->Message;
		System::Windows::Forms::MessageBox::Show(msg);
	}
}
