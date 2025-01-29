---
layout: article
title: Variables
aside:
  toc: true
sidebar:
  nav: docs
---

This action creates new variables with the specified name and value. You use it to create or populate variables from static values or to set variable values dynamically. The action uses the value of the element to set the variable's value and not the value of an attribute.

The following snippet puts the static value "Finance" into the BusinessUnit variable.

~~~ xml
<Action Type="TSVar" Name="BusinessUnit">Finance</Action>
~~~

You can also use the full power of VBScript to set the value of the variable.

~~~ xml
<Action Type="TSVar" Name="LocationCode">Left("Amsterdam",5)</Action>
~~~

The value specified is initially treated as a VBScript expression. If VBScript cannot evaluate the value, then the simple text of the value is placed in the variable; however, if the value is a valid VBScript expression, VBScript evaluates it. Seemingly simple text like **abc-def** is actually valid VBScript (because the "-" is a minus sign) and the VBScript evaluation results in the variable's value set to zero. To avoid this, enclose the value in double-quotes (") or set the **DontEval** attribute to **True**.

~~~ xml
<Action Type="TSVar" Name="OSDComputerName">"Finance-123"</Action>
~~~