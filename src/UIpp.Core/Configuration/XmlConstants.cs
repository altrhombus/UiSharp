namespace UIpp.Core.Configuration;

public static class XmlConstants
{
    public const string DefaultConfigFilename = "UI++.xml";
    public const string DefaultFontFace       = "Tahoma";

    public static class Elements
    {
        public const string Root          = "UIpp";
        public const string Actions       = "Actions";
        public const string Action        = "Action";
        public const string Software      = "Software";
        public const string Message       = "Message";
        public const string Messages      = "Messages";
        public const string ActionGroup   = "ActionGroup";
        public const string Libraries     = "Libraries";
        public const string ActionLibrary   = "ActionLibrary";
        public const string Application     = "Application";
        public const string Package         = "Package";
        public const string Variable        = "Variable";       // child of Action, Switch Case/Default
        public const string Case            = "Case";           // Switch case
        public const string Default         = "Default";        // Switch default
        public const string Attribute       = "Attribute";      // ToJSON attribute child
        public const string SoftwareListRef = "SoftwareListRef"; // TSVarList reference
        public const string SoftwareSets  = "SoftwareSets";    // AppTree container
        public const string SoftwareSet   = "Set";             // AppTree conditional set
        public const string SoftwareRef   = "SoftwareRef";     // AppTree leaf reference
        public const string SoftwareGroup = "SoftwareGroup";   // AppTree group node
        public const string Choice          = "Choice";         // child of InputChoice
        public const string ChoiceList      = "ChoiceList";     // child of InputChoice (delimited lists)
        public const string PreflightCheck  = "Check";          // child of Preflight action
        public const string Property        = "Property";        // child of WmiWrite action
        public const string Field           = "Field";           // child of UserAuth action
    }

    public static class Attributes
    {
        public const string Type                = "Type";
        public const string Name                = "Name";
        public const string Image               = "Image";
        public const string AlwaysOnTop         = "AlwaysOnTop";
        public const string InfoImage           = "InfoImage";
        public const string Title               = "Title";
        public const string Icon                = "Icon";
        public const string DialogIcons         = "DialogIcons";
        public const string DialogSidebar       = "DialogSidebar";
        public const string SidebarTextColor    = "SidebarTextColor";
        public const string Flat                = "Flat";
        public const string Subtitle            = "Subtitle";
        public const string Info                = "Info";
        public const string Font                = "Font";
        public const string Color               = "Color";
        public const string TextColor           = "TextColor";
        public const string Text                = "Text";
        public const string Description         = "Description";
        public const string ErrorDescription    = "ErrorDescription";
        public const string WarnDescription     = "WarnDescription";
        public const string Condition           = "Condition";
        public const string ConditionEngine     = "ConditionEngine";
        public const string Default             = "Default";
        public const string Prompt              = "Prompt";
        public const string Hint                = "Hint";
        public const string RegEx               = "RegEx";
        public const string Size                = "Size";
        public const string Variable            = "Variable";
        public const string AlternateVariable   = "AlternateVariable";
        public const string Question            = "Question";
        public const string Option              = "Option";
        public const string Value               = "Value";
        public const string AlternateValue      = "AlternateValue";
        public const string Required            = "Required";
        public const string Namespace           = "Namespace";
        public const string Class               = "Class";
        public const string KeyQualifier        = "KeyQualifier";
        public const string Property            = "Property";
        public const string Path                = "Path";
        public const string Hive                = "Hive";
        public const string Key                 = "Key";
        public const string Domain              = "Domain";
        public const string DomainController    = "DomainController";
        public const string DoNotFallback       = "DoNotFallback";
        public const string MaxRetry            = "MaxRetryCount";
        public const string Group               = "Group";
        public const string GetGroups           = "GetGroups";
        public const string GetGroupsForUser    = "GetGroupsForUser";
        public const string Label               = "Label";
        public const string AppName             = "Name"; // <Application Name="..."> — same XML attribute name as Attributes.Name but used only on <Application> elements in <Software>
        public const string PkgId              = "PkgID";
        public const string SoftwareInfo        = "Info";
        public const string ProgramName         = "ProgramName";
        public const string Id                  = "Id";
        public const string PackageVarBase      = "PackageVariableBase";
        public const string AppVarBase          = "ApplicationVariableBase";
        public const string IncludeId           = "IncludeId";
        public const string ExcludeId           = "ExcludeId";
        public const string Hidden              = "Hidden";
        public const string NumberOfLines       = "NumberofLines";
        public const string CheckCondition      = "CheckCondition";
        public const string WarnCondition       = "WarnCondition";
        public const string Expanded            = "Expanded";
        public const string Reg64               = "Reg64";
        public const string RegData             = "RegData";
        public const string PropertyKey         = "Key";      // WmiWrite <Property Key="True"/>
        public const string PropertyType        = "Type";     // WmiWrite <Property Type="CIM_STRING"/>
        public const string ShowCancel          = "ShowCancel";
        public const string DisableCancel       = "DisableCancel";
        public const string ShowBack            = "ShowBack";
        public const string ShowProgress        = "ShowProgress";
        public const string Password            = "Password";
        public const string Username            = "Username";
        public const string Direction           = "Direction";
        public const string Filename            = "Filename";
        public const string DisplayName         = "DisplayName";
        public const string RefId               = "RefId";
        public const string SystemComponents    = "SystemComponents";
        public const string Version             = "Version";
        public const string VersionOperator     = "VersionOperator";
        public const string DefaultValueTypes   = "ValueTypes";
        public const string ReadOnly            = "ReadOnly";
        public const string AutoComplete        = "AutoComplete";
        public const string ShowOnFailureOnly   = "ShowOnFailureOnly";
        public const string Sort                = "Sort";
        public const string OnExpression        = "OnExpression";
        public const string OnValue             = "OnValue";
        public const string Query               = "Query";
        public const string ValueList           = "ValueList";
        public const string AlternateValueList  = "AlternateValueList";
        public const string OptionList          = "OptionList";
        public const string List                = "List";
        public const string Url                 = "Url";
        public const string DropDownSize        = "DropDownSize";
        public const string ForceCase           = "ForceCase";
        public const string Items               = "Items";
        public const string Method              = "Method";
        public const string RestAttributes      = "Attributes";
        public const string AdValidate          = "ADValidate";
        public const string HScroll             = "HScroll";
        public const string CenterTitle         = "CenterTitle";
        public const string AllowedChars        = "AllowedChars";
        public const string Length              = "Length";
        public const string NoDefaultButton     = "NoDefaultButton";
        public const string RegValueType        = "ValueType";
        public const string TpmRequest          = "Request";
        public const string DeleteLine          = "DeleteLine";
        public const string CommandLine         = "CommandLine";
        public const string MaxRunTime          = "MaxRunTime";
        public const string ExitCodeVariable    = "ExitCodeVariable";
        public const string DontEval            = "DontEval";
        public const string Timeout             = "Timeout";
        public const string TimeoutMsg          = "TimeoutMsg";
        public const string TimeoutAction       = "TimeoutAction";
        public const string CaseInsensitive     = "CaseInsensitive";
        public const string Json                = "JSON";
        public const string CheckedValue        = "CheckedValue";
        public const string UncheckedValue      = "UncheckedValue";
        public const string SchemaVersion       = "SchemaVersion";
    }

    public static class ActionTypes
    {
        public const string UserInput        = "Input";
        public const string UserInfo         = "Info";
        public const string UserInfoFull     = "InfoFullScreen";
        public const string ErrorInfo        = "ErrorInfo";
        public const string RegRead          = "RegRead";
        public const string RegWrite         = "RegWrite";
        public const string AppTree          = "AppTree";
        public const string WmiRead          = "WMIRead";
        public const string WmiWrite         = "WMIWrite";
        public const string TSVar            = "TSVar";
        public const string TSVarList        = "TSVarList";
        public const string DefaultValues    = "DefaultValues";
        public const string UserAuth         = "UserAuth";
        public const string Preflight        = "Preflight";
        public const string Vars             = "Vars";
        public const string SoftwareDisc     = "SoftwareDiscovery";
        public const string FileRead         = "FileRead";
        public const string ExternalCall     = "ExternalCall";
        public const string Switch           = "Switch";
        public const string SaveItems        = "SaveItems";
        public const string RandomString     = "RandomString";
        public const string Tpm              = "TPM";
        public const string Rest             = "REST";
        public const string ToJson           = "ToJSON";
        public const string FromJson         = "FromJSON";
    }

    public static class InputTypes
    {
        public const string Text       = "InputText";
        public const string Choice     = "InputChoice";
        public const string ChoiceList = "ChoiceList";
        public const string Checkbox   = "InputCheckbox";
        public const string Info       = "InputInfo";
        public const string Browse     = "InputBrowse";

        // Legacy element names kept for backwards compat
        public const string TextOld     = "TextInput";
        public const string ChoiceOld   = "ChoiceInput";
        public const string CheckboxOld = "CheckboxInput";
    }

    public static class Defaults
    {
        public const string Namespace       = @"root\cimv2";
        public const string Question        = "What's Up Doc?";
        public const string Variable        = "UnnamedVariable";
        public const string Value           = "NoValueSet";
        public const string CimType         = "CIM_STRING";
        public const string MaxRetry        = "5";
        public const string Text            = "Some Random Info";
        public const string PackageVarBase  = "XPackages";
        public const string AppVarBase      = "XApplications";
        public const string NumberOfLines   = "1";
        public const string Filename        = @"%temp%\ui++vars.dat";
        public const string Domain          = "domain-fqdn.com";
        public const string TimeoutAction   = "Continue";
        public const string TimeoutMsg      = "Timeout in ";
        public const string AccentColor     = "#002147";
        public const string SidebarTextColor = "#FFFFFF";
        public const string AllowedChars    = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        public const int    Length          = 6;
        public const int    MaxRunTime      = 60;
        public const string RestVariable    = "RESTResult";
        public const string JsonVariable    = "JSONValue";
        public const string DefaultValueAll = "All";
    }

    public static class DefaultValueCategories
    {
        public const string OS       = "OS";
        public const string Asset    = "Asset";
        public const string VM       = "VM";
        public const string Domain   = "Domain";
        public const string Net      = "Net";
        public const string TPM      = "TPM";
        public const string Security = "Security";
        public const string User     = "User";
        public const string Mgmt     = "Mgmt";

        public static readonly IReadOnlyList<string> Ordered =
            [OS, Asset, VM, Domain, Net, TPM, Security, User, Mgmt];
    }

    public static class Variables
    {
        // OS
        public const string OsVersion           = "XOSVersion";
        public const string OsServicePack        = "XOSServicePack";
        public const string OsBuild              = "XOSBuild";
        public const string OsProduct            = "XOSProduct";
        public const string IsServerCoreOs       = "XOSServerCore";
        public const string IsWinPe              = "XOSWinPE";
        public const string OsArch               = "XOSArchitecture";
        public const string OsSystemDrive        = "XOSSystemDrive";
        public const string OsCbsRebootPending   = "XOSCBSRebootPending";
        public const string OsWuaRebootPending   = "XOSWUARebootPending";
        public const string OsPfro               = "XOSPendingFileRenameOperations";
        public const string OsPendingRename      = "XOSPendingComputerRename";
        public const string CcmRebootPending     = "XOSCCMRebootPending";
        public const string RebootPending        = "XOSRebootPending";

        // Hardware
        public const string ChassisType          = "XHWChassisType";
        public const string VmType               = "XHWVirtualMachineType";
        public const string AssetTag             = "XHWAssetTag";
        public const string SerialNumber         = "XHWSerialNumber";
        public const string Model                = "XHWModel";
        public const string LenovoModel          = "XHWLenovoModel";
        public const string Manufacturer         = "XHWManufacturer";
        public const string Memory               = "XHWMemory";
        public const string Uuid                 = "XHWUUID";
        public const string Product              = "XHWProduct";
        public const string OnBattery            = "XOnBattery";

        // CPU
        public const string CpuArch              = "XCPUArchitecture";
        public const string CpuSse2              = "XCPUSSE2";
        public const string CpuNx                = "XCPUNX";
        public const string CpuPae               = "XCPUPAE";
        public const string CpuVendor            = "XCPUVendor";
        public const string CpuLogicalCount      = "XCPULogicalCount";

        // Disk
        public const string SystemDiskTotal      = "XSystemDiskTotalSizeGB";
        public const string SystemDiskFree       = "XSystemDiskFreeSpaceGB";

        // Firmware
        public const string IsUefi              = "XSystemUEFI";
        public const string IsSecureBoot        = "XSystemSecureBoot";
        public const string SystemDriveBitlocker = "XSystemDriveBitLockerProtected";

        // Network
        public const string WlanDisconnected     = "XWLANDisconnected";
        public const string WlanSsid             = "XWLANSSID";
        public const string WiredLanConnected    = "XWiredLANConnected";
        public const string IpGateway            = "XIPGateway";
        public const string IpAddress            = "XIPAddress";
        public const string IpSubnet             = "XIPSubnet";
        public const string IpSubnetMask         = "XIPSubnetMask";
        public const string MacAddress           = "XMACAddress";

        // Auth / User
        public const string AuthUser             = "XAuthenticatedUser";
        public const string AuthUserDisplayName  = "XAuthenticatedUserDisplayName";
        public const string AuthUserDomain       = "XAuthenticatedUserDomain";
        public const string AuthUserGroups       = "XAuthenticatedUserGroups";
        public const string AltUserGroups        = "XAlternateUserGroups";
        public const string AltUser              = "XAlternateUser";
        public const string AuthUserAttr         = "XAuthUserAttribute";
        public const string UserDisplayName      = "XUserDisplayName";
        public const string UserSamAccount       = "XUserSAMAccountName";
        public const string UserPrincipalName    = "XUserPrincipalName";
        public const string UserIsLocalAdmin     = "XUserIsLocalAdmin";

        // Domain / Computer
        public const string CurrentDomain           = "XCurrentComputerDomain";
        public const string CurrentComputerName     = "XCurrentComputerName";
        public const string CurrentComputerOuDn     = "XCurrentComputerOUDN";
        public const string ComputerJoinedToDomain  = "XCurrentComputerJoinedToDomain";
        public const string AzureDomainJoined       = "XComputerAzureDomainJoined";
        public const string AzureDomainRegistered   = "XComputerAzureDomainRegistered";
        public const string AzureDomain             = "XComputerAzureDomain";
        public const string AzureUser               = "XComputerAzureUser";
        public const string AzureTenantId           = "XComputerAzureTenantId";

        // Firewall
        public const string FirewallEnabled         = "XFirewallEnabled";
        public const string FirewallInbound         = "XFirewallInbound";
        public const string FirewallOutbound        = "XFirewallOutbound";
        public const string FirewallCurrentProfiles = "XFirewallCurrentProfiles";

        // Services
        public const string ServiceState            = "XServiceState";
        public const string ServiceStartMode        = "XServiceStartMode";

        // Windows Update
        public const string WuEnabled              = "XWindowsUpdatesEnabled";
        public const string WuDefaultService       = "XWindowsUpdateDefaultService";
        public const string WuServer               = "XWindowsUpdateServer";

        // ConfigMgr
        public const string CmSiteCode             = "XConfigMgrSiteCode";
        public const string CmAgentVersion         = "XConfigMgrAgentVersion";
        public const string CmCurrentMp            = "XConfigMgrCurrentMP";

        // MDM / Intune
        public const string MdmAuthorityName        = "XMDMAuthorityName";
        public const string IntuneAgentInstalled    = "XIntuneClientAgentServiceInstalled";

        // Defender
        public const string DefenderAvEnabled       = "XDefenderAVEnabled";
        public const string DefenderAsEnabled       = "XDefenderASEnabled";
        public const string DefenderNisEnabled      = "XDefenderNISEnabled";
        public const string DefenderFullScanAge     = "XDefenderFullScanAge";
        public const string DefenderEngineVersion   = "XDefenderEngineVersion";
        public const string DefenderAvSigAge        = "XDefenderAVSignatureAge";

        // TPM
        public const string TpmEnabled             = "XTPMEnabled";
        public const string TpmActivated           = "XTPMActivated";
        public const string TpmAvailable           = "XTPMAvailable";
        public const string TpmOwned               = "XTPMOwned";
        public const string TpmSpecVersion         = "XTPMSpecVersion";
    }

    public static class Values
    {
        public const string Yes                = "Yes";
        public const string No                 = "No";
        public const string True               = "True";
        public const string False              = "False";
        public const string Continue           = "Continue";
        public const string Cancel             = "Cancel";
        public const string Upper              = "Upper";
        public const string Lower              = "Lower";
        public const string Unknown            = "Unknown";
        public const string Unsupported        = "Unsupported";

        public const string SizeRegular        = "Regular";
        public const string SizeTall           = "Tall";
        public const string SizeExtraTall      = "ExtraTall";

        public const string HiveHklm           = "HKLM";
        public const string HiveHkcu           = "HKCU";

        public const string RegValueTypeSz     = "REG_SZ";

        public const string DirectionSave      = "Save";
        public const string DirectionLoad      = "Load";

        public const string ChassisLaptop      = "Laptop";
        public const string ChassisDesktop     = "Desktop";
        public const string ChassisServer      = "Server";
        public const string ChassisVirtual     = "Virtual";
        public const string ChassisTablet      = "Tablet";

        public const string ArchX86            = "x86";
        public const string ArchX64            = "x64";
        public const string ArchOther          = "Other";

        public const string OsServer           = "Server";
        public const string OsWorkstation      = "Workstation";
        public const string OsDomainController = "DomainController";

        public const string VmHyperV           = "Hyper-V";
        public const string VmVmwareEsx        = "VMWareESX";
        public const string VmVmware           = "VMWareOther";
        public const string VmVirtualBox       = "VirtualBox";
        public const string VmXen              = "Xen";
        public const string VmOtherMs          = "OtherMicrosoft";

        public const string ConditionEngineNative   = "native";
        public const string ConditionEngineVbscript = "vbscript";
    }
}
