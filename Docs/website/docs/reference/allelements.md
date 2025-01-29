---
layout: article
title: XML Configuration - All Elements
aside:
  toc: true
sidebar:
  nav: docs
---

The following reference lists the different elements that are valid within the XML configuration file. Set additional options for the actions using the listed XML attributes. Attributes marked with an asterisk (*) are required for the element that they are listed under. Valid parent and child elements are also listed.

## Action
Performs one of the many available *Actions*. The different actions types and their valid attributes are described in the [Action Elements Reference]({% link docs/reference/actionelements.md %}).

Type*
: Specifies the type of action to perform.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Actions](#actions)   | Various (depends upon the Action type) |

## ActionGroup	
Groups actions into a single unit. Each child [Action](#action) or [ActionGroup](#actiongroup) element is performed in the order that it occurs.

Name*
: The name of the group.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| ActionGroup           | ActionGroup          |
|                       | [Action](#action)    |

## Actions
Groups all of the actions performed during UI++. Each child [Action](#action) or [ActionGroup](#actiongroup) element is performed in the order that it occurs.

There are no configurable attributes for this element.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [UIpp](#uipp)                  | [ActionGroup](#actiongroup) |
|                       | [Action](#action)    |

## Application
Defines the properties for an Application that may be displayed to the interactive user in an [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog.

Id*
: A unique identifier for the application. This Id value is referenced by a SoftwareRef element to display an Application in the AppTree action. This can be any identifier you want it to be including a GUID as long as it’s completely unique from other unique identifiers with the configuration XML.

IncludeId 
: A semi-colon separated list of Application and Package IDs that are included when this Application is chosen. This can include hidden applications and packages.

Label*
: The label shown for the Application in the [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog.

Name*
: The actual name of the Application as defined in ConfigMgr. This must match exactly including spelling and case.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Software](#software) |  |	

## Case 
`2.9.2.0`{:.info} Defines a case within a [Switch]({% link docs/reference/actionelements.md %}#switch) action. If the regular expression comparison succeeds, all child [Variable](#variable) elements are proccessed to set variables.

CaseInsensitive
: Performs a case insensitive match. (Valid values are *True* and *False*. Default is *False*.)

DontEval
: If set to *True*, do not pass the value of the element to VBScript for processing. (Valid values are *True* and *False*. Default is *False*.)

RegEx*
: The regular expression to compare against the value of the *OnValue* attribute of the parent [Switch]({% link docs/reference/actionelements.md %}#switch) element.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Switch]({% link docs/reference/actionelements.md %}#switch)) | [Variable](#variable) |

## Check
Adds a check to a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action.

CheckCondition*
: The definition of the condition to be checked. Any valid VBScript expression may be used which also may include variables.

Description
: Tooltip text to display on an info icon displayed in front of the check’s text.

ErrorDescription
: Tooltip text to display on the failed status icon for the check if it fails the specified CheckCondition.

Text*
: The text to display describing the check that was performed.

WarnCondition `2.11.0.0`{:.info}
: The definition of the condition to be checked for a warning. Any valid VBScript expression may be used which also may include variables.

WarnDescription `2.11.0.0`{:.info}
: Tooltip text to display on the warning status icon for the check if it fails the specified WarnCondition.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Preflight]({% link docs/reference/actionelements.md %}#preflight)) |  |
	
## Choice	
Adds a choice to a drop-down list box.

Option*
: Defines the text displayed in the drop-down list box.

Value
: The actual value assigned to the variable when this choice is selected. If not specified, the value of the Option attribute is used.

AlternateValue
: A second value assigned to the variable designated by the AlternateVariable attribute of the parent InputChoice element if this choice is chosen by the interactive user.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [InputChoice](#InputChoice) |  |

## ChoiceList
`2.9.2.0`{:.info} Adds a list of items to the drop-down list from a comma-separated list of values.

AlternateValueList
: A comma-separated list of values used to populate the *AlternateVariable* specified in the parent [InputChoice](#InputChoice) element based upon the choice made by the user.

OptionList
: A comma-separated list of values to add to the drop-down list created by the parent [InputChoice](#InputChoice) element.

ValueList
: A comma-separated list of values used to populate the *Variable* specified in the parent [InputChoice](#InputChoice) element based upon the choice made by the user.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [InputChoice](#InputChoice) |  |

## Field
Customizes one of the text box entry fields on a [UserAuth]({% link docs/reference/actionelements.md %}#userauth) action dialog.

Name*
: The [UserAuth]({% link docs/reference/actionelements.md %}#userauth) action dialog field to affect. (Valid values are *Username*, *Password*, or *Domain*.)

Hint
: The custom message to display in the tooltip shown when the text entered does not match the regular expression. (Defaults are *Please enter your user name*, *Please enter your password*, and *Please enter a domain*.)

List `2.9.3.0`{:.info}
: This attribute is only valid for the Domain field. If specified, this creates a drop-down list instead of a plain text field that enables the user to choose a valid domain. The values specified should be comma-separated. The value of the *Domain* attribute of the parent [UserAuth]({% link docs/reference/actionelements.md %}#userauth) action element is used as the default value. If this attribute has no value, then no default value will be used and the user must choose a value. 

Prompt
: The custom prompt text to display within the text box when it is empty. (Defaults are *User name*, *Password*, and *Domain*.)

Question
: The custom question to display above the input box. (Defaults are *Enter your user name*, *Enter your password*, and *Enter a domain*.)

ReadOnly
: For *Domain* fields only, setting this to *True* will prevent the user from changing the value. (Valid values are *True* and *False*. Default is *False*.)

RegEx
: The custom regular expression to use to validate the text entered into the text box. (Defaults are *[^\"/\\\[\]:;\|=,\+\*\?<>]{3,15}*, *.+*, and *[\w\-_.]+*.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [UserAuth]({% link docs/reference/actionelements.md %}#userauth)) |  |	

## InputCheckbox
Adds a checkbox to an [Input]({% link docs/reference/actionelements.md %}#input) action dialog.

CheckedValue
: The value to store in the specified variable if the checkbox is checked.

Default
: The default value for the checkbox. If this value matches the specified CheckedValue, then the checkbox will be checked.

Question*
: The question to display next to the check box.

Variable*
: The variable name to store the value of the chosen option in.

UnheckedValue
: The value to store in the specified variable if the checkbox is unchecked.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Input]({% link docs/reference/actionelements.md %}#input)) |  |

## InputChoice
Adds a drop-down list box to an an [Input]({% link docs/reference/actionelements.md %}#input) action dialog.

AlternateVariable
: The variable name to store the alternate value of the chosen option in.

AutoComplete
: Enables automatic completion of the text entered in the edit box portion of the drop-down list. (Valid values are *True* and *False*. Default is *False*.)

Default
: The default choice in the list box.

DropDownSize `2.10.1.0`{:.info}
: The maximum size of the drop-down list; i.e., the maximum number of items shown in the drop list at a single time. If there are more items in the list, the drop-down will contain a vertical scrollbar. (Default is *5*.)

Question*
: The question to display above the drop-down list box.

Required
: Whether or not a value must be selected. (Valid values are *True* and *False*. Default is *False*.)

Sort `2.9.1.0`{:.info}
: Sorts the items in the drop-down list box. (Valid values are *True* and *False*. Default is *True*.)

Variable*
: The variable name to store the value of the chosen option in.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Input]({% link docs/reference/actionelements.md %}#input)) | [Choice](#choice) |
	
## InputInfo	
Adds info text to an [Input]({% link docs/reference/actionelements.md %}#input) action dialog.

Color
: The hexadecimal color to display the text in. (Default is *#000000*[^1].)

NumberofLines
: The number of lines of text to be displayed. (Valid values are *1* and *2*. Default is *1*.)	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Input]({% link docs/reference/actionelements.md %}#input)) |  |	

[^1]: black

## InputText
Defines a text input box to add to an [Input]({% link docs/reference/actionelements.md %}#input) action dialog.

ADValidate `3.0.1.0`{:.info}
: Validates the input against Active Directory using the credentials of the account entered in a previous [UserAuth]({% link docs/reference/actionelements.md %}#userauth) action dialog. (Valid values are *Computer*, *ComputerWarning*, *User* and *UserWarning*. There is no default value. Note that the values *True* and *False* are now deprecated but for backwards compatibility, *True* equates to *Computer*.)

Default
: The default value of the text box.

ForceCase
: Force the case of entered text to upper or lower. (Valid values are *Upper* and *Lower*. Default is no value.)

Hint
: The message displayed in the tooltip when the text entered does not match the regular expression.

HScroll
: Enables horizontal scrolling of the edit box allowing up to 256 characters to be entered. (Valid values are *True* and *False*. Default is *False*.)

Password
: Hides the user input similar to entering a password. `3.0.3.0`{:.info} UI++ does not write the value entered by users to the log file when this attribute is *True*. (Valid values are *True* and *False*. Default is *False*.)

Prompt
: The prompt text displayed within the text box when it is empty.

Question*
: The question displayed above the input box.

RegEx
: The regular expression used to validate the text entered into the text box.

ReadOnly `3.0.1.0`{:.info}
: Setting this to *True* prevents the user from changing the value and the **Required** attribute is ignored. (Valid values are *True* and *False*. Default is *False*.)

Required
: Whether or not a value must be entered. (Valid values are *True* and *False*. Default is *True*.)

Variable*
: The variable name to store the resulting text in.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Input]({% link docs/reference/actionelements.md %}#input))                 |  |

## Match
Specifies the criteria for finding installed software within [SoftwareDiscovery]({% link docs/reference/actionelements.md %}#softwarediscovery) action.

DisplayName*
: The display name to match when searching for installed software. Can be a regular expression.

Variable*
: The variable to store the result of the software search in. The value will be set *True* or *False*.

Version
: The version of the software to match against.

VersionOperator
: The operator to use when comparing versions. (Default is *eq*. Valid values are *eq*, *ne*, *lt*, *lte*, *gt*, *gte*, *re*[^2]).	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [Software Discovery]({% link docs/reference/actionelements.md %}#softwarediscovery)) |  |		

[^2]: regular expression

## Messages
`3.0.1.0`{:.info} Contains custom status message text.

There are no configurable attributes for this element.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [UIpp](#uipp)                  | [Message](#message) |

## Message
`3.0.1.0`{:.info} Contains the custom text for a specific [Status Message]({% link docs/configuration/statusmessages.md %}). The value of this element contains the customized text.

Id*
: The Id of the status message to customize the text for.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Messages](#messages) |  |

## Package
Defines the properties for a Package and Program that may be displayed to the interactive user in an [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog.

Id*
: A unique identifier for the package. This Id value is referenced by a SoftwareRef element to display the Package in the [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog. This can be any identifier you want it to be including a GUID as long as it’s completely unique from other unique identifiers with the configuration XML.

IncludeId
: A semi-colon separated list of Application and Package IDs that are included when this Package is chosen. This can include hidden applications and packages.

Label*
: The actual program name of the program within ConfigMgr. This must match exactly including spelling and case.

PkgId*
: The actual package ID of the package within ConfigMgr. 	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Software](#software) |  |	

## Property
Defines a WMI property to write to or create if a new WMI class is being created as part of a [WMIWrite]({% link docs/reference/actionelements.md %}#softwarediscovery#wmiwrite) action.

Key
: Whether the property is a key property of the class. (Valid values are *True* and *False*. Default is *False*.)

Name*:
: The name of the property.

Type
: The WMI type for the property. (Valid values are any valid WMI type including *CIM_STRING* or *CIM_UINT8*. Default is *CIM_STRING*.)

Value*
: The value to assign to the property.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [WMIWrite]({% link docs/reference/actionelements.md %}#wmiwrite)) |  |		

## Set
Contains groups and references to software items to display to the interactive user in an [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog.

Name*
: The name of the Set.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [SoftwareSets](#softwaresets)          | [SoftwareGroup](#softwaregroup)        |
|                       | [SoftwareRef](#softwareref)          |

## Software
Contains the applications and packages that an [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog can reference and display to the interactive user.

There are no configurable attributes for this element.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [UIpp](#uipp)                  | [Application](#application) |
|                       | [Package](#package)         |

## SoftwareGroup
A group to display in an [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog. Software groups can contain other SoftwareGroups or SoftwareRefs.

Default
: Automatically selects this group and all of its children when the dialog is created. (Valid values are *True* and *False*. Default is *False*.)

Id*
: A unique identifier for the group.

Label*
: The label shown for the group in the AppTree action.

Required
: Same as default but the group and its children cannot be unselected. (Valid values are *True* and *False*. Default is *False*.)

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Set](#set)           | SoftwareGroup        |
| SoftwareGroup         |                      |

## SoftwareListRef
A reference to a software item ([Package](#package) or [Application](#application)) previously defined within the top-level [Software](#software) element. The referenced software item is added to the appropriate variable list defined by the parent [TSVarList]({% link docs/reference/actionelements.md %}#tsvarlist)) action. 

Id*
: The unique Id of the software item previously defined in the configuration XML file.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [TSVarList]({% link docs/reference/actionelements.md %}#tsvarlist)) |  |

## SoftwareRef
A reference to a software item ([Package](#package) or [Application](#application)) previously defined within the top-level [Software](#software) element. The reference software item is shown to the interactive user in the containing [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog.

Hidden
: This software reference is hidden from the tree. It is selected if it is set as default or required (or its parent group is set to default or required) or if it is included by an application or package that is also selected. Note that unselecting a group that contains a hidden software reference does not in turn unselect that hidden software reference. (Default is *false*.)

Id*
: The Id of the software item previously defined in the configuration XML file.

Default
: Automatically selects this software reference when the [AppTree]({% link docs/reference/actionelements.md %}#apptree) action dialog is created. (Valid values are *True* and *False*. Default is *False*.)

Required
: Same as default but the software reference cannot be unselected. (Valid values are *True* and *False*. Default is *False*.)	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Set](#set)           |                      |
| [SoftwareGroup](#softwaregroup)         |                      |

## SoftwareSets	
A container for [Set](#set) elements.

There are no configurable attributes for this element.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [AppTree]({% link docs/reference/actionelements.md %}#apptree))                 | [Set](#set) |

## Text
Defines text to display in the progress dialog of a [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action for a specific type of value retirieved by the action.

Type
: The [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) value type that the progress text applies to. Valid values are as follows:
* *Asset*
* *Domain*
* *Mgmt*
* *Net*
* *OS*
* *Security*
* *User*
* *VM*

Value
: The text to display in the progress dialog for the specified value type.

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Action](#action) (where Type = [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues))                 |  |

## UIpp
This is the root element of the configuration file.

AlwaysOnTop `2.10.3.0`{:.info}
: Configures the UI++ action dialogs to always be on top of all other top-level windows. (Valid values are *True* and *False*. Default is *True*.)

Color
: The hexadecimal color of the sidebar used in all action dialogs. (Valid values are hexadecimal RGB color values prefixed with a #. Default value is *#002147*[^3].)

DialogSidebar `2.11.0.0`{:.info}
: Shows the left sidebar on all action dialogs. (Valid values are *True* and *False*. Default is *True*.)

Flat `2.9.1.0`{:.info}
: Shows action dialogs using a flat look and feel. (Valid values are *True* and *False*. Default is *False*.)

Font `3.0.1.0`{:.info}
: The font face name of all UI++ elements (except the tree control within an [AppTree]({% link docs/reference/actionelements.md %}#AppTree) action dialog. (Valid values are name of any valid font installed on the system. Default is *Tahoma*.)

Icon
: The icon to display in every action dialog shown.

SidebarTextColor `3.0.2.0`{:.info}
: The hexadecimal color of the text in the sidebar. (Valid values are hexadecimal RGB color values prefixed with a #. Default value is *#FFFFFF*.)

Title
: The title used in the sidebar of every action dialog shown.	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
|                       | [Software](#software)|                     
|                       | [Actions](#actions)  |

[^3]: The University of Michigan blue

## Variable	
This action statically sets the value of a variable, either pre-existing or new. The value used to set the variable's value is the value of this element and not an attribute.

Name*
: The variable name to set a value for.

DontEval `2.9.5.0`{:.info}
: Do not pass the value of the element to VBScript for processing. (Valid values are *True* and *False*. Default is *False*.)	

| Valid Parent Elements | Valid Child Elements |
|:----------------------|:---------------------|	
| [Case](#case)         |                      |                     
