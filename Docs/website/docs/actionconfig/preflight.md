---
layout: article
title: Preflight
aside:
  toc: true
sidebar:
  nav: docs
---

The [Preflight]({% link docs/reference/actionelements.md %}#preflight) action performs a series of checks to ensure that the system is in a valid state before UI++ continues. These checks are particularly useful to validate a system before running a task sequence, which may cause significant, irreversible changes to a system but may fail because of the current state of the system, e.g., being on battery power or connected to a wireless network.

[Preflight]({% link docs/reference/actionelements.md %}#preflight) checks use any valid VBScript expression to check the state of a system; these are called conditions. The [Preflight]({% link docs/reference/actionelements.md %}#preflight) action dialog displays each check, and its condition's evaluation result. If any condition evaluates to false, the action as a whole fails, and the user cannot proceed to the next configured action. If the user chooses to exit UI++ at this point, UI++ returns a failure code of ERROR_NOT_READY (decimal 21).

The following is a standard preflight example that checks for WLAN connectivity, whether a system is on battery, a minimum amount of memory, and whether the CPU supports Windows 8+ requirements.

~~~ xml
<Action Type="Preflight" Title="Preflight checks">
  <Check Text="WLAN Disconnected" CheckCondition='"%XWLANDisconnected%" = "True"' Description="To prevent issues during deployment, a wired connection is required." ErrorDescription="Please disconnect the wireless network from this system."/>
  <Check Text="Not on battery" CheckCondition='"%XOnBattery%" = "False"' />
  <Check Text="Minimum memory > 1GB" CheckCondition='%XHWMemory% >= 1024' />
  <Check Text="CPU Supports Windows 8+" CheckCondition='%XCPUPAE% AND %XCPUNX% AND %XCPUSSE2% = True' />
</Action> 
~~~

To add a tooltip for a check shown in the Prelight action dialog, set the **Description** attribute for that check.

{% figure caption:"A Preflight action dialog showing a Description toolip" %}
  ![A Preflight action dialog showing a Description toolip](/assets/images/screenshots/dialog-preflight-2.png)
{% endfigure %}
{: #screenshot }

To add a tooltip to the check failed icon, set the **ErrorDescription** attribute for that check.

{% figure caption:"A Preflight action dialog showing an ErrorDescription toolip" %}
  ![A Preflight action dialog showing an ErrorDescription toolip](/assets/images/screenshots/dialog-preflight-3.png)
{% endfigure %}
{: #screenshot }

To prevent UI++ from showing the [Preflight]({% link docs/reference/actionelements.md %}#preflight) action unless a failure occurs, set the **ShowOnFailureOnly** attribute to **True**.

## Warnings

In addition to errors, you can configure checks that result in a warning. Checks that result in a warning do not cause the action to fail or block the user from proceeding to the next configured action. A single check may have both a warning and an error condition; error conditions take priority over warning conditions. The following slightly modified example adds a warning condition to memory and UEFI checks. It also adds custom error and warning messages as tooltips when hovering over the check's status icon.

~~~ xml
<Action Type="Preflight" Title="Preflight checks" ShowOnFailureOnly="False" ShowBack="True">
  <Check Text="WLAN Disconnected" CheckCondition='"%XWLANDisconnected%" = "True"' Description="To prevent issues during deployment, a wired connection is required." ErrorDescription="Please disconnect the wireless network from this system."/>  <Check Text="Not on battery" ErrorDescription="Please ensure that the system is connected to AC Power." CheckCondition='"%XOnBattery%" = "False"' />
  <Check Text="Minimum memory > 1GB" CheckCondition='%XHWMemory% >= 1024' WarnCondition='%XHWMemory% >= 4096' ErrorDescription="Please ensure that the system has at least 1 GB of memory." WarnDescription="For best results, this system should have at least 4 GB of memory"/>
  <Check Text="CPU Supports Windows 8+" CheckCondition='%XCPUPAE% AND %XCPUNX% AND %XCPUSSE2% = True' />
  <Check Text="UEFI Enabled" WarnCondition='"%XSystemUEFI%" = "True"' WarnDescription="For security purposes, UEFI should be enabled."/>
</Action>
~~~

{% figure caption:"A Preflight action dialog showing a warning" %}
  ![A Preflight action dialog showing a warning](/assets/images/screenshots/dialog-preflight-4.png)
{% endfigure %}
{: #screenshot }

## Timeout

You can set a timeout on a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action so that after the specified period, UI++ automatically dismisses it. This timeout is only displayed if none of the checks in the [Preflight]({% link docs/reference/actionelements.md %}#preflight) action fail; if any checks fail, and the dialog does not timeout automatically. If there are warnings, no timeout countdown is displayed, and the dialog does not timeout unless you set the **Continue** attribute to **ContinueOnWarning**.

When the countdown timer expires, and the dialog automatically closes, you can configure the dialog to perform one of three activities using the **TimeoutAction** attribute:

* Act as if the user pressed the next button (which is the default) and move to the next action in the configuration file.
* Act as if the user canceled the action and cause UI++ to return an error code of 1223 (“The operation was canceled by the user”).
* Act as if the user canceled the action and cause UI++ to return a custom error code.

~~~ xml
<Action Type="Preflight" Title="Preflight checks" ShowOnFailureOnly="False" ShowBack="True" Timeout="300" TimeoutAction="ContinueOnWarning">
...
</Action>
~~~

{% figure caption:"A Preflight action dialog showing a timeout" %}
  ![A Preflight action dialog showing a timeout](/assets/images/screenshots/dialog-preflight-5.png)
{% endfigure %}
{: #screenshot }

## Refresh

Checks within a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action explicitly depend on the computed results of the expressions configured for each check. These expressions almost always rely on the values of variables captured in previous actions, e.g., the [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action. Thus, a refresh of the [Preflight]({% link docs/reference/actionelements.md %}#preflight) action is meaningless without also rerunning these previous actions to re-capture the values of the variables referenced in the expressions which have possibly changed.

The **Refresh** button on a Preflight action is enabled if that Preflight action meets the following criteria:

*  The [Preflight]({% link docs/reference/actionelements.md %}#preflight) action is within an ActionGroup, i.e., the Preflight action element is a child of an [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup) element.
* The [Preflight]({% link docs/reference/actionelements.md %}#preflight) action is not the first action in the [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup).
 
When the user pushes the refresh button, UI++ returns to the first action within the [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup) and proceed as normal. This reruns any actions within the [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup) that occurred before the [Preflight]({% link docs/reference/actionelements.md %}#preflight) action. All actions that populate variables that the expressions in the Preflight action reference should occur before Preflight action within the same [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup).

The following example shows a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action that only depends on the variables from a [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action. In this case, pressing the refresh button reruns the [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action and then return to the [Preflight]({% link docs/reference/actionelements.md %}#preflight) action while recomputing the check expressions defined in it.
 
~~~ xml
<ActionGroup Name="Preflight">
  <Action Type="DefaultValues" ValueTypes="Asset,OS,Network" ShowProgress="True" />
  <Action Type="Preflight" Title="Preflight checks" ShowOnFailureOnly="False" ShowBack="True" ShowCancel="True">
    <Check Text="WLAN Disconnected" Description="To prevent issues during deployment, a wired connection is required." ErrorDescription="Please disconnect the wireless network from this system." CheckCondition='"%XWLANDisconnected%" = "True"' />
    <Check Text="Not on battery" ErrorDescription="Please ensure that the system is connected to AC Power." CheckCondition='"%XOnBattery%" = "False"' />
    <Check Text="Minimum memory > 1GB" CheckCondition='%XHWMemory% >= 1024' WarnCondition='%XHWMemory% >= 40960' ErrorDescription="Please ensure that the system has at least 1 GB of memory." WarnDescription="For best results, this system should have at least 4 GB of memory"/>
    <Check Text="CPU Supports Windows 8+" CheckCondition='%XCPUPAE% AND %XCPUNX% AND %XCPUSSE2% = True' />
    <Check Text="UEFI Enabled" WarnCondition='"%XSystemUEFI%" = "True"' WarnDescription="For security purposes, UEFI should be enabled."/>
  </Action>
</ActionGroup>
 ~~~

<i class="fa fa-info-circle"></i> Using a GUI based action before and within the same [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup) as a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action works but may be awkward for the interactive user and may cause the back button to yield unexpected results after the refresh button is used. For this reason, avoid placing GUI based actions before and within the same [ActionGroup]({% link docs/reference/allelements.md %}#actiongroup) as a [Preflight]({% link docs/reference/actionelements.md %}#preflight) action.
{:.note}
