---
layout: article
title: Switch
aside:
  toc: true
sidebar:
  nav: docs
---

Similar to the SQL CASE statement and the C++ switch statement, the [Switch]({% link docs/reference/actionelements.md %}#switch) action is an advanced way to populate the value of single or multiple variables based on one of the following:

* A static value.
* The value of a variable.
* The value of an expression.

The action then compares the value to a series of regular expressions – the cases -- in sequential order. The first regular expression that matches the value sets the specified variables to the specified values. Specify a default case to set variables to default values when the value does not match any cases.

The following example sets the value of the TimeZone variable based upon the IP gateway detected by a [DefaultValues]({% link docs/actionconfig/defaultvalues.md %}) action. There are four separate cases, including a default case. The first case that matches the value is the one whose value is used to populate the variable.

~~~ xml
<Action Type="Switch" OnValue='Trim("%XIPGateway%")' DontEval="False" >
  <Case RegEx="10\.0\.50\.1">
    <Variable Name="TimeZone">Pacific Standard Time </Variable>
  </Case>
  <Case RegEx="10\.127(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){2}">
    <Variable Name="TimeZone">Central Standard Time</Variable>
  </Case>
  <Case RegEx="(\W|^)(10\.35\.10\.1|10\.35\.11\.1|10\.36\.10\.1|10\.36\.11\.1)(\W|$)">
    <Variable Name="TimeZone">Romance Standard Time</Variable>
  </Case>
  <Default>
    <Variable Name="TimeZone">Eastern Standard Time</Variable>
  </Default>
</Action>
~~~