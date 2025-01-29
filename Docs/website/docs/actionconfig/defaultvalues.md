---
layout: article
title: Default Values
aside:
  toc: true
sidebar:
  nav: docs
---

You use the [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action within the [Actions]({% link docs/reference/allelements.md %}#actions) element. The [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action collects information from the current system and populates a series of pre-defined variables with corresponding values. Typically, the best place to put the [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action is at or near the beginning of the Actions element. This enables UI++ to initially collect the default information before performing any other actions, many of which may rely on the information collected. The [DefaultValues]({% link docs/reference/actionelements.md %}#defaultvalues) action collects values in the following categories:

* Asset
* Domain
* Management
* Network
* Operating System
* Security
* Trusted Platform Module
* Virtual Machine

You can choose at configuration time which categories that the action collects values for by specifying the **ValueTypes** attribute. Specify multiple categories using a comma-separated list. Omit the **ValueTypes** attribute or set it to **All** to retrieve all default values.

The [DefaultValues]({% link docs/reference/defaultvalues.md %}) reference page lists all categories and the values that each collects. 

<i class="fa fa-info-circle"></i> The categories names listed above are not the actual values to use for the **ValueTypes** attribute. Please see the [DefaultValues]({% link docs/reference/defaultvalues.md %}) reference page for the proper category values.
{:.note}
