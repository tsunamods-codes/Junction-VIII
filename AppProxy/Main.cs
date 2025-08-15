﻿using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace AppProxy
{
    static unsafe class Proxy
    {
        private static Assembly? lib = null;
        private static Type? t = null;

        private static MethodInfo? _mRun = null;
        private static MethodInfo? _mShutdown = null;
        private static MethodInfo? _mHCreateFileA = null;
        private static MethodInfo? _mHCreateFile2 = null;
        private static MethodInfo? _mHCreateFileW = null;
        private static MethodInfo? _mHReadFile = null;
        private static MethodInfo? _mHFindFirstFileW = null;
        private static MethodInfo? _mHFindFirstFileExW = null;
        private static MethodInfo? _mHFindNextFileW = null;
        private static MethodInfo? _mHFindClose = null;
        private static MethodInfo? _mHSetFilePointer = null;
        private static MethodInfo? _mHSetFilePointerEx = null;
        private static MethodInfo? _mHCloseHandle = null;
        private static MethodInfo? _mHGetFileType = null;
        private static MethodInfo? _mHGetFileInformationByHandle = null;
        private static MethodInfo? _mHDuplicateHandle = null;
        private static MethodInfo? _mHGetFileSize = null;
        private static MethodInfo? _mHGetFileSizeEx = null;
        private static MethodInfo? _mHGetFileAttributesExW = null;

        [StructLayout(LayoutKind.Sequential)]
        public struct HostExports
        {
            public delegate* unmanaged<void> Shutdown;
            public delegate* unmanaged<byte*, uint, uint, void*, uint, uint, void*, void*> CreateFileA;
            public delegate* unmanaged<ushort*, uint, uint, uint, void*, void*> CreateFile2;
            public delegate* unmanaged<ushort*, uint, uint, void*, uint, uint, void*, void*> CreateFileW;
            public delegate* unmanaged<void*, void*, uint, uint*, void*, int> ReadFile;
            //public delegate* unmanaged<void*, void*, uint, uint*, void*, int> WriteFile;
            public delegate* unmanaged<ushort*, void*, void*> FindFirstFileW;
            public delegate* unmanaged<ushort*, uint, void*, uint, void*, uint, void*> FindFirstFileExW;
            public delegate* unmanaged<void*, void*, int> FindNextFileW;
            public delegate* unmanaged<void*, int> FindClose;
            public delegate* unmanaged<void*, int, int*, uint, uint> SetFilePointer;
            public delegate* unmanaged<void*, long, void*, uint, int> SetFilePointerEx;
            public delegate* unmanaged<void*, int> CloseHandle;
            public delegate* unmanaged<void*, uint> GetFileType;
            public delegate* unmanaged<void*, void*, int> GetFileInformationByHandle;
            public delegate* unmanaged<void*, void*, void*, void**, uint, int, uint, int> DuplicateHandle;
            public delegate* unmanaged<void*, uint*, uint> GetFileSize;
            public delegate* unmanaged<void*, int*, int> GetFileSizeEx;
            public delegate* unmanaged<ushort*, uint, void*, int> GetFileAttributesExW;
        }

        private static HostExports* _exports;

        private static string StringFromUtf8(byte* pBytes)
        {
            if (pBytes == null)
                throw new ArgumentNullException(nameof(pBytes));

            // Calculate the length of the null-terminated string
            int length = 0;
            while (pBytes[length] != 0)
            {
                length++;
            }

            // Convert the byte sequence to a managed string
            return System.Text.Encoding.UTF8.GetString(pBytes, length);
        }

        [UnmanagedCallersOnly]
        public static int Main(void* exports)
        {
            try
            {
                _exports = (HostExports*)exports;

                _exports->Shutdown = &Shutdown;
                _exports->CreateFileA = &HCreateFileA;
                _exports->CreateFile2 = &HCreateFile2;
                _exports->CreateFileW = &HCreateFileW;
                _exports->ReadFile = &HReadFile;
                _exports->FindFirstFileW = &HFindFirstFileW;
                _exports->FindFirstFileExW = &HFindFirstFileExW;
                _exports->FindNextFileW = &HFindNextFileW;
                _exports->FindClose = &HFindClose;
                _exports->SetFilePointer = &HSetFilePointer;
                _exports->SetFilePointerEx = &HSetFilePointerEx;
                _exports->CloseHandle = &HCloseHandle;
                _exports->GetFileType = &HGetFileType;
                _exports->GetFileInformationByHandle = &HGetFileInformationByHandle;
                _exports->DuplicateHandle = &HDuplicateHandle;
                _exports->GetFileSize = &HGetFileSize;
                _exports->GetFileSizeEx = &HGetFileSizeEx;
                _exports->GetFileAttributesExW = &HGetFileAttributesExW;

                lib = AssemblyLoadContext.GetLoadContext(typeof(Proxy).Assembly).LoadFromAssemblyPath(Path.Combine(Directory.GetCurrentDirectory(), "AppWrapper.dll"));
                t = lib.GetType("AppWrapper.Wrap");

                if (t != null)
                {
                    _mRun = t.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
                    _mShutdown = t.GetMethod("Shutdown", BindingFlags.Static | BindingFlags.Public);
                    _mHCreateFileA = t.GetMethod("HCreateFileA", BindingFlags.Static | BindingFlags.Public);
                    _mHCreateFile2 = t.GetMethod("HCreateFile2", BindingFlags.Static | BindingFlags.Public);
                    _mHCreateFileW = t.GetMethod("HCreateFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHReadFile = t.GetMethod("HReadFile", BindingFlags.Static | BindingFlags.Public);
                    _mHFindFirstFileW = t.GetMethod("HFindFirstFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHFindFirstFileExW = t.GetMethod("HFindFirstFileExW", BindingFlags.Static | BindingFlags.Public);
                    _mHFindNextFileW = t.GetMethod("HFindNextFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHFindClose = t.GetMethod("HFindClose", BindingFlags.Static | BindingFlags.Public);
                    _mHFindFirstFileW = t.GetMethod("HFindFirstFileW", BindingFlags.Static | BindingFlags.Public);
                    _mHSetFilePointer = t.GetMethod("HSetFilePointer", BindingFlags.Static | BindingFlags.Public);
                    _mHSetFilePointerEx = t.GetMethod("HSetFilePointerEx", BindingFlags.Static | BindingFlags.Public);
                    _mHCloseHandle = t.GetMethod("HCloseHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileType = t.GetMethod("HGetFileType", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileInformationByHandle = t.GetMethod("HGetFileInformationByHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHDuplicateHandle = t.GetMethod("HDuplicateHandle", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileSize = t.GetMethod("HGetFileSize", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileSizeEx = t.GetMethod("HGetFileSizeEx", BindingFlags.Static | BindingFlags.Public);
                    _mHGetFileAttributesExW = t.GetMethod("HGetFileAttributesExW", BindingFlags.Static | BindingFlags.Public);
                }

                if (_mRun != null) _mRun.Invoke(null, new object[] { Process.GetCurrentProcess(), Type.Missing });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return 0;
        }

        [UnmanagedCallersOnly]
        public static void Shutdown()
        {
            try
            {
                if (_mShutdown != null) _mShutdown.Invoke(null, null);

                t = null;
                lib = null;

                _exports->Shutdown = null;
                _exports->CreateFileA = null;
                _exports->CreateFile2 = null;
                _exports->CreateFileW = null;
                _exports->ReadFile = null;
                _exports->FindFirstFileW = null;
                _exports->FindFirstFileExW = null;
                _exports->FindNextFileW = null;
                _exports->FindClose = null;
                _exports->SetFilePointer = null;
                _exports->SetFilePointerEx = null;
                _exports->CloseHandle = null;
                _exports->GetFileType = null;
                _exports->GetFileInformationByHandle = null;
                _exports->DuplicateHandle = null;
                _exports->GetFileSize = null;
                _exports->GetFileSizeEx = null;
                _exports->GetFileAttributesExW = null;

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.WaitForFullGCComplete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        [UnmanagedCallersOnly]
        public static void* HCreateFileA(byte* lpFileName, uint dwDesiredAccess, uint dwShareMode, void* lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, void* hTemplateFile)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHCreateFileA != null) ret = (IntPtr)(_mHCreateFileA.Invoke(null, new object[] { StringFromUtf8(lpFileName), (System.IO.FileAccess)dwDesiredAccess, (System.IO.FileShare)dwShareMode, new IntPtr(lpSecurityAttributes), (System.IO.FileMode)dwCreationDisposition, (System.IO.FileAttributes)dwFlagsAndAttributes, new IntPtr(hTemplateFile) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static void* HCreateFile2(ushort* lpFileName, uint dwDesiredAccess, uint dwShareMode, uint dwCreationDisposition, void* pCreateExParams)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHCreateFile2 != null) ret = (IntPtr)(_mHCreateFile2.Invoke(null, new object[] { new string((char*)lpFileName), (System.IO.FileAccess)dwDesiredAccess, (System.IO.FileShare)dwShareMode, (System.IO.FileMode)dwCreationDisposition, new IntPtr(pCreateExParams) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static void* HCreateFileW(ushort* lpFileName, uint dwDesiredAccess, uint dwShareMode, void* lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, void* hTemplateFile)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHCreateFileW != null) ret = (IntPtr)(_mHCreateFileW.Invoke(null, new object[] { new string((char*)lpFileName), (System.IO.FileAccess)dwDesiredAccess, (System.IO.FileShare)dwShareMode, new IntPtr(lpSecurityAttributes), (System.IO.FileMode)dwCreationDisposition, (System.IO.FileAttributes)dwFlagsAndAttributes, new IntPtr(hTemplateFile) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static int HReadFile(void* handle, void* bytes, uint numBytesToRead, uint* numBytesRead, void* overlapped)
        {
            int ret = 0;

            try
            {
                if (_mHReadFile != null) ret = (int)(_mHReadFile.Invoke(null, new object[] { new IntPtr(handle), new IntPtr(bytes), numBytesToRead, new IntPtr(numBytesRead), new IntPtr(overlapped) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static void* HFindFirstFileW(ushort* lpFileName, void* lpFindFileData)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHFindFirstFileW != null) ret = (IntPtr)(_mHFindFirstFileW.Invoke(null, new object[] { new string((char*)lpFileName), new IntPtr(lpFindFileData) }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static void* HFindFirstFileExW(ushort* lpFileName, uint fInfoLevelId, void* lpFindFileData, uint fSearchOp, void* lpSearchFilter, uint dwAdditionalFlags)
        {
            IntPtr ret = IntPtr.Zero;

            try
            {
                if (_mHFindFirstFileExW != null) ret = (IntPtr)(_mHFindFirstFileExW.Invoke(null, new object[] { new string((char*)lpFileName), fInfoLevelId, new IntPtr(lpFindFileData), fSearchOp, new IntPtr(lpSearchFilter), dwAdditionalFlags }) ?? IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret == IntPtr.Zero ? null : ret.ToPointer();
        }

        [UnmanagedCallersOnly]
        public static int HFindNextFileW(void* hFindFile, void* lpFindFileData)
        {
            int ret = 0;

            try
            {
                if (_mHFindNextFileW != null) ret = (int)(_mHFindNextFileW.Invoke(null, new object[] { new IntPtr(hFindFile), new IntPtr(lpFindFileData) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HFindClose(void* hFindFile)
        {
            int ret = 0;

            try
            {
                if (_mHFindClose != null) ret = (int)(_mHFindClose.Invoke(null, new object[] { new IntPtr(hFindFile) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static uint HSetFilePointer(void* hFile, int lDistanceTomove, int* lpDistanceToMoveHigh, uint dwMoveMethod)
        {
            uint ret = uint.MaxValue;

            try
            {
                if (_mHSetFilePointer != null) ret = (uint)(_mHSetFilePointer.Invoke(null, new object[] { new IntPtr(hFile), lDistanceTomove, new IntPtr(lpDistanceToMoveHigh), dwMoveMethod }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HSetFilePointerEx(void* hFile, long liDistanceToMove, void* lpNewFilePointer, uint dwMoveMethod)
        {
            int ret = 0;

            try
            {
                if (_mHSetFilePointerEx != null) ret = (int)(_mHSetFilePointerEx.Invoke(null, new object[] { new IntPtr(hFile), liDistanceToMove, new IntPtr(lpNewFilePointer), dwMoveMethod }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HCloseHandle(void* hObject)
        {
            int ret = 0;

            try
            {
                if (_mHCloseHandle != null) ret = (int)(_mHCloseHandle.Invoke(null, new object[] { new IntPtr(hObject) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static uint HGetFileType(void* hFile)
        {
            uint ret = 0;

            try
            {
                if (_mHGetFileType != null) ret = (uint)(_mHGetFileType.Invoke(null, new object[] { new IntPtr(hFile) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HGetFileInformationByHandle(void* hFile, void* lpFileInformation)
        {
            int ret = 0;

            try
            {
                if (_mHGetFileInformationByHandle != null) ret = (int)(_mHGetFileInformationByHandle.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileInformation) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HDuplicateHandle(void* hSourceProcessHandle, void* hSourceHandle, void* hTargetProcessHandle, void** lpTargetHandle, uint dwDesiredAccess, int bInheritHandle, uint dwOptions)
        {
            int ret = 0;

            try
            {
                if (_mHDuplicateHandle != null) ret = (int)(_mHDuplicateHandle.Invoke(null, new object[] { new IntPtr(hSourceProcessHandle), new IntPtr(hSourceHandle), new IntPtr(hTargetProcessHandle), new IntPtr(lpTargetHandle), dwDesiredAccess, bInheritHandle, dwOptions }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static uint HGetFileSize(void* hFile, uint* lpFileSizeHigh)
        {
            uint ret = 0xFFFFFFFF;

            try
            {
                if (_mHGetFileSize != null) ret = (uint)(_mHGetFileSize.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileSizeHigh) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HGetFileSizeEx(void* hFile, int* lpFileSize)
        {
            int ret = 0;

            try
            {
                if (_mHGetFileSizeEx != null) ret = (int)(_mHGetFileSizeEx.Invoke(null, new object[] { new IntPtr(hFile), new IntPtr(lpFileSize) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }

        [UnmanagedCallersOnly]
        public static int HGetFileAttributesExW(ushort* lpFileName, uint fInfoLevelId, void* lpFileInformation)
        {
            int ret = 0;

            try
            {
                if (_mHGetFileAttributesExW != null) ret = (int)(_mHGetFileAttributesExW.Invoke(null, new object[] { new string((char*)lpFileName), fInfoLevelId, new IntPtr(lpFileInformation) }) ?? 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return ret;
        }
    }
}