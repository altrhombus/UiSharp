---
layout: article
title: Default Values
aside:
  toc: true
sidebar:
  nav: docs
---

The following reference lists all of the variables and their values that the [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action collects organized by category. Example values are included where applicable. Value lists prefixed with an asterisk (*) are the only possible values for that variable. If a variable within a category configured for collection is not applicable to a system, its value is set to blank.

<i class="fa fa-info-circle"></i> The variable names all begin with the letter X to avoid any conflicts with MDT variables (or any other custom variable names). Many of these variables contain the same information as MDT variables and in fact many extract data from the same locations and so may be redundant if you are using MDT. Because of the naming difference, there should be no conflicts, however.
{:.note}

## Asset

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|	
| XCPUArchitecture     | The architecture of the CPU | * x86, x64 |
| XCPULogicalCount      | The number of logical CPUs in the system | 8, 4, 2 |
| XCPUNX[^1]          | Whether the CPU supports No Execute (NX) functionality | * True, False |
| XCPUPAE[^1]         | Whether the CPU supports Physical Address Extensions (PAE) | * True, False |
| XCPUSSE2[^1]        | Whether the CPU supports SSE2 | * True, False |
| XCPUVendor          | The vendor of the CPU | * GenuineIntel, AuthenticIntel1 |
| XHWAssetTag           | The asset tag of the hardware | Various based on the hardware vendor |
| XHWChassisType      | The chassis type of the system | * Desktop, Laptop, Server, Tablet, Unknown |
| XHWLenovoModel        | The Lenovo system model | Various based on the hardware |
| XHWManufacturer       | The manufacturer of the hardware | Various based on the hardware vendor |
| XHWMemory             | The amount of physical memory in MB | 16329, 4096 |
| XHWModel              | The model of the hardware | Various based on the hardware vendor |
| XHWProduct            | The product name of the hardware | Various based on the hardware vendor |
| XHWSerialNumber       | The serial number of the hardware from the BIOS | Various based on the hardware vendor |
| XSystemDiskTotalSizeGB `3.0.1.0`{:.info} | The total size (in GB) of the system disk (identified using the %SystemDrive% environment variable) | 500.00, 256.00, 1024.00[^2] |
| XSystemDiskFreeSizeGB `3.0.1.0`{:.info} | The free space (in GB) on the system disk (identified using the %SystemDrive% environment variable) | 15.67, 189.12, 725.43[^2] |
| XHWUUID               | The UUID of the hardware |  |
| XOnBattery          | Whether the system is currently on battery or not | * True, False |
| XSystemUEFI         | Whether the system is in UEFI mode or not | * True, False |
| XSystemSecureBoot   | Whether Secure Boot is enabled or not | * True, False |

[^1]: PAE, NX, and SSE2 are required for Win 8 (and above) and WinPE 4.0 (and above)
[^2]: These example values are shown with a US decimal separator; actual values will include the proper decimal separator based on the system's locale settings. See the note at the bottom of the [Conditions]({% link docs/configuration/conditions.md %}) page for how to properly use these values in a VBScript comparison such as one used for a UI++ Condition.

## Domain

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XCurrentComputerJoinedToDomain | If the system is currently joined to an AD domain | * True, False |
| XCurrentComputerDomain | The current domain of the computer | Unbounded set |
| XCurrentComputerName | The current name of the computer | Unbounded set |
| XCurrentComputerOUDN | The current distinguished name of the computer’s OU in Active Directory | Unbounded set |
| XComputerAzureDomainJoined | Whether the system is Azure AD domain joined | * True, False |
| XComputerAzureDomainRegistered | Whether the system is Azure AD registered by the current user | * True, False |
| XComputerAzureTenantId | The Tenant ID that the system is joined or registered to | Unbounded set |
| XComputerAzureUser | The user that registered or joined the system to Azure AD | Unbounded set |
| XComputerAzureDomain | The Azure AD name that the system is joined or registered to | Unbounded set |

## Mgmt

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XServiceStartModeSMSAgentHost | The start mode of the ConfigMgr agent service | * Auto, Manual, Disabled |
| XServiceStateSMSAgentHost | The state of the ConfigMgr agent service | * Stopped, Start Pending, Stop, Continue Pending, Pause Pending, Paused, Unknown |
| XConfigMgrSiteCode | The ConfigMgr site code that the agent is assigned to | Unbounded set |
| XConfigMgrAgentVersion | The version of the ConfigMgr agent | Various |
| XConfigMgrCurrentMP | The current MP that the ConfigMgr agent is assigned to | Unbounded set |
| XMDMAuthorityName | The name of the MDM authority that the system is managed by | Unbounded set |

## Net

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XIPGateway | A list of IP gateways for the system | 192.168.1.1 |
| XMACAddress | A list of MAC Addresses for the system | 01:23:45:67:89:AB |
| XIPSubnetMask | A list of IP subnet masks for the system | 255.255.255.0 |
| XWLANDisconnected | Whether the system is connected to a wireless network or not | * True, False |
| XWLANSSID | The SSID for the wireless network the system is connected to | Unbounded set |
| XWiredLANConnected | If a wired network connection is detected | * True, False |

## OS

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
 | XOSArchitecture | The architecture of the OS | * x86, x64 |
 | XOSBuild | The build number of the OS | 9200, 2600 |
 | XOSProduct | The OS product type | * Workstation, Server |
 | XOSServerCore | Is the OS product running server core | * True, False |
 | XOSServicePack | The OS (major[^3]) service pack number | 1, 2 |
 | XOSSystemDrive | The system drive | C: |
 | XOSVersion | The Windows OS version | 6.2, 5.1 |
 | XOSWinPE | Is the OS WinPE – indicates whether the TS is currently in WinPE | * True, False |
 | XOSCBSRebootPending | A reboot is pending because of a CBS maintenance action | * True, False |
 | XOSWUARebootPending | A reboot is pending because of a Windows Update action. | * True, False |
 | XOSPendingFileRenameOperations | A reboot is pending because of a file rename operation | * True, False |
 | XOSPendingComputerRename | A reboot is pending because of a computer rename | * True, False |
 | XOSCCMRebootPending | A reboot is pending because of a ConfigMgr action | * True, False |
 | XOSRebootPending | A reboot is pending for any of the previous five reasons | * True, False |

[^3]: To my knowledge, no OS service pack has ever used a minor number other than zero.  

## Security

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XSystemDriveBitLockerProtected | Whether the system drive is protected by BitLocker | * 0, 1, 2[^4] |
| XServiceStateWindowsFirewall | The start mode of the Windows Firewall service | * Auto, Manual, Disabled |
| XServiceStartModeWindowsFirewall | The state of the Windows Firewall service | * Stopped, Start Pending, Stop, Continue Pending, Pause Pending, Paused, Unknown |
| XFirewallEnabledDomain | Whether the Windows Firewall is enabled for the Domain Profile | * True, False |
| XFirewallInboundDomain | The blocking policy for inbound traffic for the Windows Firewall Domain Profile | * True, False |
| XFirewallOutboundDomain | The blocking policy for outbound traffic for the Windows Firewall Domain Profile | * True, False |
| XFirewallEnabledPrivate | Whether the Windows Firewall is enabled for the Private Profile | * True, False |
| XFirewallInbounPrivate | The blocking policy for inbound traffic for the Windows Firewall Private Profile | * True, False |
| XFirewallOutboundPrivate | The blocking policy for outbound traffic for the Windows Firewall Private Profile | * True, False |
| XFirewallEnabledPublic | Whether the Windows Firewall is enabled for the Public Profile | * True, False |
| XFirewallInboundPublic | The blocking policy for inbound traffic for the Windows Firewall Public Profile | * True, False |
| XFirewallOutboundPublic | The blocking policy for outbound traffic for the Windows Firewall Public Profile | * True, False |
| XFirewallCurrentProfiles  | The currently active Windows Firewall profiles | Public, Domain, Private |
| XServiceStateWindowsDefenderAntivirusService | The start mode of the Windows Defender service | * Auto, Manual, Disabled |
| XServiceStartModeWindowsDefenderAntivirusService | The state of the Windows Defender service | * Stopped, Start Pending, Stop, Continue Pending, Pause Pending, Paused, Unknown |
| XDefenderAVEnabled | Whether Windows Defender anti-virus is enabled | * True, False |
| XDefenderASEnabled | Whether Windows Defender anti-spyware is enabled | * True, False |
| XDefenderNISEnabled | Whether Windows Defender network intrusion is enabled | * True, False |
| XDefenderFullScanAge | The number of days since the last full Windows Defender scan | Number |
| XDefenderAVSignatureAge | The age of the Windows Defender signatures (in days) | Number |
| XDefenderEngineVersion | The version of the Windows Defender scan engine | Various |
| XServiceStateWindowsUpdate | The start mode of the Windows Update service | * Auto, Manual, Disabled |
| XServiceStartModeWindowsUpdate | The state of the Windows Update service | * Stopped, Start Pending, Stop, Continue Pending, Pause Pending, Paused, Unknown |
| XWindowsUpdatesEnabled | Whether Windows updates is enabled | * True, False |
| XWindowsUpdateDefaultService | What service Windows updates is using | Various |
| XWindowsUpdateServer | The WSUS server that Windows updates is using (if any) | Unbounded set |

[^4]: See the defintion of the [ProtectionStatus](https://msdn.microsoft.com/en-us/library/windows/desktop/aa376483(v=vs.85).aspx#properties) attribute for the Win32_EncryptableVolume WMI class for the meaning of these values.

## TPM

All values within the TPM category require boot images of WinPE 4.0 or higher or the proper driver loaded in a full instance of Windows.

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XTPMEnabled | Whether the TPM chip is enabled | * True, False |
| XTPMActivated | Whether the TPM chip is activated | * True, False |
| XTPMAvailable | Whether a TPM chip was detected | * True, False |
| XTPMOwned | Whether the TPM chip is owned | * True, False |
| XTPMSpecVersion[^5] | The specification version of the TPM chip | 2.0, 0, 1.16 |

[^5]: See the defintion of the [SpecVersion](https://msdn.microsoft.com/en-us/library/windows/desktop/aa376484%28v=vs.85%29.aspx#properties) attribute for the Win32_Tpm WMI class for the details and meaning of this value.

## User

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XUserDisplayName | The display name for current user from Active Directory or AzureAD | Unbounded set |
| XUserSAMAccountName | The SAM account name (including domain) for the current user | Unbounded set |
| XUserPrincipalName | The user principal name for the current user | Unbounded set |

## VM

| Variable              | Description          | Example/Possible Values |
|:----------------------|:---------------------|:------------------------|
| XHWVirtualMachineType | Specifies the type of virtualization software used for the current systems | * Hyper-V, VMWareESX, VMWareOther, VirtualBox, Xen, OtherMicrosoft |