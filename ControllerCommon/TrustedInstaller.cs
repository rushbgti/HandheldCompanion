using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ControllerCommon
{
    public class TrustedInstaller
    {
        // Import Win32 API functions
        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessWithTokenW(
            IntPtr hToken,
            LogonFlags dwLogonFlags,
            string lpApplicationName,
            string lpCommandLine,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESSINFO lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr phNewToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSINFO
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public enum LogonFlags
        {
            WithProfile = 1,
            NetCredentialsOnly
        }

        public enum SecurityImpersonationLevel
        {
            SecurityAnonymous = 0,
            SecurityIdentification = 1,
            SecurityImpersonation = 2,
            SecurityDelegation = 3
        }

        // Define constants
        const int TokenTypePrimary = 1;

        public const uint NORMAL_PRIORITY_CLASS = 0x00000020;
        public const uint CREATE_NEW_CONSOLE = 0x00000010;
        public const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const uint STANDARD_RIGHTS_READ = 0x00020000;
        public const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const uint TOKEN_DUPLICATE = 0x0002;
        public const uint TOKEN_IMPERSONATE = 0x0004;
        public const uint TOKEN_QUERY = 0x0008;
        public const uint TOKEN_QUERY_SOURCE = 0x0010;
        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_ADJUST_GROUPS = 0x0040;
        public const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        public const uint TOKEN_ADJUST_SESSIONID = 0x0100;
        public const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const uint TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID);

        const uint CreateUnicodeEnvironment = 0x00000400;
        static IntPtr pEnvBlock;

        static STARTUPINFO SI;
        static PROCESSINFO PI;

        // Define a function to start a process with TrustedInstaller token
        public static void StartProcessWithTrustedInstaller(string ExeToRun, string[] args)
        {
            // Check if TrustedInstaller service is running
            ServiceController sc = new ServiceController("TrustedInstaller");

            if (sc.Status != ServiceControllerStatus.Running)
            {
                // Start the service if not running
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);
            }

            // Get the process id of TrustedInstaller service
            int pid = Process.GetProcessesByName("TrustedInstaller")[0].Id;

            // Get the handle of TrustedInstaller process
            IntPtr hProcess = Process.GetProcessById(pid).Handle;

            // Get the handle of TrustedInstaller token
            IntPtr hToken = IntPtr.Zero;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_DUPLICATE | TOKEN_QUERY, out hToken))
                return;

            // Duplicate the TrustedInstaller token
            IntPtr hNewToken = IntPtr.Zero;
            DuplicateTokenEx(hToken, TOKEN_ALL_ACCESS, IntPtr.Zero, (int)SecurityImpersonationLevel.SecurityIdentification, TokenTypePrimary, out hNewToken);

            // Create environment block
            if (!CreateEnvironmentBlock(out pEnvBlock, hToken, true))
                return;

            // Create process with the token we "stole" ^^
            uint dwCreationFlags = (NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT);
            SI = new STARTUPINFO();
            SI.cb = Marshal.SizeOf(SI);
            SI.lpDesktop = "winsta0\\default";
            PI = new PROCESSINFO();

            // If ExeToRun is not absolute path, then let it be
            ExeToRun = Environment.ExpandEnvironmentVariables(ExeToRun);

            string Arguments = string.Join(" ", args);
            Arguments = Environment.ExpandEnvironmentVariables(Arguments);

            string CmdLine = string.Empty;
            if (!string.IsNullOrEmpty(Arguments))
            {
                if (ExeToRun.Contains(" "))
                    CmdLine = "\"" + ExeToRun + "\" " + Arguments;
                else
                    CmdLine = ExeToRun + " " + Arguments;
            }

            // Create the process with the TrustedInstaller token
            // if (!CreateProcessWithTokenW(hDupToken, LogonFlags.WithProfile, ExeToRun, CmdLine, dwCreationFlags, pEnvBlock, WorkingDir, ref SI, out PI))
            bool success = CreateProcessWithTokenW(hNewToken, LogonFlags.WithProfile, ExeToRun, CmdLine, dwCreationFlags, pEnvBlock, Environment.CurrentDirectory, ref SI, out PI);

            // Close the handles
            CloseHandle(hNewToken);
            CloseHandle(hToken);
        }

        // Import Win32 API function to close a handle
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
        
        // Check if the current process is running with TrustedInstaller rights
        public static bool IsTrustedInstaller()
        {
            // Get the current user identity
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // Check if the user name is "NT SERVICE\\TrustedInstaller"
            return identity != null && identity.Name.Equals("NT SERVICE\\TrustedInstaller", StringComparison.OrdinalIgnoreCase);
        }
    }
}
