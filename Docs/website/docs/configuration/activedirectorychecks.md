---
layout: article
title: Active Directory Checks
aside:
  toc: true
sidebar:
  nav: docs
---

`3.0.1.0`{:.info} UI++ can check the value of an **InputText** item within an [Input]({% link docs/reference/actionelements.md %}#input) interactive action against Active Directory. This check determines whether or not a user or computer object exists in Active Directory with a name based on the value entered by the user. UI++ uses the credentials of the user authenticated in a preceding [UserAuth]({% link docs/reference/actionelements.md %}#userauth) interactive action to perform these checks.

There are four possible checks, as summarized in the following table.

| Check name | Validation | Result if false |
|:----------------------|:---------------------|:---------------------| 
| Computer | Checks if a computer object does not exist with the specified name | Failure |
| ComputerWarning | Checks if a computer object does not exist with the specified name | Warning |
| User | Checks if a user object exists with the specified name | Failure |
| UserWarning | Checks if a user object exists with the specified name | Warning |

UI++ performs the check when the user presses the next button on the [Input]({% link docs/reference/actionelements.md %}#input) dialog. For failures, UI++ does not dismiss the dialog and forces the user to change the field's value. For warnings, UI++ does not dismiss the dialog but allows the user to press the next button to continue. In both cases, UI++ displays a status message indicating an issue and also flags the field.

Only one **InputText** item per [Input]({% link docs/reference/actionelements.md %}#input) interactive action dialog should be marked for validation by UI++. If multiple are flagged in the configuration, only the last field is actually validated.

The following example displays a [Input]({% link docs/reference/actionelements.md %}#input) interactive action dialog with three fields. UI++ validates the value of the third field against Active Directory. If a computer object already exists in Active Directory with this name, UI++ display a warning to the end user but allows them to continue by pressing the next button again.

~~~ xml
<Action Type="Input" Name="Input1" Title="System Information" >
  <TextInput Prompt="Primary Administrator" Hint="Enter the name of primary administrator for this system" RegEx=".*" Variable="PrimaryAdmin" Question="Primary administrator for this system" />
  <TextInput Prompt="Primary Application or Role" Hint="Enter the name of the primary application on or role for this system" RegEx=".*" Variable="PrimaryApp" Question="Primary application or role for this system" Required="False"/>
  <TextInput Prompt="System Name" Hint="Enter the name for this system" RegEx="[/w/d-]{3-15}" Variable="SystemName" Question="System Name" Required="True" ADValidate="ComputerWarning"/>
</Action>
~~~