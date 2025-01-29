---
layout: article
title: Status Messages
aside:
  toc: true
sidebar:
  nav: docs
---

`3.0.1.0`{:.info} UI++ displays status messages near the bottom of interactive action dialogs in various scenarios to relay information to the interactive user. You can customize these messages by supplying alternate text in the configuration XML.

The following table lists the status messages that UI++ supports for customization.

| Message Id | Action | Time shown | Default |
|:----------------------|:---------------------|:---------------------| 
| COMPUTEREXISTS | [Input]({% link docs/reference/actionelements.md %}#input) | When a computer object with the entered name already exist and the AdValidate attribute is set to *Computer*. | A computer with this name already exists. Please try another name. |
| COMPUTEREXISTSWARNING | [Input]({% link docs/reference/actionelements.md %}#input) | When a computer object with the entered name already exist and the AdValidate attribute is set to *ComputerWarning*. | A computer with this name already exists but you may continue. |
| USERDOESNOTEXIST | [Input]({% link docs/reference/actionelements.md %}#input) | When a user object with the entered name does not exist and the AdValidate attribute is set to *User*. | A user with this name does not exist. Please try another name. |
| USERDOESNOTEXISTWARNING | [Input]({% link docs/reference/actionelements.md %}#input) | When a user object with the entered name does not exist and the AdValidate attribute is set to *UserWarning*. | A user with this name does not exist but you may continue. |
| AUTHENTICATING | [UserAuth]({% link docs/reference/actionelements.md %}#userauth) | When UI++ is authenticating the entered user's credentials. | Authenticating... |
| PREFLIGHTFAILED | [Preflight]({% link docs/reference/actionelements.md %}#preflight) | When any preflight checks failed. | Please check and correct the failed checks above. |
| PREFLIGHTPASSED | [Preflight]({% link docs/reference/actionelements.md %}#preflight) | When all preflight checks passed.| All checks successfully passed. |
| PREFLIGHTPASSEDWITHWARNING | [Preflight]({% link docs/reference/actionelements.md %}#preflight) | When no preflight checks failed. | All checks successfully passed although there are warnings. |

The following configuration file example shows how to do this.

~~~ xml
<UIpp ... >
...
  <Messages>
    <Message Id="COMPUTEREXISTS">Sorry, a computer already exists with that name. Please try another name.</Message>
    <Message Id="COMPUTEREXISTSWARNING">Warning, a computer with this name already exists but you may continue if desired.</Message>
    <Message Id="AUTHENTICATING">Now authenticating user ...</Message>
  </Messages>
  ...
  <Actions>
    ...
  </Actions>
</UIpp>
~~~