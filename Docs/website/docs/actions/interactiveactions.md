---
layout: article
title: Interactive Actions
aside:
  toc: true
sidebar:
  nav: docs
---

UI++ solicits user input from the interactive user using dialogs. Each dialog type has a unique interactive action that describes what and how to collect information from the user. Each interactive action dialog type has its own set of customizable features and options.

## Interactive Action Characteristics

### Standard Characteristics
{% figure caption:"An action dialog" %}
  ![A UI++ action dialog](/assets/images/screenshots/dialog-standard.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

All user interface dialogs created by interactive actions have the same look and feel, including the following common characteristics.
 1. Flat look and feel
 2. Major title
 3. Action buttons

You can individually customize the title of each dialog. The type of interactive action and the valid activites for that interactive action determine which buttons that dialog shows. These may include some or all of the following button types:

* OK
* Cancel
* Go Back
* Retry

### Optional Characteristics
{% figure caption:"An action dialog showing optional characteristics" %}
  ![A UI++ action dialog showing optional characteristics](/assets/images/screenshots/dialog-optional.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The following are optional look and feel characteristics for dialogs created by interactive actions.
 1. Rounded corners
 2. Sidebar with title
 3. Sidebar icon

You can customize the sidebar color, title, and icon. Note that all dialogs within a single UI++ configuration share whatever values you specify for these. You do not have to supply a sidebar icon in which case the sidebar title aligns with the bottom of the sidebar.

## Interactive Actions Introduction

Interactive actions show a dialog to the interactive user and enable direct user input or provide feedback to the user. Each interactive action has multiple configurable appearance and behavior options.

UI++ is not limited to a single interactive action but instead can present multiple interactive actions and thus dialogs. Multiple interactive actions are useful to group specific types of user requests together, to prompt for different items of information, or display information to the user. The exact order, behavior, and appearance are up to you based on your requirements.

### Info
{% figure caption:"An **Info** action dialog" %}
  ![A UI++ Info action dialog](/assets/images/screenshots/dialog-info.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [Info]({% link docs/reference/actionelements.md %}#info) interactve action shows a simple informational dialog. This dialog conveys information to the user. It includes an open text area which is formatted using a subset of standard HTML tags.

[Info]({% link docs/reference/actionelements.md %}#info) actions have the following characteristics:

1.  Information text.
2.  Optional header image.
3.  Optional informational image.
4.  Optional countdown timer.

### Error Info
{% figure caption:"An **ErrorInfo** action dialog" %}
  ![A UI++ ErrorInfo action dialog](/assets/images/screenshots/dialog-errorinfo.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [ErrorInfo]({% link docs/reference/actionelements.md %}#errorinfo) interactive action is nearly identical to an [Info]({% link docs/reference/actionelements.md %}#info) action except that it only shows a Cancel button.

[ErrorInfo]({% link docs/reference/actionelements.md %}#errorinfo) actions have the following characteristics:

1.  Information text.
2.  Optional header image.
3.  Optional informational image.
4.  Cancel button (and only a cancel button).
  
### Input
User input in UI++ is mainly solicted using [Input]({% link docs/reference/actionelements.md %}#input) interactive actions. [Input]({% link docs/reference/actionelements.md %}#input) action dialogs display a dialog containing multiple text boxes, drop-down list boxes, checkboxes, or simple informational items. Collectively, these are referred to as input elements. If you configure more input elements than will fit on a single dialog, only the first elements will be shown based on the size of the dialog and the type of input elements.

The OK button on an [Input]({% link docs/reference/actionelements.md %}#input) action dialog is disabled until all input elements match their defined regular expressions and all drop-down list boxes that require a selection have a valid selection.

#### Text Input
{% figure caption:"An **Input** action dialog showing **InputText** elements" %}
  ![A UI++ Input action dialog showing InputText elements](/assets/images/screenshots/dialog-input-InputText.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

Users enter text into [InputText]({% link docs/reference/allelements.md %}#InputText) elements. [InputText]({% link docs/reference/allelements.md %}#InputText) elements have the following characteristics:

1.	A prompt/question.
2.	Tooltip with a message describing the correct format of the text.
3.	Exclamation icon indicating that the field is not in the correct format or input is required.
4.	[InputText]({% link docs/reference/allelements.md %}#InputText) elements are outlined in red when they do not have focus and do not contain text matching the correct format.
5.	[InputText]({% link docs/reference/allelements.md %}#InputText) elements with no text display a prompt to help the user know what to enter.
6.	Text within an [InputText]({% link docs/reference/allelements.md %}#InputText) element that does not match the correct format is colored red.
7.	Optional regular expression defining the exact format of acceptable text (not shown).

#### Choice Input
{% figure caption:"An **Input** action dialog showing an **InputChoice** element" %}
  ![A UI++ Input action dialog showing an InputChoice element](/assets/images/screenshots/dialog-input-InputChoice.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

An [InputChoice]({% link docs/reference/allelements.md %}#InputChoice) element is presented as a drop-down list box. Each [InputChoice]({% link docs/reference/allelements.md %}#InputChoice) element has the following  characteristics:

1.	A prompt/question
2.	An exclamation icon indicating that a choice has been made when one is required.
3.	A drop-down list of customizable choices.
1.  Optional auto-completion.
2.  Optional visible list size.
3.  Optional automatic sorting of list items.

#### Checkbox Input
{% figure caption:"An **Input** action dialog showing a **CheckInput** element and an **InputInfo** element" %}
  ![A UI++ Input action dialog showing a CheckInput element and an InputInfo element](/assets/images/screenshots/dialog-input-InputCheckbox.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

Ineractive users make a binary choice using [InputCheckbox]({% link docs/reference/allelements.md %}#InputCheckbox) elements which are presented as standard checkboxes.

Each [InputCheckbox]({% link docs/reference/allelements.md %}#InputCheckbox) element has the following characteristics:

1.	A prompt/question

#### Info Input
[InputInfo]({% link docs/reference/allelements.md %}#inputinfo) elements present static text to the interactive user. They are classified as input elements because they appear on [Input]({% link docs/reference/actionelements.md %}#input) action dialogs.

Each [InputInfo]({% link docs/reference/allelements.md %}#inputinfo) element has the following characteristics.

1.  Text color.
2.  Single or double line of text.

### Preflight
{% figure caption:"A **Preflight** action dialog" %}
  ![A UI++ Preflight action dialog](/assets/images/screenshots/dialog-preflight.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [Preflight]({% link docs/reference/actionelements.md %}#preflight) interactive action performs a series of checks, generally as a precursor to additional activity. Check evaluation may result in success, warning, or failure based on thresholds and criteria configured for each check. Check evaluation results are displayed to the interactive user so that they can address any issues. Failed checks block UI++ from proceeding to the next configured action.

[Preflight]({% link docs/reference/actionelements.md %}#preflight) actions have the following characteristics:

1.  A list of evaluated checks.
2.  Result indicators for each evaluated check.
3.  Tooltips for warning and error indicators.
4.  Additional information indicators with tooltips to display the additional information.
5.  Overall preflight evaluation result text.
6.  Optional retry button to re-evaluated all checks and display updated results.
7.  Optional countdown timer (not shown).

### Application Tree
{% figure caption:"An **AppTree** action dialog" %}
  ![A UI++ AppTree action dialog](/assets/images/screenshots/dialog-apptree.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [AppTree]({% link docs/reference/actionelements.md %}#apptree) interactive action displays a dialog with a selection tree containing pre-defined software items. These software items may include applications and packages as well as pre-defined groupings of software items. The interactive user chooses which of the software items they wish to install. Note that this action is only beneficial when used in combination with a ConfigMgr-based task sequence.

[AppTree]({% link docs/reference/actionelements.md %}#apptree) actions have the following characteristics:

1.  Groups.
2.  Subgroups.
3.  Software items (applications or packages).
4.  Default software items and groups.
5.  Required software items and groups.
6.  Hidden software items (not shown).

### User Authentication
{% figure caption:"An **UserAuth** action dialog" %}
  ![A UI++ UserAuth action dialog](/assets/images/screenshots/dialog-userauth.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [UserAuth]({% link docs/reference/actionelements.md %}#userauth) interactive action displays a fixed set of three fields to the user. The user must supply their Active Directory domain credentials to authenticate and proceed with the rest of the actions defined for UI++.

[UserAuth]({% link docs/reference/actionelements.md %}#userauth) actions have the following characteristics:

1.  Customizable regular expression based validation of all fields.
2.  Masked password entry.
3.  Drop-down selection or free-text entry of domain to authenticate against.
4.  Authentication attempt message.
5.  Customizable prompt text for each field (not shown).
6.  Maximum number of retries (not shown).
7.  User immediate group membership retrieval (not shown).
8.  User authorization based on immediate group membership (not shown).

### Info (Full-Screen)
`Preview`{:.warning} `3.0.1.0`{:.info}
{% figure caption:"An **UserAuth** action dialog" %}
  ![A UI++ InfoFullScreen action window](/assets/images/screenshots/dialog-infofullscreen2.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [InfoFullScreen]({% link docs/reference/actionelements.md %}#infofullscreen) interactive action displays a full-screen, always on top window with a single line of text and a banner image.

[InfoFullScreen]({% link docs/reference/actionelements.md %}#infofullscreen) actions have the following characteristics:

1.  Customizable banner image.
2.  Customizable message text.
3.  Infinite progress bar.