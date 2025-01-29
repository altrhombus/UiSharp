---
layout: article
title: The Back Button
aside:
  toc: true
sidebar:
  nav: docs
---

The back button gives UI++ the ability to move backward in the sequence of dialogs shown. You can selectively add the back button to an interactive dialog action by adding the **ShowBack** attribute and setting it to **True**. For example, enabling the back button on an Input action would look something like this:

~~~ xml
<Action Type="Input" Name="ClientSetupInput" Title="Client Setup" ShowBack="True">
~~~
 
You can add a back button to the following interactive actions:

* [AppTree]({% link docs/actionconfig/apptree.md %})
* [ErrorInfo]({% link docs/actionconfig/errorinfo.md %})
* [Info]({% link docs/actionconfig/info.md %})
* [Input]({% link docs/actionconfig/input.md %})
* [PreFlight]({% link docs/actionconfig/preflight.md %})
* [UserAuth]({% link docs/actionconfig/userauth.md %})

If an interactive action is the first dialog shown, UI++ does not show the back button on the action’s dialog regardless of the value of the **ShowBack** attribute.

Going back preserves all of the user’s previous entries or selections with a few exceptions:

* For [UserAuth]({% link docs/actionconfig/userauth.md %}) actions, UI++ only preserves the domain; UI++ clears the user name and password.
* For [PreFlight]({% link docs/actionconfig/preflight.md %}) actions, there’s nothing to preserve, and all checks are re-run.
* For [AppTree]({% link docs/actionconfig/apptree.md %}) actions, UI++ selects all occurrences of a selected software item; i.e., if the tree shows an application or package multiple times in different locations and the user chooses one, UI++ selects all of them upon returning to the [AppTree]({% link docs/actionconfig/apptree.md %}) interactive dialog action.

Keep in mind though that the user must press the **Next** button on an interactive dialog for UI++ to preserve any entered information or selections.

Going back jumps directly to the previous interactive action dialog that was shown and does not execute any intermediary actions; this includes skipping interactive actions that UI++ previously skipped because a condition prevented them from being shown in the first place. Using the back button does not affect the normal forward flow of actions in UI++. UI++ preserves all user entries and selections with the same exceptions as noted above.