using ControllerCommon;
using ControllerCommon.Managers;
using ControllerCommon.Utils;
using Force.Crc32;
using System.Diagnostics;
using System.Reflection;
using XInputPlus.Properties;
using static ControllerCommon.Utils.ProcessUtils;

namespace XinputPlus
{
    internal class Program
    {
        private static readonly Dictionary<bool, uint> CRCs = new Dictionary<bool, uint>()
        {
            { false, 0xcd4906cc },
            { true, 0x1e9df650 }
        };

        private static readonly string IniContent = Resources.XInputPlus;

        static void Main(string[] args)
        {
            // get current assembly
            var CurrentAssembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(CurrentAssembly.Location);

            // initialize log
            LogManager.Initialize("XInputPlus");
            LogManager.LogInformation("{0} ({1})", CurrentAssembly.GetName(), fileVersionInfo.FileVersion);
            LogManager.LogInformation("args: {0}", string.Join(" ", args));

            if (args.Length >= 1)
            {
                string path = args[0];

                LogManager.LogInformation("Path: {0}" , path);

                if (args.Length == 1)
                {
                    UnregisterApplication(path);
                }
                else if (args.Length == 5)
                {
                    int idx1 = Convert.ToInt32(args[1]);
                    int idx2 = Convert.ToInt32(args[2]);
                    int idx3 = Convert.ToInt32(args[3]);
                    int idx4 = Convert.ToInt32(args[3]);

                    RegisterApplication(path, idx1, idx2, idx3, idx4);
                }
            }
        }

        public static void RegisterApplication(string path, int idx1, int idx2, int idx3, int idx4)
        {
            var IniPath = Path.Combine(path, "XInputPlus.ini");

            LogManager.LogInformation("Trying to deploy XInputPlus on {0}", IniPath);

            if (!CommonUtils.IsFileWritable(IniPath))
            {
                LogManager.LogError("Failed to deploy XInputPlus. IniPath cannot be written");
                Environment.Exit(3);
            }

            File.WriteAllText(IniPath, IniContent);

            // make virtual controller new 1st controller
            var IniFile = new IniFile(IniPath);
            IniFile.Write("Controller1", Convert.ToString(idx1), "ControllerNumber");
            IniFile.Write("Controller2", Convert.ToString(idx2), "ControllerNumber");
            IniFile.Write("Controller3", Convert.ToString(idx3), "ControllerNumber");
            IniFile.Write("Controller4", Convert.ToString(idx4), "ControllerNumber");

            // get binary type (x64, x86)
            BinaryType bt;
            GetBinaryType(path, out bt);
            var x64 = bt == BinaryType.SCS_64BIT_BINARY;

            for (var i = 0; i < 5; i++)
            {
                LogManager.LogInformation("Trying to deploy xinput1_{0}.dll", i + 1);

                var dllpath = Path.Combine(path, $"xinput1_{i + 1}.dll");
                var backpath = Path.Combine(path, $"xinput1_{i + 1}.back");

                // dll has a different naming format
                if (i == 4)
                {
                    dllpath = Path.Combine(path, "xinput9_1_0.dll");
                    backpath = Path.Combine(path, "xinput9_1_0.back");
                }

                var dllexist = File.Exists(dllpath);
                var backexist = File.Exists(backpath);

                byte[] inputData = { 0 };

                // check CRC32
                if (dllexist) inputData = File.ReadAllBytes(dllpath);
                var crc = Crc32Algorithm.Compute(inputData);
                var is_x360ce = CRCs[x64] == crc;

                // pull data from dll
                var outputData = x64 ? Resources.xinput1_x64 : Resources.xinput1_x86;

                if (dllexist && is_x360ce) continue; // skip to next file

                if (!dllexist)
                {
                    File.WriteAllBytes(dllpath, outputData);
                }
                else if (dllexist && !is_x360ce)
                {
                    // create backup of current dll
                    if (!backexist)
                        File.Move(dllpath, backpath, true);

                    // deploy wrapper
                    if (CommonUtils.IsFileWritable(dllpath))
                        File.WriteAllBytes(dllpath, outputData);
                }
            }

            LogManager.LogInformation($"Successfully deployed XInputPlus");
        }

        public static void UnregisterApplication(string path)
        {
            var IniPath = Path.Combine(path, "XInputPlus.ini");

            // get binary type (x64, x86)
            BinaryType bt;
            GetBinaryType(path, out bt);
            var x64 = bt == BinaryType.SCS_64BIT_BINARY;

            for (var i = 0; i < 5; i++)
            {
                var dllpath = Path.Combine(path, $"xinput1_{i + 1}.dll");
                var backpath = Path.Combine(path, $"xinput1_{i + 1}.back");

                // dll has a different naming format
                if (i == 4)
                {
                    dllpath = Path.Combine(path, "xinput9_1_0.dll");
                    backpath = Path.Combine(path, "xinput9_1_0.back");
                }

                var dllexist = File.Exists(dllpath);
                var backexist = File.Exists(backpath);

                byte[] inputData = { 0 };

                // check CRC32
                if (dllexist) inputData = File.ReadAllBytes(dllpath);
                var crc = Crc32Algorithm.Compute(inputData);
                var is_x360ce = CRCs[x64] == crc;

                // restore backup is exists
                if (backexist)
                    File.Move(backpath, dllpath, true);
            }

            LogManager.LogInformation($"Successfully removed XInputPlus");
        }
    }
}