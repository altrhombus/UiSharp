---
layout: article
title: External Call
aside:
  toc: true
sidebar:
  nav: docs
---

This action initiates the command line specified.  What this command line explicitly does or calls is up to you. The location of the files or scripts that the action references are up to you but they are best placed in the same package as UI++, in the boot image with UI++ (if using UI++ as a pre-start command), or in a separate package downloaded to the target system using the **Download Package Content** action.

The following example calls the VBScript named QueryAD.vbs.

~~~ xml 
<Action Type="ExternalCall" Title="External Command">cscript.exe //NOLOGO //B QueryAD.vbs</Action>
~~~

Using an [ExternalCall]({% link docs/reference/actionelements.md %}#externalcall) action enables you to leverage existing scripts or other tools to perform a specific action that UI++ cannot directly perform. Possible activities for these external commands include calling a web service (which is much easier to do in PowerShell or VBScript), calling a driver installation routine, or initiating a reboot. Many more possibilities exist, as well.

If calling PowerShell from within WinPE, make sure that you include PowerShell in the WinPE boot image that you are using.