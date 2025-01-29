---
layout: article
title: Info
aside:
  toc: true
sidebar:
  nav: docs
---

The [Info]({% link docs/reference/actionelements.md %}#info) action displays information to the end-user.

The action automatically adds line breaks to wrap longer lines based upon the following:

* Spaces
* Periods
* Commas
* Forward slashes
* Backslashes

{% figure caption:"An Info action dialog with a timeout" %}
  ![An Info action dialog with a timeout](/assets/images/screenshots/dialog-info-timeout.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

You can also configure a timeout on the [Info]({% link docs/reference/actionelements.md %}#info) action so that it automatically closes after the specified period. Upon automatic dismissal, you can configure the action to perform one of three activities:

* Act as if the user pressed the next button (which is the default) and move to the next action in the configuration file.
* Act as if the user pressed the cancel button and cause UI++ to return an error code of 1223 (“The operation was canceled by the user”).
* Act as if the user pressed the cancel button and cause UI++ to return a custom error code.

The following snippet creates an [Info]({% link docs/reference/actionelements.md %}#info) action interactive dialog; it includes some minor HTML formatting and a timeout. For a complete list of supported text formatting tags, see the [Text Formatting]({% link docs/reference/textformatting.md %}) section.

~~~ xml
<Action Type="Info" Name="WelcomeInfo" Title=" Welcome" ShowCancel="True" Timeout="120" TimeoutAction="Continue" >
  <![CDATA[Velkommen – Welkom - Tere tulemast – Bienvenue – Willkommen<br><u>Bienvenido</u> – <i>Välkomme</i> – <b>Welcome</b>]]>
</Action>
~~~

<i class="fa fa-info-circle"></i> Note the use of <![CDATA[ and ]]> tags to enclose the informational text. These instruct the XML parser to ignore the contained content which is key when using any embedded HTML tags as these disrupt the XML parser.
{:.note}