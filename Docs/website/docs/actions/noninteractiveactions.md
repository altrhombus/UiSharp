---
layout: article
title: Non-interactive Actions
aside:
  toc: true
sidebar:
  nav: docs
---

In addition to collecting information and input from the interactive user, UI++ can gather information from other sources, store information, perform a background activity, and call external actions all without any user interaction or user interface.

## Non-interactive Actions Introduction

Non-interactive actions do not solicit input from the user. Instead, they perform some background activity. As with interactive actions, your requirements dictate precisely which non-interactive actions are required, how many are required, and precisely what they do. You can freely intermingle non-interactive actions with interactive actions. Additionally, their results can influence interactive actions, or can conversely be influenced by the results of interactive actions.

### Default Values

The [Default Values]({% link docs/reference/actionelements.md %}#defaultvalues) action gathers information from various sources on the local system. This action places all gathered information into variables accessible elsewhere in UI++ or within a ConfigMgr task sequence. The following categories of information are available for gathering:

* Asset
* Domain
* Management
* Network
* Operating System
* Security
* Trusted Platform Module
* Virtual Machine

###	External Call
As its name implies, the [External Call]({% link docs/reference/actionelements.md %}#externalcall) action enables you to run (or call) any command-line that you wish. The called command does not explicitly return anything to UI++, but running the command from UI++ enables the following:
 - Passing values to the external command.
 - Use of any task sequence variables created or set by the external command within subsequent UI++ actions.

A common use case for external commands is calling PowerShell scripts that perform actions in or return data from external sources like Active Directory or ConfigMgr. These PowerShell scripts may directly perform these activities or could, in turn, call a web service.

###	File Read
The [File Read]({% link docs/reference/actionelements.md %}#fileread) action reads the first line of a specified text file and optionally deletes that line. The action then places the content of that line into a variable.

The designed scenario for the [File Read]({% link docs/reference/actionelements.md %}#fileread) action is for providing a text file with a list of possible system names. This file then enables UI++ to provide a unique name to each new system when populating the *OSDComputerName* task sequence variable. There are other possible scenarios for the [File Read]({% link docs/reference/actionelements.md %}#fileread) action, however.

One way to supply the text file to UI++ during OSD is to place the text file in a share accessible to systems that execute UI++. Then use a [Connect to Network Folder](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_ConnectToNetworkFolder) task before executing UI++ to map to this share using credentials that have read and write access to the text file. In the UI++ configuration file, specify the path to the text file using the drive letter specified in the [Connect to Network Folder](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_ConnectToNetworkFolder) task as well as the proper sub-folders as necessary.

### Random String
The [Random String]({% link docs/reference/actionelements.md %}#randomstring) action generates a string of random characters and populates a designated variable with the resulting string. You can configure the length of the string as well as the character set this action uses to generate the string.

###	Registry
The [RegRead]({% link docs/reference/actionelements.md %}#regread) and [RegWrite]({% link docs/reference/actionelements.md %}#regwrite) actions read from and write values to the registry. The [RegRead]({% link docs/reference/actionelements.md %}#regread) action stores all retrieved values as strings in variables. [RegWrite]({% link docs/reference/actionelements.md %}#regwrite) actions write values to the registry as either strings (REG_SZ) or numbers (REG_DWORD). If a key or value does not exist, the [RegWrite]({% link docs/reference/actionelements.md %}#regwrite) action will automatically create it.

### Save Items

The [SaveItems]({% link docs/reference/actionelements.md %}#saveitems) action saves or copies files to a specified location. There are five item types or files that it can save:

1.	The SMSTS.log (including all smsts.log files that were created due to a rollover).
2.	The UI++.log.
3.	Any files in the current ConfigMgr agent log directory (%temp% in WinPE or if the ConfigMgr agent is not installed).
4.	Any specified files.
5.	A full dump of all current variables and their values.

### Software Discovery

The [SoftwareDiscovery]({% link docs/reference/actionelements.md %}#softwarediscovery) action discovers software installed on the current system by scanning the **Uninstall** key in the registry. Both 64-bit and 32-bit software is discovered. UI++ matches discovered software to a series of defined regular expression rules that compare the display name or the version of the software. UI++ setts the value of a variable based on whether or not a match is found.

### Switch

The [Switch]({% link docs/reference/actionelements.md %}#switch) action is an advanced way to populate the values of a single or multiple variables; it is similar to the SQL **CASE** statement and the C++ **switch** statement. The [Switch]({% link docs/reference/actionelements.md %}#switch) action  uses the value of one of the following to determine a set of defined variables to populate.

*	A static value.
*	A variable.
*	An expression.

Based on this value, UI++ chooses a single **Case** sets the variable(s) defined within this case to the specified values. 

### Variables

The [TSVar]({% link docs/reference/actionelements.md %}#tsvar) action creates a new variable and sets its value or sets the value of an existing variable. Inside a task sequence (or a pre-start command), these variables are task sequence variables and are accessed through standard task sequence variable manipulation methods. Outside of a task sequence, the variables are internal to UI++ and are not available elsewhere.

The [Vars]({% link docs/reference/actionelements.md %}#vars) action saves all current variables and their values to a file or loads variables and their values from a file. Saving and loading variables enable variable persistence between executions of UI++. UI++ uses a binary format , so the values are obfuscated; however, no specific security or protection is used to protect values.

The primary use case is to enable UI++ to be run before a refresh task sequence executed from Software center; this is usually done using a program that a task sequence depends on. Doing this allows the UI++ to display its interactive action dialogs without the use of ServiceUI.exe. Running UI++ in this way though prevents UI++ from saving variables as task sequence variables within the task sequence. Saving the variables and their values during this initial execution of UI++ then allows these variables and their values to be re-loaded as the first step of a task sequence.

### Variable Lists

The [TSVarList]({% link docs/reference/actionelements.md %}#tsvarlist) action creates a sequence of variables populated with application names or package IDs and program names. A base variable name appended with a two-digit suffix, starting with **01**, defines a list of variables. Each subsequent variable has its two-digit suffix incremented by one. These lists of variables are suitable for use within either an **Install Application** or **Install Package** task within a task sequence.

### WMI
The **WMIRead** and **WMIWrite** actions respectively read and write values in WMI. All values retrieved are stored as strings in variables.

<i class="fa fa-info-circle"></i> Nearly all of the default Windows WMI classes have very few (if any) writable attributes, so writing information to WMI is generally not possible except with custom classes.
{:.note}