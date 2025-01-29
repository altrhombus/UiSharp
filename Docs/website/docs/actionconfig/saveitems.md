---
layout: article
title: Save Items
aside:
  toc: true
sidebar:
  nav: docs
---

This action saves or copies files to a specified location. There are five item types or files that it saves:

1. The SMSTS.log (including all previous smsts.log files that the task sequence created due to a rollover).
2. The **ui++.log**.
3. Any files in the current ConfigMgr agent log directory (%temp% in WinPE or if the system does not have ConfigMgr agent installed).
4. Any specified files.
5. A full dump of all current variables and their values.

The following snippet saves the **ui++.log file**, all **smsts.log** files, and a dump of all variables in a file called **vars.txt** to the desktop of the user running UI++.

~~~ xml 
<Action Type="SaveItems" Items="UILog,TSVariables:Vars.txt,SMSTSLog" Path="%userprofile%\Desktop" />
~~~

To specify additional log files from the existing ConfigMgr agent log location, add their file names to the **Items** attribute as in the following example.

~~~ xml
<Action Type="SaveItems" Items="SMSTSLog,BDD.log,Gather.log" Path="%userprofile%\Desktop" />
~~~

Use wildcards (to copy multiple files), full paths, and environment variables are all also valid as shown in the next example.

~~~ xml
<Action Type="SaveItems" Items="UILog,*.log,%windir%\Panther\*.log" Path="%userprofile%\Desktop" />
~~~

The value that you specify for the **Path** attribute can be any valid UNC; however, keep in mind that the user running UI++ must have permissions to write to the UNC specified – UI++ can’t magically overcome permissions. If UI++ runs during OSD in ConfigMgr, then it runs under the context of the local System account; if it runs after the [Setup Windows and ConfigMgr](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_SetupWindowsandConfigMgr) step and the installed OS is joined to the domain, then the AD computer account of the computer is used to access network resources. Adding a [Connect to Network Folder](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_ConnectToNetworkFolder) step to the task sequence before running UI++ is a simple way to ensure access to any given UNC.