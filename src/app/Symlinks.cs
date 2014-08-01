using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace PhpVersionSwitcher
{
    public static class Symlinks
    {
        private const int CreationDispositionOpenExisting = 3;
        private const int FileFlagBackupSemantics = 0x02000000;
        private const int SymbolicLinkFlagDirectory = 1;

        // http://msdn.microsoft.com/en-us/library/aa364962(v=vs.85).aspx
        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);

        // http://msdn.microsoft.com/en-us/library/aa363858(v=vs.85).aspx
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr securityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        // http://msdn.microsoft.com/en-us/library/aa363866(v=vs.85).aspx
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
 
        public static string GetTarget(string symlink)
        {
            SafeFileHandle fileHandle = CreateFile(symlink, 0, 2, IntPtr.Zero, CreationDispositionOpenExisting, FileFlagBackupSemantics, IntPtr.Zero);
            if (fileHandle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

            var path = new StringBuilder(512);
            int size = GetFinalPathNameByHandle(fileHandle.DangerousGetHandle(), path, path.Capacity, 0);
            if (size < 0) throw new Win32Exception(Marshal.GetLastWin32Error());

            // The remarks section of GetFinalPathNameByHandle mentions the return being prefixed with "\\?\"
            // More information about "\\?\" here -> http://msdn.microsoft.com/en-us/library/aa365247(v=vs.85).aspx
            var pathStr = path.ToString();
            if (pathStr.StartsWith(@"\\?\")) pathStr = pathStr.Substring(4);
            return pathStr;
        }

        public static bool CreateDir(string symlink, string target)
        {
            return CreateSymbolicLink(symlink, target, SymbolicLinkFlagDirectory);
        }
    }
}
