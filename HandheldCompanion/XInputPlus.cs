using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ControllerCommon;
using ControllerCommon.Managers;
using ControllerCommon.Utils;
using Force.Crc32;
using HandheldCompanion.Controllers;
using HandheldCompanion.Managers;
using HandheldCompanion.Properties;
using HandheldCompanion.Views;
using static ControllerCommon.Utils.ProcessUtils;

namespace HandheldCompanion;

public static class XInputPlus
{
    static XInputPlus()
    {
    }

    public static void RegisterApplication(Profile profile)
    {
        var DirectoryPath = $"\"{Path.GetDirectoryName(profile.Path)}\"";

        // we need to define Controller index overwrite
        XInputController controller = (XInputController)ControllerManager.GetVirtualControllers().FirstOrDefault(c => c.GetType() == typeof(XInputController));
        if (controller is null)
            return;

        // prepare arguments array
        List<string> args = new() { DirectoryPath };

        // get virtual controller index
        int idx = controller.GetUserIndex() + 1;
        args.Add($"{idx}");

        // prepare index array and remove current index from it
        var userIndex = new List<int> { 1, 2, 3, 4 };
        userIndex.Remove(idx);

        for (var i = 0; i < userIndex.Count; i++)
            args.Add($"{userIndex[i]}");

        TrustedInstaller.StartProcessWithTrustedInstaller(MainWindow.CurrentPathXInputPlus, args.ToArray());

        LogManager.LogDebug("XInputPlus controller index updated for {0}. Controller1 set to UserIndex: {1}",
            profile.Name, idx);

        // call trusted installer here
    }

    public static void UnregisterApplication(Profile profile)
    {
        var DirectoryPath = $"\"{Path.GetDirectoryName(profile.Path)}\"";
        TrustedInstaller.StartProcessWithTrustedInstaller(MainWindow.CurrentPathXInputPlus, new string[] { DirectoryPath });
    }
}