---
layout: article
title: Actions Introduction
aside:
  toc: true
sidebar:
  nav: docs
---

Actions form the basis of all activity in UI++. Configuring UI++ involves configuring a series of actions that UI++ processes and acts upon. Some of these actions display dialogs to the interactive user, while others perform tasks that require no user input. Values gathered from the interactive user or elsewhere are stored in variables and can be referenced or used in later occurring actions.

What actions UI++ performs, what UI++ shows, and ultimately, everything that UI++ does is wholly up to you, and the configuration that you provide to UI++. There are no required actions; UI++ is entirely open-ended and moldable to your requirements, environment, and organization. 

UI++ supports two general types of actions: interactive actions and non-interactive actions. Interactive actions show a dialog to the interactive user and enable direct user input or provide feedback to the user. Non-interactive actions do not display a dialog but instead perform some other processing that does not require user interaction. Some non-interactive actions may display some visual feedback however like a progress bar.

## Interactive Actions
UI++ supports the following interactive actions.

* **[AppTree]({% link docs/reference/actionelements.md %}#apptree)** - Shows an application selection tree in a dialog.
* **[ErrorInfo]({% link docs/reference/actionelements.md %}#errorinfo)** - Shows a dialog with an error message.
* **[Info]({% link docs/reference/actionelements.md %}#info)** - Shows a dialog with informational, formatted text.
* **[Input]({% link docs/reference/actionelements.md %}#input)** - Shows a dialog with text boxes, checkboxes, and drop-down list boxes to solit user input.
* **[Preflight]({% link docs/reference/actionelements.md %}#preflight)** - Performs a seriers of checks and displays their results in a dialog.
* **[UserAuth]({% link docs/reference/actionelements.md %}#userauth)** - Displays a dialog prompting for user credentials and then performs an authenticates against Active Directory using those credentials.

## Non-interactive Actions
UI++ supports the following non-interactive actions.
* **[DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues)** - Collects information from the local system.
* **[ExternalCall]({% link docs/reference/actionelements.md %}#externalcall)** - Performs a call to an external command-line.
* **[FileRead]({% link docs/reference/actionelements.md %}#fileread)** - Reads information from a file.
* **[RegRead]({% link docs/reference/actionelements.md %}#regread)** - Reads information from the Windows registry.
* **[RegWrite]({% link docs/reference/actionelements.md %}#regwrite)** - Writes information to the Windows registry.
* **[SaveItems]({% link docs/reference/actionelements.md %}#saveitems)** - Saves logs and other configuration to a specifed location.
* **[SoftwareDiscovery]({% link docs/reference/actionelements.md %}#softwarediscovery)** - Searches the registry for installed software.
* **[Switch]({% link docs/reference/actionelements.md %}#switch)** - Sets variable values based on the value of a base variable.
* **[TSVar]({% link docs/reference/actionelements.md %}#tsvar)** - Sets the value of a single variable.
* **[TSVarList]({% link docs/reference/actionelements.md %}#tsvarlist)** - Sets the values of a variable list.
* **[Vars]({% link docs/reference/actionelements.md %}#vars)** - Loads variables from a data file or saves the current variables to data file.
* **[WMIRead]({% link docs/reference/actionelements.md %}#wmiread)** - Reads information from the local system using Windows Management Instrumentation (WMI).
* **[WMIWrite]({% link docs/reference/actionelements.md %}#wmiwrite)** - Writes information to the local system using Windows Management Instrumentation (WMI).