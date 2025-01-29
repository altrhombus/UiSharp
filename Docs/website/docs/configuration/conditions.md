---
layout: article
title: Conditions
aside:
  toc: true
sidebar:
  nav: docs
---

You can configure a condition for every UI++ element, including actions within the configuration file. UI++ evaluates the condition when it reaches an element; if the condition evaluates to false, then the element, including any sub-elements, is skipped by  UI++.  Attaching conditions to actions and other elements enables UI++ dynamically show or perform actions or populate other data based on the runtime environment and user input.

Conditions consist of any valid VBScript expression and generally evaluate the value of variables against some other value. Because conditions are VBScript all of the functions of VBScript are available, including standard string processing and math functions.
To add a condition to an element, add the Condition attribute.

Here are a few example conditions:

~~~ xml
<Action Type="TSVar" Name="Company" Condition='"%OSDComputerName%" = "TheBoss"'>"Acme, Inc."</Action>

<Choice Option="Maybe" Value="MAYBE" Condition='Len("%FirstAnswer%") = "5"'/>

<InputText Variable="FourthAnswer" Condition='"%SecondAnswer%" = "Dos"' Question="What is your fourth answer?" />
~~~

Notice that the XML **Condition** attribute's value uses single quotes instead of double-quotes to define its value. Using single-quotes is perfectly valid for XML and is required because VBScript surrounds string literals with double-quotes. Notice also that the second example uses a VBScript function, **Len**, to evaluate the condition.

Within a condition, always surround a variable name with double-quotes to ensure that VBScript treats the variable's value as a string. Without doing this, UI++ and VBScript cannot evaluate your condition successfully.

<i class="fa fa-info-circle"></i> When comparing regionalized decimal values, like those returned for the *XSystemDriveTotalSize* and *XSystemDriveFreeSpace* values by the [DefaultValues]({% link docs/reference/defaultvalues.md %}) action, use the VBScript **CDbl** function to properly handle the localized formating; i.e., VBScript fails to parse numbers using anything except a dot (.) as the decimal separator.
Example: Condition='CDbl("%XSystemDriveFreeSpace%") > 10'
{:.note}