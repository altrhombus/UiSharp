---
layout: article
title: Random String
aside:
  toc: true
sidebar:
  nav: docs
---

This action sets the value of a variable to a random string of characters. You can configure the length of this random string as well as define the characters allowed in the string. You can use this variable and its value in the same way as all other [variables]({% link docs/configuration/valuesandvariables.md %}) within UI++ or a task sequence.

The following snippet generates a string of length 6 and populates the variable named *RandomName* with this string.

~~~ xml
<Action Type="RandomString" Name="RandomName" />
~~~

The following snippet generates a string of length 12 using only numbers and populates the variable named *RandomName* with this string.

~~~ xml
<Action Type="RandomString" Name="RandomName" Length="12" AllowedChars="0123456789" />
~~~
