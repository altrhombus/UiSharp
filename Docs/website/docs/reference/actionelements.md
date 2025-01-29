---
layout: article
title: XML Configuration - Action Elements
aside:
  toc: true
sidebar:
  nav: docs
---

The following reference lists the different action types and their available configuration options. Configure actions using *Action* XML elements within the XML configuration file. Set the desired action type using the *Type* attribute and set additional options for the actions using the listed XML attributes. Attributes marked with an asterisk (*) are required for the element that they are listed under.

## AppTree
Displays a dialog with a tree of selectable applications and packages.

ApplicationVariableBase
: The base variable to populate with selected applications. Default is *XApplications*.)

CenterTitle `3.0.1.0`{:.info}
: Centers the action dialog's title text. (Valid values are *True* and *False*. Default is *False*.)

Expanded
: Sets whether the tree is expanded or not when the action is first launched. (Valid values are *True* and *False*. Default is *True*.)

NoDefaultButton `3.0.1.0`{:.info}
: Prevents pressing the Enter key from dismissing the dialog if a button does not have focus. (Valid values are *True* and *False*. Default is *False*.)

PackageVariableBase
: The base variable to populate with selected packages. Default is *XPackages*.)

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.)

ShowCancel `2.9.4.0`{:.info}
: Shows a cancel button allowing the user to gracefully exit UI++ without proceeding. (Valid values are *True* and *False*. Default is *False*.)

Size
: The height of the AppTree dialog displayed. (Valid values are *Regular*, *Tall*, and *ExtraTall*. Default is *Regular*.)

Title
: The title of the dialog.

## DefaultValues
Discovers and populates default variables based on the current system state.

ShowProgress
: Shows a dialog with a progress bar so that the user is aware of the background activity taking place. (Valid values are *True* and *False*. Default is *True*.)

ValueTypes
: The category of values to retrieve. Valid values are as follows:
* *Asset*
* *Domain*
* *Mgmt*
* *Net*
* *OS*
* *Security*
* *User*
* *VM*
* *All* 
  
  To retrieve multiple categories of values specify a comma separated list of categories. (Default is *All*.)

## ErrorInfo
Displays an error dialog box. The actual text shown is from the value of this element and not an attribute. To use HTML tags, ensure that you use a <![CDATA[ tag so that the XML parser doesn’t freak out.

Both the **Image** and **InfoImage** attributes can specify local images or those retrievable using an HTTP based URL; e.g., http://home.configmgrftw.com/images/coretechnew.png. Simply specify the URL instead of a local path for either of these attributes.

CenterTitle `3.0.1.0`{:.info}
: Centers the action dialog's title text. (Valid values are *True* and *False*. Default is *False*.)

Image
: An optional image to display at the top of the dialog. For best results, the image should be 462x75 pixels and have a white background.

InfoImage
: Adds an optional image, center and at the bottom of the dialog. This image will appear on top of any information displayed in the dialog if there is any overlap.

Name
: The name of the dialog box.

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.) 

ShowCancel `3.0.1.0`{:.info}
: If set to *False* and UI++ is running in WinPE, UI++ shows a restart button instead of a cancel button. If set to *True* of UI++ is not running in WinPE, then UI++ shows a cancel button allowing the user to gracefully exit UI++ without proceeding. (Valid values are *True* and *False*. Default is *True*.)

Title
: The title text of the dialog box.

## ExternalCall
Runs the specified external command. The command-line run is contained in the value of the element and not an attribute. Enclose this command in <![CDATA[ XML tags if necessary to prevent the XML parser from mis-interpreting characters in the specified command-line.

ExitCodeVariable
: The name of a variable to store the exit code in from the external command.

MaxRunTime
: The maximum allowed run time in seconds for the command. If the command does not finish in this amount time, the process will be forcefully terminated by UI++. Default is *60*.)

Title
: The title of the external command. This is shown in the log as well as the progress bar but is not used to actually execute the command-line.	

## FileRead
Reads the first line from the specified file, places the value into the specified variable, and optionally deletes the line from the file.

DeleteLine
: Whether the line should be deleted after it is read from the file. (Valid values are *True* and *False*. Default is *True*.)

Filename*
: The full path including the file name of the text file to read from.

Variable*
: The variable name to store the resulting text in.

## Info	
Displays an info dialog box. The actual text shown is from the value of this element and not an attribute. To use HTML tags, ensure that you use a <![CDATA[ tag (see the example) so that the XML parser doesn’t freak out.

Both the **Image** and **InfoImage** attributes can specify local images or those retrievable using an HTTP based URL; e.g., http://home.configmgrftw.com/images/coretechnew.png. Simply specify the URL instead of a local path for either of these attributes.

CenterTitle `3.0.1.0`{:.info}
: Centers the action dialog's title text. (Valid values are *True* and *False*. Default is *False*.)

Image
: An optional image to display at the top of the dialog. For best results, the image should be 462x75 pixels and have a white background.

InfoImage
: Adds an optional image, center and at the bottom of the dialog. This image will appear on top of any information displayed in the dialog if there is any overlap.

Name
: The name of the dialog box

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.) 

ShowCancel
: Shows a cancel button allowing the user to gracefully exit UI++ without proceeding. (Valid values are *True* and *False*. Default is *False*.)

Timeout `2.9.0.0`{:.info}
: The number of seconds after which the dialog will be automatically dismissed. Default is *0* which results in the dialog not timing out at all.)

TimeoutAction `2.9.0.0`{:.info}
: The action to take when the dialog timeout elapses. (Valid values are *Continue*, *ContinueNoPreempt* `3.0.1.0`{:.info}, *Cancel* or a custom return code which also cancels the dialog and exits UI++ return the specified code. Default is *Continue*.)

Title
: The title text of the dialog box.

## InfoFullScreen
`Preview`{:.warning} `3.0.1.0`{:.info}
Displays information to the interactive user using a full-screen, always on top, dialog. The actual text shown is from the value of this element and not an attribute.

The **Image** attribute can specify a local image or one retrievable using an HTTP based URL; e.g., http://home.configmgrftw.com/images/ElkwaterUnited-big.png. Simply specify the URL instead of a local path for the attribute.

BackgroundColor
: The hexadecimal color for the background. (Default is *#002147*[^1].)

Image
: An optional image to display at the top of the dialog. UI++ will automatically and proportionally scale down the image to fit no more than half of the screen's height or 90% of the screen's width, whichever is smaller.

TextColor
: The hexadecimal color to display the text in. (Default is *#FFFFFF*[^2].)

[^1]: The University of Michigan Blue
[^2]: white

## Input
Displays a dialog with a series of customizable text input boxes, combo/choice boxes, and info text.

CenterTitle `3.0.1.0`{:.info}
: Centers the action dialog's title text. (Valid values are *True* and *False*. Default is *False*.)

Name
: The name of the dialog box.

NoDefaultButton `3.0.1.0`{:.info}
: Prevents pressing the Enter key from dismissing the dialog if a button does not have focus. (Valid values are *True* and *False*. Default is *False*.)

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.)

ShowCancel `2.9.4.0`{:.info}
: Shows a cancel button allowing the user to gracefully exit UI++ without proceeding. (Valid values are *True* and *False*. Default is *False*.)

Size
: The height of the input dialog displayed. A regular sized Input dialog will display up to 3 controls and a tall will display up to 6. (Valid values are *Regular* and *Tall*. Default is *Regular*.)

Title
: The title text of the dialog box.

## Preflight
Displays a dialog containing “preflight” checks. All checks must pass for the dialog to be able to be dismissed successfully. If any checks do not pass, dismissing the dialog results in an error code and a halt to UI++.

CenterTitle `3.0.1.0`{:.info}
: Centers the action dialog's title text. (Valid values are *True* and *False*. Default is *False*.)

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.)

ShowCancel `2.9.4.0`{:.info}
: Shows a cancel button allowing the user to gracefully exit UI++ without proceeding. (Valid values are *True* and *False*. Default is *False*.)

ShowOnFailureOnly
: Shows the preflight dialog if and only if one or more of the Checks defined for it have failed. (Valid values are *True* and *False*. Default is *False*.)

Size
: The height of the preflight dialog displayed. (Valid values include *Regular* and *Tall*. Default is *Regular*.)

Timeout `2.11.0.0`{:.info}
: The number of seconds after which the dialog will be automatically dismissed. Default is *0* which results in the dialog not timing out at all.)

TimeoutAction `2.11.0.0`{:.info}
: The action to take when the dialog timeout elapses. (Valid values are *Continue*, *ContinueOnWarning*, *Cancel* or a custom return code which also cancels the dialog and exits UI++ return the specified code. Default is *Continue*.)

Title
: The title of the preflight dialog.

## RandomString	
This action generates a random string of a given length from a pre-defined set of allowed characters and stores this value in the specified variable.

AllowedChars
: A string containing the set of allowed characters used by the action to populate the variable. (Default is *ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890*.)

Length
: The length of the random string generated. (Valid values include integers between *1* and *36*. Default is *6*.)

Variable
: The variable name to store the random string in.


## RegRead	
Reads a value from the registry and places it into a variable.

Default
: A default value to populate the specified variable with if the registry read operation cannot read a value or returns a blank value.

Hive
: The registry hive to read the value from. (Valid values are *HKLM* and *HKCU*. Default is *HKLM*.)

Key*
: The full path to the registry key to read from.

Reg64
: Read from the 64-bit registry instead of the 32-bit registry if using the 32-bit version. (Valid values are *True* and *False*. Default is *True*.)

Variable*
: The variable name to store the registry value in.

Value*
: The name of the registry value to retrieve.	

## RegWrite
Writes a value to the registry in the specified location. If the value already exists, UI++ overwrites it.	

Hive
: The registry hive to read the value from. (Valid values are *HKLM* and *HKCU*. Default is *HKLM*.)

Key*
: The full path to the registry key to read from.

Reg64
: Write to the 64-bit registry instead of the 32-bit registry if using the 32-bit version. (Valid values are *True* and *False*. Default is *True*.)

Type
: The registry value type to be written. (Valid values include any valid registry value type including *REG_SZ* and *REG_DWORD*. Default is *REG_SZ*.)

Value*
: The name of the registry value to retrieve.	

## SaveItems
`2.10.3.0`{:.info} Saves "items" to the specified location.

Items* 
: A comma-separated list of items to save. Valid values for Items include the following:
* *SMSTSLog*
* *UILog*
* *TSVariables*
* *\<filename\>*
* *\<path\>\\\<filename\>*

  By default, a value of *TSVariables* for this attribute will use a file name of “UI++ Variable Dump \<time and date\>.txt”. To specify a custom file name, append the desired name after specifying *TSVariables* but separated by a colon; e.g, `TSVariables:vars.txt` will name the file `vars.txt`.

  Environment variables and wildcards are valid for the *\<filename\>* and *\<path\>\\\<filename\>* values.

Path*
: The path to save the items to. Wildcards are valid for or in this attribute's value.

## SoftwareDiscovery
Searches the registry for installed software. There are no configurable options or attributes for this action.

## Switch
`2.9.2.0`{:.info} This action matches the value specified in the **OnValue** attribute against a series of regular expression cases (which are defined in **Case** sub-elements). If any of the cases match, then the Variable sub-elements are used to set the value of one or more variables.

DontEval
: If set to *True*, do not pass the value of the OnValue attribute to VBScript for processing. (Valid values are *True* and *False*. Default is *False*.)

OnValue*
: The value to match against. This can be a static value, a variable name (surround in '%') or a VBScript expression.

## TSVar	
This action statically sets a variable's value; the variable can be any pre-existing variable or a new one. The actual value set is from the value of the element and not an attribute.

DontEval
: If set to *True*, do not pass the value of the OnValue attribute to VBScript for processing. (Valid values are *True* and *False*. Default is *False*.)

Name or Variable*
: The variable name to set a value for.

  **Variable** and **Name** are synonymous. **Variable** was added for attribute naming consistency with other actions and is preferred. If both are set in an occurence of this action, the value of the **Variable** attribute takes precedence to set the name of the variable.

## TSVarList	
Creates a variable list populated with application or package information for use with an Install Application task or an Install Package task respectively. One or both of the attributes must have a value for this Action to be processed.

ApplicationVariableBase
: The base variable to populate with included applications.

PackageVariableBase
: The base variable to populate with included packages.

## UserAuth
Displays a dialog with three partially customizable fields: Username, Password, and Domain. 

Attributes `2.10.3.0`{:.info} 
: A comma (or semi-colon) separated list of valid AD attributes to gather from the user that successfully authenticates using this action.

DisableCancel `2.9.3.0`{:.info} 
: Initially disables the cancel button on the UserAuth dialog. If set to true, the button will be re-enabled if the maximum number of retries without a successfully authentication occurs. (Valid values are *True* and *False*. Default is *False*.)

Domain*
: The default domain to authenticate the user against. This should be in the form of an FQDN.

DomainController `3.0.1.0`{:.info}
: Specifies a domain controller to use for authentication. If UI++ cannot contact this domain controller, then authentication falls back to searching for and using the closest domain controller.

DoNotFallback `3.0.1.0`{:.info}
: If a domain controller is specified using the **DomainController** attribute, setting this attribute to *True* prevents UI++ from searching for a domain controller if the specified domain controller is unreachable. (Valid values are *True* and *False*. Default is *False*.)

GetGroups
: Instructs this action to retrieve a list of Active Directory groups that the authenticated user is a direct member of. (Valid values are *True* and *False*. Default is *False*.)

Group
: A semi-colon separated list of domain security groups that the user must be a member of one in order to pass this action. 

MaxRetryCount
: The maximum number of authentication attempts allowed. (Default is *5*.)

ShowBack
: Shows a back button on the dialog enabling the interactive user to return to the previous GUI based action. (Valid values are *True* and *False*. Default is *False*.)

Title
: The title of the user authentication dialog.

## Vars
Loads variables from a data file or saves the current variables to data file. Saving only works if UI++ is executed outside of a task sequence; loading works within a task sequence or outside of it. Read-only variables (those beginning with an underscore) and variables beginning with an ‘X’ are never saved or loaded.

Direction
: Specifies whether to load or save the variables from or to a data file. (Valid values are *Load* and *Save*. Default is *Save*.

Filename
: The filename to load data from or save data to. (Default is *%temp%\ui++vars.dat*.)	

## WMIRead
Queries and retrieves information from WMI. There are two ways to retrieve information: by class instance or by query. If a property contains an array, the first value in the array is returned.

For class instance, you must specify the **Class** and a **Property**. If multiple instances of a class exist, then the first will be retrieved unless a key qualifier is specified.

For a query, you must specify a **Query** and a **Property**. Ensure that the query returns the property specified. If the query returns multiple instances, then the first will be used.

Class*
: The WMI Class to read from.

Default
: A default value to populate the specified variable with if the WMI query operation cannot read a value or returns a blank value.

KeyQualifier
: Specifies the WMI class qualifier using the key attribute of the class to identify a specific instance of a class.

Namespace
: The WMI namespace to read from. Root\cimv2 is the default if none namespace is specified.

Property*
: The WMI Property to pull the value from.

Query* `2.9.3.0`{:.info} 
: A valid WQL query. 	Reads a value from WMI and places it into a variable.

Variable*
: The variable name to store the WMI value in. 

## WMIWrite	
Opens or creates a namespace, a WMI class, and a WMI object instance within that namespace. Use a child *Property* element to write attributes into the opened or newly created object instance. If the class already exists then the properties specified must match those from that class’s definition. If the value of the key property is the same, the instance will be overwritten; if the values differs, then a new instance will be created.

Class*
: The WMI Class to write to.

Namespace
: The WMI namespace to write to. (Default is *Root\cimv2*.)
	
