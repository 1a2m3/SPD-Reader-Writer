//-----------------------------------------------------------------------------
//     Author : hiyohiyo
//       Mail : hiyohiyo@crystalmark.info
//        Web : http://openlibsys.org/
//    License : The modified BSD license
//
//                          Copyright 2007 OpenLibSys.org. All rights reserved.
//-----------------------------------------------------------------------------

#include "stdafx.h"
#include "Driver.h"
#include <tchar.h>
#include "OlsDll.h"

//-----------------------------------------------------------------------------
//
// Global
//
//-----------------------------------------------------------------------------

extern HANDLE gHandle;

//-----------------------------------------------------------------------------
//
// Prototypes
//
//-----------------------------------------------------------------------------

static BOOL InstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath);
static BOOL RemoveDriver(SC_HANDLE hSCManager, LPCTSTR DriverId);
static BOOL StartDriver(SC_HANDLE hSCManager, LPCTSTR DriverId);
static BOOL StopDriver(SC_HANDLE hSCManager, LPCTSTR DriverId);
static BOOL SystemInstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath);
static BOOL IsSystemInstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath);

//-----------------------------------------------------------------------------
//
// Manage Driver
//
//-----------------------------------------------------------------------------

BOOL ManageDriver(LPCTSTR DriverId, LPCTSTR DriverPath, USHORT Function)
{
	SC_HANDLE	hSCManager = NULL;
	BOOL		rCode = FALSE;
	DWORD		error = NO_ERROR;

	if(DriverId == NULL || DriverPath == NULL)
	{
		return FALSE;
	}
	hSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);

    if(hSCManager == NULL)
	{
        return FALSE;
    }

    switch(Function)
	{
	case OLS_DRIVER_INSTALL:
		if(InstallDriver(hSCManager, DriverId, DriverPath))
		{
			rCode = StartDriver(hSCManager, DriverId);
		}
		break;
	case OLS_DRIVER_REMOVE:
		if(! IsSystemInstallDriver(hSCManager, DriverId, DriverPath))
		{
			StopDriver(hSCManager, DriverId);
			rCode = RemoveDriver(hSCManager, DriverId);
		}
		break;
	case OLS_DRIVER_SYSTEM_INSTALL:
		if(IsSystemInstallDriver(hSCManager, DriverId, DriverPath))
		{
			rCode = TRUE;
		}
		else
		{
			if(! OpenDriver())
			{
				StopDriver(hSCManager, DriverId);
				RemoveDriver(hSCManager, DriverId);
				if(InstallDriver(hSCManager, DriverId, DriverPath))
				{
					StartDriver(hSCManager, DriverId);
				}
				OpenDriver();
			}
			rCode = SystemInstallDriver(hSCManager, DriverId, DriverPath);
		}
		break;
	case OLS_DRIVER_SYSTEM_UNINSTALL:
		if(! IsSystemInstallDriver(hSCManager, DriverId, DriverPath))
		{
			rCode = TRUE;
		}
		else
		{
			if(gHandle != INVALID_HANDLE_VALUE)
			{
				CloseHandle(gHandle);
				gHandle = INVALID_HANDLE_VALUE;
			}

			if(StopDriver(hSCManager, DriverId))
			{
				rCode = RemoveDriver(hSCManager, DriverId);
			}
		}
		break;
	default:
		rCode = FALSE;
		break;
    }

	if(hSCManager != NULL)
	{
        CloseServiceHandle(hSCManager);
    }

	return rCode;
}

//-----------------------------------------------------------------------------
//
// Install Driver
//
//-----------------------------------------------------------------------------

BOOL InstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath)
{
	SC_HANDLE	hService = NULL;
    BOOL        rCode = FALSE;
	DWORD		error = NO_ERROR;

	hService = CreateService(hSCManager,
							DriverId,
							DriverId,
							SERVICE_ALL_ACCESS,
							SERVICE_KERNEL_DRIVER,
							SERVICE_DEMAND_START,
							SERVICE_ERROR_NORMAL,
							DriverPath,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL
							);

    if(hService == NULL)
	{
		error = GetLastError();
		if(error == ERROR_SERVICE_EXISTS)
		{
			rCode = TRUE;
        }
    }
	else
	{
		rCode = TRUE;
        CloseServiceHandle(hService);
    }

    return rCode;
}

//-----------------------------------------------------------------------------
//
// System Install Driver
//
//-----------------------------------------------------------------------------

BOOL SystemInstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath)
{
	SC_HANDLE	hService = NULL;
	BOOL		rCode = FALSE;
	
	hService = OpenService(hSCManager, DriverId, SERVICE_ALL_ACCESS);

	if(hService != NULL)
	{
		rCode = ChangeServiceConfig(hService,
							SERVICE_KERNEL_DRIVER,
							SERVICE_AUTO_START,
							SERVICE_ERROR_NORMAL,
							DriverPath,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL
							);
		CloseServiceHandle(hService);
	}

	return rCode;
}

//-----------------------------------------------------------------------------
//
// Remove Driver
//
//-----------------------------------------------------------------------------

BOOL RemoveDriver(SC_HANDLE hSCManager, LPCTSTR DriverId)
{
    SC_HANDLE   hService = NULL;
    BOOL        rCode = FALSE;

    hService = OpenService(hSCManager, DriverId, SERVICE_ALL_ACCESS);
    if(hService == NULL)
	{
		rCode = TRUE;
	}
	else
	{
		rCode = DeleteService(hService);
		CloseServiceHandle(hService);
	}

	return rCode;
}

//-----------------------------------------------------------------------------
//
// Start Driver
//
//-----------------------------------------------------------------------------

BOOL StartDriver(SC_HANDLE hSCManager, LPCTSTR DriverId)
{
	SC_HANDLE	hService = NULL;
	BOOL		rCode = FALSE;
	DWORD		error = NO_ERROR;

	hService = OpenService(hSCManager, DriverId, SERVICE_ALL_ACCESS);

    if(hService != NULL)
	{
		if(! StartService(hService, 0, NULL))
		{
			error = GetLastError();
			if(error == ERROR_SERVICE_ALREADY_RUNNING)
			{
				rCode = TRUE;
			}
		}
		else
		{
			rCode = TRUE;
		}
		CloseServiceHandle(hService);
	}

	return rCode;
}

//-----------------------------------------------------------------------------
//
// Stop Driver
//
//-----------------------------------------------------------------------------

BOOL StopDriver(SC_HANDLE hSCManager, LPCTSTR DriverId)
{
	SC_HANDLE		hService = NULL;
	BOOL			rCode = FALSE;
	SERVICE_STATUS	serviceStatus;
	DWORD		error = NO_ERROR;

	hService = OpenService(hSCManager, DriverId, SERVICE_ALL_ACCESS);

    if(hService != NULL)
	{
		rCode = ControlService(hService, SERVICE_CONTROL_STOP, &serviceStatus);
		error = GetLastError();
        CloseServiceHandle(hService);
	}

    return rCode;
}

//-----------------------------------------------------------------------------
//
// IsSystemInstallDriver
//
//-----------------------------------------------------------------------------

BOOL IsSystemInstallDriver(SC_HANDLE hSCManager, LPCTSTR DriverId, LPCTSTR DriverPath)
{
	SC_HANDLE				hService = NULL;
	BOOL					rCode = FALSE;
	DWORD					dwSize;
	LPQUERY_SERVICE_CONFIG	lpServiceConfig;

    hService = OpenService(hSCManager, DriverId, SERVICE_ALL_ACCESS);

	if(hService != NULL)
	{
		QueryServiceConfig(hService, NULL, 0, &dwSize);
		lpServiceConfig = (LPQUERY_SERVICE_CONFIG)HeapAlloc(GetProcessHeap(), 
															HEAP_ZERO_MEMORY, dwSize);
		QueryServiceConfig(hService, lpServiceConfig, dwSize, &dwSize);

		if(lpServiceConfig->dwStartType == SERVICE_AUTO_START)
		{
			rCode = TRUE;
		}

		CloseServiceHandle(hService);

		HeapFree(GetProcessHeap(), HEAP_NO_SERIALIZE, lpServiceConfig);
	}
	
	return rCode;
}