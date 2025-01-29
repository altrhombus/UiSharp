---
layout: article
title: Variable Saving and Loading
aside:
  toc: true
sidebar:
  nav: docs
---

These actions read data from WMI, storing the value in a variable, or write data to WMI. 

The [WMIRead]({% link docs/reference/actionelements.md %}#wmiread) action reads data from WMI in one of two ways:
1. Using a WMI Query Language (WQL) query.
2. Directly from a specified WMI class.
In both cases, the action uses the first instance returned from WMI to populate the designated variable. For the second method, you can specify a key qualifier property/value expression specific to the desired WMI class to return only the data from the instance that matches that value.

When writing data, the [WMIWrite]({% link docs/reference/actionelements.md %}#wmiwrite) action creates WMI namespaces and classes if necessary.

The following snippet queries WMI for the model of the system.

~~~ xml
<Action Type="WMIRead" Namespace="root\cimv2" Class="Win32_ComputerSystem" Property="model" Variable="csmodel" />
~~~

The following snippet queries WMI for the state of the **Windows Update Service**.

~~~ xml
<Action Type="WMIRead" Variable="WUAUServiceState" Namespace="root\cimv2" Class="Win32_Service" KeyQualifier='Name="wuauserv"' Property="State"/>
~~~

The following snippet does the following:
1. Opens or creates the **root\ITlocal** WMI namespace. 
2. Creates the **Local_Config** WMI class if it does not exist. 
3. Creates or updates an instance of the **Local_Config** class by setting the **ComputerName** and **Tier** values to the specified values. If an object already exists with the same key property value, then this overwrites the values in that object.

~~~ xml
<Action Type="WMIWrite" Namespace="root\ITLocal" Class="Local_Config" >
  <Property Name="ComputerName" Type="CIM_STRING" Value="%ComputerName%" Key="True"/>
  <Property Name="Tier" Type="CIM_UINT8" Value="%Tier%" Key="False"/>
</Action>
~~~