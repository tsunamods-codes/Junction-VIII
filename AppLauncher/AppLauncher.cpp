// AppLauncher.cpp : Defines the entry point for the application.
//

#include <windows.h>
#include <stdio.h>
#include <sys/stat.h>
#include "Resource.h"

int WINAPI WinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE hPrevInstance, _In_ LPSTR lpCmdLine, _In_ int nCmdShow)
{
    // Initialize the process start information
    STARTUPINFO si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    // Check if the .J8LaunchChoco file exists
    struct stat dummy;
    if (stat(".J8LaunchChoco", &dummy) == 0)
    {
        // Start the process
        if (!CreateProcess(L"chocobo_en.exe", NULL, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) return 1;        
    }
    else
    {
        // Start the process
        if (!CreateProcess(L"ff8_en.exe", NULL, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) return 1;
    }

    // Wait for the process to finish
    WaitForSingleObject(pi.hProcess, INFINITE);

    // Close process and thread handles
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);

	return 0;
}
