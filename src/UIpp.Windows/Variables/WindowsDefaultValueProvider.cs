using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using UIpp.Core.Configuration;
using UIpp.Core.Logging;
using UIpp.Core.Variables;

namespace UIpp.Windows.Variables;

// Implements IDefaultValueProvider by collecting hardware, OS, network, TPM,
// domain, security, and management info from WMI, registry, and Win32 APIs.
public sealed class WindowsDefaultValueProvider : IDefaultValueProvider
{
    private const string C = @"root\cimv2";

    public IReadOnlySet<string> SupportedCategories { get; } =
        new HashSet<string>(XmlConstants.DefaultValueCategories.Ordered,
                            StringComparer.OrdinalIgnoreCase);

    public void Collect(string category, ITSEnv env, ICMLog log)
    {
        switch (category.ToUpperInvariant())
        {
            case "OS":       PopulateOs(env);       break;
            case "ASSET":    PopulateAsset(env);    break;
            case "VM":       PopulateVm(env);       break;
            case "DOMAIN":   PopulateDomain(env);   break;
            case "NET":      PopulateNetwork(env);  break;
            case "TPM":      PopulateTpm(env);      break;
            case "SECURITY": PopulateSecurity(env); break;
            case "USER":     PopulateUser(env);     break;
            case "MGMT":     PopulateMgmt(env, log); break;
        }
    }

    // -------------------------------------------------------------------------
    // OS
    // -------------------------------------------------------------------------

    private static void PopulateOs(ITSEnv env)
    {
        using var os = WmiFirst(C, "Win32_OperatingSystem");
        if (os is not null)
        {
            env.Set(XmlConstants.Variables.OsVersion,     os["Version"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.OsBuild,       os["BuildNumber"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.OsServicePack, os["ServicePackMajorVersion"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.OsSystemDrive, os["SystemDrive"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.OsArch,
                os["OSArchitecture"]?.ToString()?.Contains("64") == true
                    ? XmlConstants.Values.ArchX64 : XmlConstants.Values.ArchX86);

            var productType = Convert.ToInt32(os["ProductType"] ?? 1);
            env.Set(XmlConstants.Variables.OsProduct, productType switch
            {
                2 => XmlConstants.Values.OsDomainController,
                3 => XmlConstants.Values.OsServer,
                _ => XmlConstants.Values.OsWorkstation,
            });
        }

        // WinPE: HKLM\System\CurrentControlSet\Control\MiniNT exists only in WinPE.
        env.Set(XmlConstants.Variables.IsWinPe,
            Bool(RegKeyExists(@"SYSTEM\CurrentControlSet\Control\MiniNT")));

        // Server Core: no explorer.exe.
        var explorer = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        env.Set(XmlConstants.Variables.IsServerCoreOs, Bool(!File.Exists(explorer)));

        // UEFI.
        env.Set(XmlConstants.Variables.IsUefi, Bool(IsUefiFirmware()));

        // SecureBoot.
        var sb = RegValue<int?>(
            @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled");
        env.Set(XmlConstants.Variables.IsSecureBoot, Bool(sb == 1));

        // Reboot pending.
        env.Set(XmlConstants.Variables.OsCbsRebootPending,
            Bool(RegKeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending")));
        env.Set(XmlConstants.Variables.OsWuaRebootPending,
            Bool(RegKeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired")));
        var pfro = RegValue<string[]?>(
            @"SYSTEM\CurrentControlSet\Control\Session Manager", "PendingFileRenameOperations");
        env.Set(XmlConstants.Variables.OsPfro, Bool(pfro?.Length > 0));

        var pending = RegValue<string?>(
            @"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName", "ComputerName");
        var active  = RegValue<string?>(
            @"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName", "ComputerName");
        env.Set(XmlConstants.Variables.OsPendingRename,
            Bool(pending is not null && active is not null &&
                 !pending.Equals(active, StringComparison.OrdinalIgnoreCase)));

        // ConfigMgr reboot.
        bool ccmReboot = false;
        TryRun(() =>
        {
            var scope = new ManagementScope(@"root\ccm\ClientSDK");
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT IsRebootPending FROM CCM_ClientUtilities"));
            ccmReboot = s.Get().Cast<ManagementObject>()
                .Any(o => Convert.ToBoolean(o["IsRebootPending"] ?? false));
        });
        env.Set(XmlConstants.Variables.CcmRebootPending, Bool(ccmReboot));

        var anyReboot =
            env.Get(XmlConstants.Variables.OsCbsRebootPending)  == XmlConstants.Values.True ||
            env.Get(XmlConstants.Variables.OsWuaRebootPending)  == XmlConstants.Values.True ||
            env.Get(XmlConstants.Variables.OsPfro)              == XmlConstants.Values.True ||
            env.Get(XmlConstants.Variables.OsPendingRename)     == XmlConstants.Values.True ||
            ccmReboot;
        env.Set(XmlConstants.Variables.RebootPending, Bool(anyReboot));
    }

    // -------------------------------------------------------------------------
    // Asset / Hardware
    // -------------------------------------------------------------------------

    private static void PopulateAsset(ITSEnv env)
    {
        using var cs = WmiFirst(C, "Win32_ComputerSystem");
        if (cs is not null)
        {
            env.Set(XmlConstants.Variables.Model,        cs["Model"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.Manufacturer, cs["Manufacturer"]?.ToString() ?? string.Empty);
            var mem = Convert.ToUInt64(cs["TotalPhysicalMemory"] ?? 0UL);
            env.Set(XmlConstants.Variables.Memory, (mem / (1024 * 1024)).ToString());
        }

        using var bios = WmiFirst(C, "Win32_BIOS");
        if (bios is not null)
            env.Set(XmlConstants.Variables.SerialNumber, bios["SerialNumber"]?.ToString() ?? string.Empty);

        using var enc = WmiFirst(C, "Win32_SystemEnclosure");
        if (enc is not null)
        {
            env.Set(XmlConstants.Variables.AssetTag, enc["SMBIOSAssetTag"]?.ToString() ?? string.Empty);
            var types = enc["ChassisTypes"] as ushort[];
            env.Set(XmlConstants.Variables.ChassisType, MapChassis(types?.FirstOrDefault() ?? 0));
        }

        using var prod = WmiFirst(C, "Win32_ComputerSystemProduct");
        if (prod is not null)
        {
            env.Set(XmlConstants.Variables.Uuid,        prod["UUID"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.LenovoModel, prod["Version"]?.ToString() ?? string.Empty);
        }

        using var board = WmiFirst(C, "Win32_BaseBoard");
        if (board is not null)
            env.Set(XmlConstants.Variables.Product, board["Product"]?.ToString() ?? string.Empty);

        using var cpu = WmiFirst(C, "Win32_Processor");
        if (cpu is not null)
        {
            var width = Convert.ToInt32(cpu["DataWidth"] ?? 32);
            env.Set(XmlConstants.Variables.CpuArch, width == 64 ? XmlConstants.Values.ArchX64 : XmlConstants.Values.ArchX86);
            env.Set(XmlConstants.Variables.CpuVendor,       cpu["Manufacturer"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.CpuLogicalCount, cpu["NumberOfLogicalProcessors"]?.ToString() ?? string.Empty);
        }

        // Disk.
        var sysRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
        var di = new DriveInfo(sysRoot);
        if (di.IsReady)
        {
            env.Set(XmlConstants.Variables.SystemDiskTotal, (di.TotalSize        / (1024L * 1024 * 1024)).ToString());
            env.Set(XmlConstants.Variables.SystemDiskFree,  (di.AvailableFreeSpace / (1024L * 1024 * 1024)).ToString());
        }

        using var bat = WmiFirst(C, "Win32_Battery");
        env.Set(XmlConstants.Variables.OnBattery,
            Bool(bat is not null && Convert.ToInt32(bat["BatteryStatus"] ?? 2) != 2));

        GetCpuFeatures(env);
    }

    private static string MapChassis(ushort type) => type switch
    {
        8 or 9 or 10 or 11 or 12 or 14 or 18 or 21 => XmlConstants.Values.ChassisLaptop,
        30 or 31 or 32                              => XmlConstants.Values.ChassisTablet,
        1 or 13 or 15 or 16 or 17 or 23 or 24      => XmlConstants.Values.ChassisServer,
        _                                           => XmlConstants.Values.ChassisDesktop,
    };

    [DllImport("kernel32.dll")]
    private static extern bool GetFirmwareType(out uint firmwareType);

    private static bool IsUefiFirmware()
    {
        try { return GetFirmwareType(out var t) && t == 2; }
        catch { return false; }
    }

    private static void GetCpuFeatures(ITSEnv env)
    {
        try
        {
            var (_, _, _, edx) = System.Runtime.Intrinsics.X86.X86Base.CpuId(1, 0);
            env.Set(XmlConstants.Variables.CpuSse2, Bool((edx & (1 << 26)) != 0));
            env.Set(XmlConstants.Variables.CpuPae,  Bool((edx & (1 << 6))  != 0));

            var (_, _, _, edx2) = System.Runtime.Intrinsics.X86.X86Base.CpuId(unchecked((int)0x80000001), 0);
            env.Set(XmlConstants.Variables.CpuNx, Bool((edx2 & (1 << 20)) != 0));
        }
        catch
        {
            env.Set(XmlConstants.Variables.CpuSse2, XmlConstants.Values.Unknown);
            env.Set(XmlConstants.Variables.CpuNx,   XmlConstants.Values.Unknown);
            env.Set(XmlConstants.Variables.CpuPae,  XmlConstants.Values.Unknown);
        }
    }

    // -------------------------------------------------------------------------
    // Virtualization
    // -------------------------------------------------------------------------

    private static void PopulateVm(ITSEnv env)
    {
        var model = env.Get(XmlConstants.Variables.Model).ToUpperInvariant();
        var mfr   = env.Get(XmlConstants.Variables.Manufacturer).ToUpperInvariant();

        if (string.IsNullOrEmpty(model))
        {
            using var cs = WmiFirst(C, "Win32_ComputerSystem");
            model = (cs?["Model"]?.ToString() ?? string.Empty).ToUpperInvariant();
            mfr   = (cs?["Manufacturer"]?.ToString() ?? string.Empty).ToUpperInvariant();
        }

        string vmType = string.Empty;

        if (mfr.Contains("MICROSOFT") && (model.Contains("VIRTUAL") || model.Contains("HYPER-V")))
            vmType = XmlConstants.Values.VmHyperV;
        else if (model.Contains("VMWARE") && model.Contains("ESX"))
            vmType = XmlConstants.Values.VmVmwareEsx;
        else if (model.Contains("VMWARE") || mfr.Contains("VMWARE"))
            vmType = XmlConstants.Values.VmVmware;
        else if (model.Contains("VIRTUALBOX"))
            vmType = XmlConstants.Values.VmVirtualBox;
        else if (mfr.Contains("XEN"))
            vmType = XmlConstants.Values.VmXen;
        else if (mfr.Contains("MICROSOFT"))
            vmType = XmlConstants.Values.VmOtherMs;

        env.Set(XmlConstants.Variables.VmType, vmType);
        if (!string.IsNullOrEmpty(vmType))
            env.Set(XmlConstants.Variables.ChassisType, XmlConstants.Values.ChassisVirtual);
    }

    // -------------------------------------------------------------------------
    // Domain
    // -------------------------------------------------------------------------

    private static void PopulateDomain(ITSEnv env)
    {
        env.Set(XmlConstants.Variables.CurrentComputerName, Environment.MachineName);

        using var cs = WmiFirst(C, "Win32_ComputerSystem");
        if (cs is not null)
        {
            env.Set(XmlConstants.Variables.ComputerJoinedToDomain,
                Bool(Convert.ToBoolean(cs["PartOfDomain"] ?? false)));
            env.Set(XmlConstants.Variables.CurrentDomain, cs["Domain"]?.ToString() ?? string.Empty);
        }

        // Strip the CN=ComputerName, prefix to get just the OU path (matching C++ behavior).
        var dn = RegValue<string?>(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy\State\Machine",
            "Distinguished-Name") ?? string.Empty;
        var commaIdx = dn.IndexOf(',');
        env.Set(XmlConstants.Variables.CurrentComputerOuDn,
            commaIdx > 0 ? dn[(commaIdx + 1)..] : string.Empty);

        // Azure AD domain join.
        var aadPath = FindSubkeyWithValue(
            Registry.LocalMachine,
            @"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo", "TenantId");
        env.Set(XmlConstants.Variables.AzureDomainJoined,   Bool(aadPath is not null));
        env.Set(XmlConstants.Variables.AzureTenantId,       ReadRegSubValue(aadPath, "TenantId"));
        env.Set(XmlConstants.Variables.AzureDomain,         ReadRegSubValue(aadPath, "DisplayName"));
        env.Set(XmlConstants.Variables.AzureUser,           ReadRegSubValue(aadPath, "UserEmail"));

        // Workplace join (HKCU, like C++ which connects HKEY_CURRENT_USER for this case).
        var wpPath = FindSubkeyWithValue(
            Registry.CurrentUser,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WorkplaceJoin\JoinInfo", "TenantId");
        env.Set(XmlConstants.Variables.AzureDomainRegistered, Bool(wpPath is not null));
    }

    private static string? FindSubkeyWithValue(RegistryKey hive, string keyPath, string valueName)
    {
        try
        {
            using var key = hive.OpenSubKey(keyPath, writable: false);
            if (key is null) return null;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var child = key.OpenSubKey(sub, writable: false);
                if (child?.GetValue(valueName) is not null) return $@"{keyPath}\{sub}";
            }
        }
        catch { }
        return null;
    }

    private static string ReadRegSubValue(string? path, string valueName)
    {
        if (path is null) return string.Empty;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path, writable: false);
            return key?.GetValue(valueName)?.ToString() ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    // -------------------------------------------------------------------------
    // Network — WLAN P/Invoke (dynamic load; graceful no-op when WlanApi.dll absent)
    // -------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid    InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string? Description;
        public uint    IsState;   // WLAN_INTERFACE_STATE; 1 = connected
    }

    [DllImport("WlanApi.dll", ExactSpelling = true)] private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);
    [DllImport("WlanApi.dll", ExactSpelling = true)] private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);
    [DllImport("WlanApi.dll", ExactSpelling = true)] private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);
    [DllImport("WlanApi.dll", ExactSpelling = true)] private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, out uint pWlanOpcodeValueType);
    [DllImport("WlanApi.dll", ExactSpelling = true)] private static extern void WlanFreeMemory(IntPtr pMemory);

    // Returns the connected WLAN SSID(s), semicolon-separated for multiple adapters.
    // Matches C++ WlanApi.dll dynamic-load pattern; returns empty string on any failure
    // (including WlanApi.dll not present in WinPE without the Wireless optional component).
    private static string GetWlanSsid()
    {
        try
        {
            if (WlanOpenHandle(2, IntPtr.Zero, out _, out var hClient) != 0)
                return string.Empty;
            try
            {
                if (WlanEnumInterfaces(hClient, IntPtr.Zero, out var pList) != 0)
                    return string.Empty;
                try
                {
                    int count   = Marshal.ReadInt32(pList, 0);
                    int infoSz  = Marshal.SizeOf<WlanInterfaceInfo>();
                    var ssids   = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var info = Marshal.PtrToStructure<WlanInterfaceInfo>(IntPtr.Add(pList, 8 + i * infoSz));
                        if (info.IsState != 1) continue; // wlan_interface_state_connected
                        var guid = info.InterfaceGuid;
                        if (WlanQueryInterface(hClient, ref guid, 7 /*wlan_intf_opcode_current_connection*/,
                                IntPtr.Zero, out _, out var pConn, out _) != 0)
                            continue;
                        try
                        {
                            // WLAN_CONNECTION_ATTRIBUTES layout:
                            //   isState(4) + wlanConnectionMode(4) + strProfileName(WCHAR[256]=512)
                            //   = 520 bytes before wlanAssociationAttributes.dot11Ssid
                            // dot11Ssid: uSSIDLength(4 at +520) + ucSSID[32](at +524)
                            int ssidLen = Marshal.ReadInt32(pConn, 520);
                            if (ssidLen > 0 && ssidLen <= 32)
                            {
                                var bytes = new byte[ssidLen];
                                Marshal.Copy(IntPtr.Add(pConn, 524), bytes, 0, ssidLen);
                                ssids.Add(Encoding.UTF8.GetString(bytes));
                            }
                        }
                        finally { WlanFreeMemory(pConn); }
                    }
                    return string.Join(";", ssids);
                }
                finally { WlanFreeMemory(pList); }
            }
            finally { WlanCloseHandle(hClient, IntPtr.Zero); }
        }
        catch (DllNotFoundException)        { return string.Empty; }
        catch (EntryPointNotFoundException) { return string.Empty; }
    }

    // -------------------------------------------------------------------------
    // Network
    // -------------------------------------------------------------------------

    private static void PopulateNetwork(ITSEnv env)
    {
        using var searcher = new ManagementObjectSearcher(C,
            "SELECT IPAddress,IPSubnet,DefaultIPGateway,MACAddress " +
            "FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True");
        using var col = searcher.Get();

        ManagementObject? best = null;
        foreach (var obj in col.Cast<ManagementObject>())
        {
            if ((obj["DefaultIPGateway"] as string[])?.Length > 0) { best = obj; break; }
            best ??= obj;
        }

        if (best is not null)
        {
            env.Set(XmlConstants.Variables.IpAddress,
                (best["IPAddress"] as string[])?.FirstOrDefault(a => !a.Contains(':')) ?? string.Empty);
            env.Set(XmlConstants.Variables.IpSubnetMask,
                (best["IPSubnet"] as string[])?.FirstOrDefault() ?? string.Empty);
            env.Set(XmlConstants.Variables.IpGateway,
                (best["DefaultIPGateway"] as string[])?.FirstOrDefault() ?? string.Empty);
            env.Set(XmlConstants.Variables.MacAddress,
                best["MACAddress"]?.ToString() ?? string.Empty);
        }

        var nics     = NetworkInterface.GetAllNetworkInterfaces();
        var hasWired = nics.Any(n =>
            n.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
            n.OperationalStatus    == OperationalStatus.Up);
        env.Set(XmlConstants.Variables.WiredLanConnected, Bool(hasWired));

        var wifi = nics.FirstOrDefault(n =>
            n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
            n.OperationalStatus    == OperationalStatus.Up);
        env.Set(XmlConstants.Variables.WlanDisconnected, Bool(wifi is null));
        env.Set(XmlConstants.Variables.WlanSsid, GetWlanSsid());
    }

    // -------------------------------------------------------------------------
    // TPM
    // -------------------------------------------------------------------------

    private static void PopulateTpm(ITSEnv env)
    {
        const string ns = @"root\cimv2\Security\MicrosoftTpm";
        try
        {
            var scope = new ManagementScope(ns);
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM Win32_Tpm"));
            var instances = s.Get().Cast<ManagementObject>().ToList();

            env.Set(XmlConstants.Variables.TpmAvailable, Bool(instances.Count > 0));
            if (instances.Count == 0)
            {
                env.Set(XmlConstants.Variables.TpmEnabled,     XmlConstants.Values.False);
                env.Set(XmlConstants.Variables.TpmActivated,   XmlConstants.Values.False);
                env.Set(XmlConstants.Variables.TpmOwned,       XmlConstants.Values.False);
                env.Set(XmlConstants.Variables.TpmSpecVersion, string.Empty);
                return;
            }

            using var tpm = instances[0];
            env.Set(XmlConstants.Variables.TpmSpecVersion, tpm["SpecVersion"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.TpmEnabled,   TpmBool(tpm, "IsEnabled"));
            env.Set(XmlConstants.Variables.TpmActivated, TpmBool(tpm, "IsActivated"));
            env.Set(XmlConstants.Variables.TpmOwned,     TpmBool(tpm, "IsOwned"));
        }
        catch
        {
            env.Set(XmlConstants.Variables.TpmAvailable, XmlConstants.Values.False);
        }
    }

    private static string TpmBool(ManagementObject tpm, string method)
    {
        try
        {
            using var r  = tpm.InvokeMethod(method, null, null);
            var key = char.ToLowerInvariant(method[2]) + method[3..]; // "IsEnabled" → "isEnabled"
            return Bool(r?[key] is true);
        }
        catch { return XmlConstants.Values.False; }
    }

    // -------------------------------------------------------------------------
    // Security
    // -------------------------------------------------------------------------

    private static void PopulateSecurity(ITSEnv env)
    {
        // BitLocker — C++ sets raw ProtectionStatus integer (0=Unprotected,1=Protected,2=Unknown).
        TryRun(() =>
        {
            var scope = new ManagementScope(
                @"root\cimv2\Security\MicrosoftVolumeEncryption");
            scope.Connect();
            var sysRoot = (Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\").TrimEnd('\\');
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery($"SELECT ProtectionStatus FROM Win32_EncryptableVolume WHERE DriveLetter='{sysRoot}'"));
            var status = s.Get().Cast<ManagementObject>()
                .Select(o => Convert.ToInt32(o["ProtectionStatus"] ?? 0))
                .FirstOrDefault();
            env.Set(XmlConstants.Variables.SystemDriveBitlocker, status.ToString());
        });

        PopulateFirewall(env);
        PopulateWindowsUpdate(env);

        // Defender.
        TryRun(() =>
        {
            var scope = new ManagementScope(@"root\Microsoft\Windows\Defender");
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM MSFT_MpComputerStatus"));
            var mp = s.Get().Cast<ManagementObject>().FirstOrDefault();
            if (mp is null) return;
            env.Set(XmlConstants.Variables.DefenderAvEnabled,   Bool(Convert.ToBoolean(mp["AntivirusEnabled"]   ?? false)));
            env.Set(XmlConstants.Variables.DefenderAsEnabled,   Bool(Convert.ToBoolean(mp["AntispywareEnabled"] ?? false)));
            env.Set(XmlConstants.Variables.DefenderNisEnabled,  Bool(Convert.ToBoolean(mp["NISEnabled"]         ?? false)));
            env.Set(XmlConstants.Variables.DefenderEngineVersion, mp["AMEngineVersion"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.DefenderFullScanAge,   mp["FullScanAge"]?.ToString() ?? string.Empty);
            env.Set(XmlConstants.Variables.DefenderAvSigAge,      mp["AntivirusSignatureAge"]?.ToString() ?? string.Empty);
        });
    }

    private static void PopulateFirewall(ITSEnv env)
    {
        GetServiceInfo(env, "MpsSvc");
        TryRun(() =>
        {
            const string ns = @"root\StandardCimv2";
            var scope = new ManagementScope(ns);
            scope.Connect();

            // Per-profile firewall state and default actions.
            using var ps = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT Name,Enabled,DefaultInboundAction,DefaultOutboundAction FROM MSFT_NetFirewallProfile"));
            using var pc = ps.Get();
            foreach (var obj in pc.Cast<ManagementObject>())
            {
                using var p = obj;
                var name     = p["Name"]?.ToString() ?? string.Empty;
                var enabled  = Convert.ToInt32(p["Enabled"]              ?? 0);
                var inbound  = Convert.ToInt32(p["DefaultInboundAction"]  ?? 0);
                var outbound = Convert.ToInt32(p["DefaultOutboundAction"] ?? 0);
                env.Set($"{XmlConstants.Variables.FirewallEnabled}{name}",  Bool(enabled == 1));
                env.Set($"{XmlConstants.Variables.FirewallInbound}{name}",  MapFirewallAction(inbound));
                env.Set($"{XmlConstants.Variables.FirewallOutbound}{name}", MapFirewallAction(outbound));
            }

            // Active profiles: enumerate current network connection categories.
            using var cs = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT NetworkCategory FROM MSFT_NetConnectionProfile"));
            using var cc = cs.Get();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var obj in cc.Cast<ManagementObject>())
            {
                using var conn = obj;
                var profileName = Convert.ToInt32(conn["NetworkCategory"] ?? -1) switch
                {
                    2 => "Domain",
                    1 => "Private",
                    0 => "Public",
                    _ => string.Empty,
                };
                seen.Add(profileName);
            }
            seen.Remove(string.Empty);
            // Match C++ loop order: Domain, Private, Public.
            env.Set(XmlConstants.Variables.FirewallCurrentProfiles,
                string.Join(",", new[] { "Domain", "Private", "Public" }.Where(seen.Contains)));
        });
    }

    private static string MapFirewallAction(int wmiAction) => wmiAction switch
    {
        2 => "Allow", // MSFT_NetFirewallProfile: 2=Allow (NET_FW_ACTION_ALLOW)
        4 => "Block", // 4=Block (NET_FW_ACTION_BLOCK)
        _ => "Max",
    };

    private static void PopulateWindowsUpdate(ITSEnv env)
    {
        GetServiceInfo(env, "wuauserv");

        // XWindowsUpdatesEnabled: approximate via Group Policy registry key.
        // C++ uses COM IAutomaticUpdates.ServiceEnabled; we check NoAutoUpdate GP setting instead.
        // NoAutoUpdate=1 → disabled; absent or 0 → enabled.
        TryRun(() =>
        {
            var noAu = RegValue<int?>(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate");
            env.Set(XmlConstants.Variables.WuEnabled, Bool(noAu != 1));
        });

        // WSUS server (C++: only set when default service is WSUS; we always read the policy key).
        TryRun(() =>
            env.Set(XmlConstants.Variables.WuServer,
                RegValue<string?>(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "WUServer") ?? string.Empty));
    }

    // Sets XServiceState{DisplayName} and XServiceStartMode{DisplayName} for the named service.
    // Matches C++ GetServiceInfo: queries Win32_Service, strips spaces from DisplayName.
    private static void GetServiceInfo(ITSEnv env, string serviceName)
    {
        TryRun(() =>
        {
            using var s = new ManagementObjectSearcher(C,
                $"SELECT DisplayName,State,StartMode FROM Win32_Service WHERE Name='{serviceName}'");
            var svc = s.Get().Cast<ManagementObject>().FirstOrDefault();
            if (svc is null) return;
            var displayName = (svc["DisplayName"]?.ToString() ?? string.Empty)
                .Replace(" ", string.Empty);
            if (displayName.Length == 0) return;
            env.Set($"{XmlConstants.Variables.ServiceState}{displayName}",
                svc["State"]?.ToString() ?? string.Empty);
            env.Set($"{XmlConstants.Variables.ServiceStartMode}{displayName}",
                svc["StartMode"]?.ToString() ?? string.Empty);
        });
    }

    // -------------------------------------------------------------------------
    // User
    // -------------------------------------------------------------------------

    private static void PopulateUser(ITSEnv env)
    {
        // NameSamCompatible=2 gives DOMAIN\username (matches C++ GetUserNameExW(NameSamCompatible,...))
        env.Set(XmlConstants.Variables.UserSamAccount,   GetUserNameEx(2) ?? Environment.UserName);
        env.Set(XmlConstants.Variables.UserPrincipalName, GetUserNameEx(8) ?? string.Empty);
        env.Set(XmlConstants.Variables.UserDisplayName,   GetUserNameEx(3) ?? string.Empty);

        using var identity  = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        env.Set(XmlConstants.Variables.UserIsLocalAdmin,
            Bool(principal.IsInRole(WindowsBuiltInRole.Administrator)));
    }

    [DllImport("secur32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserNameEx(int nameFormat, StringBuilder lpNameBuffer, ref uint nSize);

    private static string? GetUserNameEx(int format)
    {
        uint size = 512;
        var sb = new StringBuilder((int)size);
        return GetUserNameEx(format, sb, ref size) ? sb.ToString() : null;
    }

    // -------------------------------------------------------------------------
    // Mgmt
    // -------------------------------------------------------------------------

    private static void PopulateMgmt(ITSEnv env, ICMLog log)
    {
        TryRun(() =>
        {
            var scope = new ManagementScope(@"root\ccm");
            scope.Connect();
            using var cls  = new ManagementClass(scope, new ManagementPath("SMS_Client"), null);
            var inst = cls.GetInstances().Cast<ManagementObject>().FirstOrDefault();
            if (inst is null) return;
            env.Set(XmlConstants.Variables.CmAgentVersion, inst["ClientVersion"]?.ToString() ?? string.Empty);
            using var r = inst.InvokeMethod("GetAssignedSite", null, null);
            env.Set(XmlConstants.Variables.CmSiteCode, r?["sSiteCode"]?.ToString() ?? string.Empty);
        });

        TryRun(() =>
        {
            var scope = new ManagementScope(@"root\ccm");
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM SMS_Authority"));
            var auth = s.Get().Cast<ManagementObject>().FirstOrDefault();
            env.Set(XmlConstants.Variables.CmCurrentMp, auth?["CurrentManagementPoint"]?.ToString() ?? string.Empty);
        });

        TryRun(() =>
        {
            using var sc = new System.ServiceProcess.ServiceController("OmcSvc");
            env.Set(XmlConstants.Variables.IntuneAgentInstalled, Bool(true)); // if no exception, service exists
        });

        TryRun(() =>
        {
            var scope = new ManagementScope(@"root\cimv2\mdm");
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery("SELECT * FROM MDM_MgmtAuthority"));
            var auth = s.Get().Cast<ManagementObject>().FirstOrDefault();
            env.Set(XmlConstants.Variables.MdmAuthorityName, auth?["AuthorityName"]?.ToString() ?? string.Empty);
        });
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private static string Bool(bool v) =>
        v ? XmlConstants.Values.True : XmlConstants.Values.False;

    private static ManagementObject? WmiFirst(string ns, string cls)
    {
        try
        {
            var scope = new ManagementScope(ns);
            scope.Connect();
            using var s = new ManagementObjectSearcher(scope,
                new ObjectQuery($"SELECT * FROM {cls}"));
            return s.Get().Cast<ManagementObject>().FirstOrDefault();
        }
        catch { return null; }
    }

    private static bool RegKeyExists(string subKey)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: false);
            return key is not null;
        }
        catch { return false; }
    }

    private static T? RegValue<T>(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey, writable: false);
            var raw = key?.GetValue(valueName);
            if (raw is null) return default;
            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T?)Convert.ChangeType(raw, targetType);
        }
        catch { return default; }
    }

    private static void TryRun(Action a)
    {
        try { a(); }
        catch { /* category failures are non-fatal */ }
    }
}
