---
layout: article
title: RegRead and RegWrite
aside:
  toc: true
sidebar:
  nav: docs
---

These actions read data from the registry, storing the value in a variable or write data to the registry.

The following snippet reads the **CurrentVersion** value from the key **SOFTWARE\Microsoft\Windows NT\CurrentVersion** in the **HKLM** hive.

~~~ xml
<Action Type="RegRead" Hive="HKLM" Key="SOFTWARE\Microsoft\Windows NT\CurrentVersion" Value="CurrentVersion" Variable="CurrentVersion" />
~~~

The following snippet writes a string value named **DefaultVal** to the **SOFTWARE\ConfigMgrFTW** key in the **HKLM** hive.

~~~ xml
<Action Type="RegWrite" Hive="HKLM" Key="Software\ConfigMgrFTW" Value="DefaultVal" ValueType="REG_SZ">Nicky Romero</Action>
~~~
