---
layout: article
title: Values and Variables
aside:
  toc: true
sidebar:
  nav: docs
---

When running within a task sequence, UI++ stores all values directly as task sequence variables, and they are thus usable within the task sequence after UI++ exits. Additionally, all valid task sequence variables are directly usable from within UI++. All actions that set the value of a variable have a **Variable** attribute to specify which variable to create or populate with a value. The actual value that the action populates into the variable depends on the action.

If you run UI++ outside of a task sequence, then UI++ transparently uses an internal variable system. This variable system is equivalent to the task variable system except that it is not available outside of the currently running instance of UI++. If you need to persist these values for use in a future instance of UI++, you can easily add them to the registry or WMI and then reload them or use the Vars action.

## Variable Replacement

For any attribute or value of any element in the configuration file, you can insert the value of any environment variable or task sequence variable, including any that were previously created by UI++, by inserting the name of the variable surrounded by percent signs (%). If an environment variable and variable have the same name, the environment variable takes precedence.

The following are multiple examples using variable replacement.

~~~ xml
<Action Type="TSVar" Name="OSDComputerName" >"Prefix-" & Left("%XHWAssetTag%", 7)</Action>

<Action Type="TSVar" Name="OSDDomainOUName" Condition='"%XHWChassisType%" = "Laptop" Or "%XHWChassisType%" = "Desktop"'>ou=Computers,ou=%Location%,dc=domain,dc=com</Action>

<Action Type="Info" Name="FinalInfo" Title="Summary" ShowCancel="True" ShowBack="True" >
      <![CDATA[<b>System name:</b>  %OSDComputerName%<br><b>OU:</b>  %OSDDomainOUName%<br><b>Timezone:</b>  %Timezone%]]>
</Action>
~~~

<i class="fa fa-info-circle"></i> Note that there is a built-in environment variable named **ComputerName**. If you set a variable named **ComputerName** and then try to use **ComputerName**, you will not get the value that you previously set but will instead get the value of the **ComputerName** environment vartiable.
{:.note}

## Boolean Variable Negation

For any attribute that expects a Boolean value; i.e., **True** or **False**, prepending the value with an exclamation point negates the value specified. For example, the following adds a software item to an [AppTree]({% link docs/actionconfig/apptree.md %}) interactive action:

~~~ xml
<SoftwareRef Id="E6677316-BA46-4553-A8B8-0818875DFADB" Default="!%SnagitFound%"/>
~~~

The **Default** attribute for this example evaluates to the opposite of the value of the **SnagitFound** variable. Thus if **SnagitFound** is **True**, the opposite value, **False**, is instead used and vice-versa of course. If **SnagitFound** isn’t a valid Boolean value, then **False**  is passed.

Boolean variable negation does not apply to **Condition** attributes. For Conditions attributes, use the VBScript Not operator.

## Variable Editor

The Variable Editor enables the interactive user to view all set variables and modify them (except of course read-only variables which are not modifiable). To open the variable editor, push Ctrl+F2 on any interactive action dialog. There are two lists presented, one for *Read-only* variables and one for *Editable* variables.

{% figure caption:"Read-only variables in the variable editor" %}
  ![Read-only variables in the variable editor](/assets/images/screenshots/variableeditor-readonly.png)
{% endfigure %}
{: #screenshot }

{% figure caption:"Editable variablea in the variable editor" %}
  ![Editable variables in the variable editor](/assets/images/screenshots/variableeditor-editable.png)
{% endfigure %}
{: #screenshot }
                    
To modify an editable variable, click in the appropriate text box and modify the value.

<i class="fa fa-info-circle"></i> Your mileage may vary in modifying most of the built-in variables. Each has a specific purpose, and changing its value may result in various behaviors not controlled or controllable by UI++. Know what the value is and does before changing it. In general, changing values during a task sequence from the variable editor has limited (if any) production value but can be very useful for testing purposes.
{:.note}

To disable the use of the variable editor, start UI++ using the [/disabletsvareditor]({% link docs/running/commandline.md %}#disabletsvareditor) switch on the command-line. 

## Variable Dump to File

To dump a complete list of variables and their values from within UI++ to a text file, press Ctrl+F3 on any interactive action dialog. This creates a file named **UI++ Variable Dump &lt;Date&gt; &lt;Time&gt;.txt** in the same place as the UI++ log.

To disable dumping variables to a file, start UI++ using the [/disabletsvareditor]({% link docs/running/commandline.md %}#disabletsvareditor) switch on the command-line.

