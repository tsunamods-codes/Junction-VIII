/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AppWrapper {
    public static class Win32 {

        public enum DEP_SYSTEM_POLICY_TYPE
        {
            DEPPolicyAlwaysOff = 0,
            DEPPolicyAlwaysOn,
            DEPPolicyOptIn,
            DEPPolicyOptOut,
            DEPTotalPolicyCount,
        }

        public enum EMoveMethod : uint
        {
            Begin = 0,
            Current = 1,
            End = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        public struct OVERLAPPED {
            public UIntPtr Internal;
            public UIntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr EventHandle;
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal delegate void ReadFileCompletionDelegate(int dwErrorCode, int dwNumBytesTransferred, ref OVERLAPPED lpOverlapped);

        // -------------------------------------------------------------------------------------------------------------------------------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static internal extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateFileW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
           IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
           uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern bool GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern int ReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern int ReadFile(IntPtr handle, IntPtr bytes, uint numBytesToRead, ref uint numBytesRead, ref OVERLAPPED overlapped);

        [DllImport("kernel32.dll")]
        static extern bool ReadFileEx(IntPtr hFile, [Out] byte[] lpBuffer,
           uint nNumberOfBytesToRead, [In] ref System.Threading.NativeOverlapped lpOverlapped,
           ReadFileCompletionDelegate lpCompletionRoutine);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static internal extern int SetFilePointer(
            [In] IntPtr hFile,
            [In] int lDistanceToMove,
            [In] IntPtr lpDistanceToMoveHigh,
            [In] EMoveMethod dwMoveMethod);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern int WriteFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten, [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport("kernel32", ExactSpelling = true)]
        public static extern DEP_SYSTEM_POLICY_TYPE GetSystemDEPPolicy();

        /// <summary>Checks whether a process is being debugged.</summary>
        /// <remarks>
        /// The "remote" in CheckRemoteDebuggerPresent does not imply that the debugger
        /// necessarily resides on a different computer; instead, it indicates that the 
        /// debugger resides in a separate and parallel process.
        /// <para/>
        /// Use the IsDebuggerPresent function to detect whether the calling process 
        /// is running under the debugger.
        /// </remarks>
        [DllImport("Kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CheckRemoteDebuggerPresent(
            IntPtr hProcess,
            [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);
    }

}
