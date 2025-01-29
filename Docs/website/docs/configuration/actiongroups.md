---
layout: article
title: Action Groups
aside:
  toc: true
sidebar:
  nav: docs
---

ActionGroups enable the grouping of other action elements into a single unit. There are two main reasons to group actions: organization and conditional execution of grouped actions.

You should specify a name for each ActionGroup using the **Name** attribute for organization and identification purposes. 

ActionGroups may or may not have a [Condition]({% link docs/configuration/conditions.md %}#apptree) attribute that UI++ evaluates. If an ActionGroup does have a condition specified and this condition evaluates to false, UI++ skips sub-actions.

An action group can have any number of sub-actions, and all Action element types may be part of an ActionGroup.

~~~ xml
<ActionGroup Name="Laptop Group" Condition="'%ChassisType%' = 'Laptop'">
     …
</ActionGroup>
~~~