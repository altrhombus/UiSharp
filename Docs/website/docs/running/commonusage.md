---
layout: article
title: Common Usage and Scenarios
aside:
  toc: true
sidebar:
  nav: docs
---

UI++ is a native Windows executable that runs like any other Windows application or program. UI++ also depends upon two DLL files that must be in the same location as the UI++ EXE file. There are separate versions of all of these files for x86 and x64 operating systems; this is significant on x86 operating systems which of course cannot run x64 code and also when UI++ runs in Windows PE where the UI++ version must match the architecture of the Windows PE image currently running. 

|  OS            | UI++ Architecture |
|:---------------|:--------------|
| Windows x64    | x86 or x64    |
| Windows x86    | x86           |
| WinPE x64      | x64           |
| WinPE x86      | x86           |


<i class="fa fa-info-circle"></i> UI++ no longer requires oledlg.dll.
{:.note}

By default, UI++ loads its configuration from an XML configuration file named **ui++.xml** using the standard Windows file location rules. To load an alternately named file or a file in an alternate location, use the [/config]({% link docs/running/commandline.md %}#config) command line argument.

## Outside of a Task Sequence
Running UI++ outside of a task sequence is perfect for testing your configuration or for soliciting user input at other times. Run UI++ by double-clicking on the icon or from the command line. You can include it as a Windows startup command, as a program deployed from ConfigMgr, or anywhere else that you find a use for it.

UI++ does not use task sequence variables outside of a task sequence, but there is no actual UI++ functionality loss as there is an internal variable system that is equivalent and provides the same functionality transparently. The only difference is that the variables are not accessible to external processes.

## Inside of a Task Sequence
Using UI++ within a task sequence is one of the main ways to add interactivity to a task sequence. The main drawback with this approach is that it may not be the first thing that the users see. Also keep in mind that if you run a task sequence from within Windows, you need to inject processes that display a user interface, like UI++, onto the interactive desktop using something like **serviceui.exe** from MDT/UDI.

{% figure caption:"Running UI++ in a task sequence" %}
  ![Running UI++ in a task sequence](/assets/images/screenshots/tasksequence-run.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

To add UI++ to a task sequence, follow these steps.

1. Make the UI++ binary files available to the task sequence. This includes UI++[64].exe, FTWldap[64].dll and FTWCMLog[64].dll.

2. Make the configuration file available to UI++. This may or may not be in the same location as the binary files from step 1.

3. Create and use a **Run Command Line** task to run UI++.

See the [Making UI++ Files Available](#making-ui-files-available) section below for a discussion on how to make the UI++ binary files or your configuration available to the task sequence.

## As a Prestart Command

Instead of including UI++ in a task sequence explicitly, you can embed it in your boot image(s) and run it as a prestart command. This has the advantage of making UI++ the first thing the user sees after WinPE boots and also gives you the ability to set the [SMSTSPreferredAdvertID](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-variables#SMSTSPreferredAdvertID) task sequence variable using UI++ which forces the selection of a specific task sequence.

As of ConfigMgr Current Branch, configuring a prestart command for a boot image or specific boot media  is easy and is done entirely in the console.

### Boot Image

{% figure caption:"Setting a boot image prestart command" %}
  ![Setting a boot image prestart command](/assets/images/screenshots/bootimage-prestart.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

Configuring a prestart command on a boot image ensures that the prestart command is run whenever you use the boot image to boot a system.

1. Create a UNC accessible folder with the necessary UI++ files in it.
2. Open the properties of the boot image that you wish to add a prestart command to and go to the **Customization** tab.
   1. Check the **Enable prestart command** option.
   2. Supply the proper command-line to run UI++. Note that you do not need to supply a  fully-qualified path for prestart command.
   3. Check the **Include files for the prestart command** option.
   4. Specify the UNC path to the folder with the UI++ files in it.
   5. Update the boot image.

### Boot Media

{% figure caption:"Setting a boot media prestart command" %}
  ![Setting a boot media prestart command](/assets/images/screenshots/bootmedia-prestart.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

You can also configure a prestart command for boot media. Configuring a prestart command on boot media limits when the prestart command runs to only when you use the specific boot media to boot a system. The actual boot image used doesn't matter. Prestart commands configured in the boot media trump those configured in the boot image.

You configure a prestart command in boot media during the **Create Task Sequence Media Wizard**. On the customization tab, do the following:
1. Check the **Enable prestart command** option.
2. Supply a command-line to run UI++.
3. Check the **Include files for the prestart command** option.
4. Choose a package containing the UI++ files. No program is necessary for the package.
5. Choose a distribution point where the specified package was successfully distributed.

## Making UI++ Files Available
There are multiple ways to make the files available to the task sequence as described in the following sections. Keep in mind that you do not need to store your configuration file in the same location as the binary files.

### ConfigMgr **Package**

For this option, place the files into the source location of a **Package**. No program is necessary for this option.

#### Advantage

* Ensures that the files come from the closest DP.

#### Disadvantages

* Requires updating the package when the files change.

### Mapped Shared Location

This option uses a [Connect to Network Folder](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_ConnectToNetworkFolder) task to connect to the shared location before UI++ runs; this makes the file(s) accessible using the specified mapped drive letter. Use this mapped drive letter as necessary to reference the EXE or configuration file.

#### Advantages

* No DP updates are necessary.
* Does not require local drive preparation before use.

#### Disadvantages

* Requires an account in the task.
* Requires connectivity to the shared location specified from all systems where the task sequence runs.

### HTTP Location

With this option, UI++ accesses the configuration file from a web server at a predefined URL. This option is only valid for the configuration file.

#### Advantages

* No DP updates are necessary
* Does not require local drive preparation before use.

#### Disadvantage

* Requires connectivity to the HTTP location specified from all systems where the task sequence runs. There are various methods to easily extend the accessibility of an HTTP location, including DNS with multiple location-specific web servers, WAN accelerators, or CDNs.

### Embedded in Boot Image or Media

You can embed the binary and configuration files in the boot image or boot media in a variety of ways as described in the following table.

| Embed Method                 | File location               | 
|:-----------------------------|:----------------------------|
| Boot image prestart command  | X:\SMS\PKG\SMS10000         |
| Boot media prestart command  | &lt;MEDIA:&gt;\SMS\PKG\&lt;PKGID&gt;  |
| Manual injection             | Injection location          |
| osdinjection.xml             | Defined in osdinjection.xml |

#### Advantage

* No connectivity is required to a remote location.

#### Disadvantage

* Requires updating the boot image or media when the files change.