﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Interop;
using Microsoft.PowerToys.Run.Plugin.System.Properties;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common.Win32;
using Wox.Plugin.Logger;

namespace Microsoft.PowerToys.Run.Plugin.System.Components
{
    /// <summary>
    /// This class holds all available results
    /// </summary>
    internal static class Commands
    {
        internal const int EWXLOGOFF = 0x00000000;
        internal const int EWXSHUTDOWN = 0x00000001;
        internal const int EWXREBOOT = 0x00000002;
        internal const int EWXFORCE = 0x00000004;
        internal const int EWXPOWEROFF = 0x00000008;
        internal const int EWXFORCEIFHUNG = 0x00000010;

        /// <summary>
        /// Returns a list with all system command results
        /// </summary>
        /// <param name="isUefi">Value indicating if the system is booted in uefi mode</param>
        /// <param name="iconTheme">The current theme to use for the icons</param>
        /// <param name="culture">The culture to use for the result's title and sub title</param>
        /// <param name="confirmCommands">A value indicating if the user should confirm the system commands</param>
        /// <returns>A list of all results</returns>
        internal static List<Result> GetSystemCommands(bool isUefi, string iconTheme, CultureInfo culture, bool confirmCommands)
        {
            var results = new List<Result>();
            results.AddRange(new[]
            {
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_shutdown_computer", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_shutdown_computer_description", culture),
                    IcoPath = $"Images\\shutdown.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_shutdown_computer_confirmation, () => Helper.OpenInShell("shutdown", "/s /hybrid /t 0"));
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_restart_computer", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_restart_computer_description", culture),
                    IcoPath = $"Images\\restart.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_restart_computer_confirmation, () => Helper.OpenInShell("shutdown", "/r /t 0"));
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_sign_out", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_sign_out_description", culture),
                    IcoPath = $"Images\\logoff.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_sign_out_confirmation, () => NativeMethods.ExitWindowsEx(EWXLOGOFF, 0));
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_lock", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_lock_description", culture),
                    IcoPath = $"Images\\lock.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_lock_confirmation, () => NativeMethods.LockWorkStation());
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_sleep", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_sleep_description", culture),
                    IcoPath = $"Images\\sleep.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_sleep_confirmation, () => NativeMethods.SetSuspendState(false, true, true));
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_hibernate", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_hibernate_description", culture),
                    IcoPath = $"Images\\sleep.{iconTheme}.png", // Icon change needed
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_hibernate_confirmation, () => NativeMethods.SetSuspendState(true, true, true));
                    },
                },
                new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_emptyrecyclebin", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_emptyrecyclebin_description", culture),
                    IcoPath = $"Images\\recyclebin.{iconTheme}.png",
                    Action = c =>
                    {
                        // http://www.pinvoke.net/default.aspx/shell32/SHEmptyRecycleBin.html
                        // FYI, couldn't find documentation for this but if the recycle bin is already empty, it will return -2147418113 (0x8000FFFF (E_UNEXPECTED))
                        // 0 for nothing
                        var result = NativeMethods.SHEmptyRecycleBin(new WindowInteropHelper(Application.Current.MainWindow).Handle, 0);
                        if (result != (uint)HRESULT.S_OK && result != 0x8000FFFF)
                        {
                            var name = "Plugin: " + Resources.Microsoft_plugin_sys_plugin_name;
                            var message = $"Error emptying recycle bin, error code: {result}\n" +
                                          "please refer to https://msdn.microsoft.com/en-us/library/windows/desktop/aa378137";
                            Log.Error(message, typeof(Commands));
                            _ = MessageBox.Show(message, name);
                        }

                        return true;
                    },
                },
            });

            // UEFI command/result. It is only available on systems booted in UEFI mode.
            if (isUefi)
            {
                results.Add(new Result
                {
                    Title = Resources.ResourceManager.GetString("Microsoft_plugin_sys_uefi", culture),
                    SubTitle = Resources.ResourceManager.GetString("Microsoft_plugin_sys_uefi_description", culture),
                    IcoPath = $"Images\\firmwareSettings.{iconTheme}.png",
                    Action = c =>
                    {
                        return ResultHelper.ExecuteCommand(confirmCommands, Resources.Microsoft_plugin_sys_uefi_confirmation, () => Helper.OpenInShell("shutdown", "/r /fw /t 0", null, true));
                    },
                });
            }

            return results;
        }

        /// <summary>
        /// Returns a list of all ip and mac results
        /// </summary>
        /// <param name="iconTheme">The theme to use for the icons</param>
        /// <param name="culture">The culture to use for the result's title and sub title</param>
        /// <returns>The list of available results</returns>
        internal static List<Result> GetNetworkConnectionResults(string iconTheme, CultureInfo culture)
        {
            var results = new List<Result>();

            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.GetPhysicalAddress() != null);
            foreach (NetworkInterface i in interfaces)
            {
                NetworkConnectionProperties intInfo = new NetworkConnectionProperties(i);

                if (!string.IsNullOrEmpty(intInfo.IPv4))
                {
                    results.Add(new Result()
                    {
                        Title = intInfo.IPv4,
                        SubTitle = string.Format(CultureInfo.InvariantCulture, Resources.ResourceManager.GetString("Microsoft_plugin_sys_ip4_description", culture), intInfo.ConnectionName) + " - " + Resources.ResourceManager.GetString("Microsoft_plugin_sys_SubTitle_CopyHint", culture),
                        IcoPath = $"Images\\networkAdapter.{iconTheme}.png",
                        ToolTipData = new ToolTipData(Resources.Microsoft_plugin_sys_ConnectionDetails, intInfo.GetConnectionDetails()),
                        ContextData = new SystemPluginContext { Type = ResultContextType.NetworkAdapterInfo, Data = intInfo.GetConnectionDetails() },
                        Action = _ => ResultHelper.CopyToClipBoard(intInfo.IPv4),
                    });
                }

                if (!string.IsNullOrEmpty(intInfo.IPv6Primary))
                {
                    results.Add(new Result()
                    {
                        Title = intInfo.IPv6Primary,
                        SubTitle = string.Format(CultureInfo.InvariantCulture, Resources.ResourceManager.GetString("Microsoft_plugin_sys_ip6_description", culture), intInfo.ConnectionName) + " - " + Resources.ResourceManager.GetString("Microsoft_plugin_sys_SubTitle_CopyHint", culture),
                        IcoPath = $"Images\\networkAdapter.{iconTheme}.png",
                        ToolTipData = new ToolTipData(Resources.Microsoft_plugin_sys_ConnectionDetails, intInfo.GetConnectionDetails()),
                        ContextData = new SystemPluginContext { Type = ResultContextType.NetworkAdapterInfo, Data = intInfo.GetConnectionDetails() },
                        Action = _ => ResultHelper.CopyToClipBoard(intInfo.IPv6Primary),
                    });
                }

                if (!string.IsNullOrEmpty(intInfo.PhysicalAddress))
                {
                    results.Add(new Result()
                    {
                        Title = intInfo.PhysicalAddress,
                        SubTitle = string.Format(CultureInfo.InvariantCulture, Resources.ResourceManager.GetString("Microsoft_plugin_sys_mac_description", culture), intInfo.Adapter, intInfo.ConnectionName) + " - " + Resources.ResourceManager.GetString("Microsoft_plugin_sys_SubTitle_CopyHint", culture),
                        IcoPath = $"Images\\networkAdapter.{iconTheme}.png",
                        ToolTipData = new ToolTipData(Resources.Microsoft_plugin_sys_AdapterDetails, intInfo.GetAdapterDetails()),
                        ContextData = new SystemPluginContext { Type = ResultContextType.NetworkAdapterInfo, Data = intInfo.GetAdapterDetails() },
                        Action = _ => ResultHelper.CopyToClipBoard(intInfo.PhysicalAddress),
                    });
                }
            }

            return results;
        }
    }
}
