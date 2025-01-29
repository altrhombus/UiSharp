---
layout: article
title: Error Info
aside:
  toc: true
sidebar:
  nav: docs
---

{% figure caption:"An ErrorInfo action dialog" %}
  ![An ErrorInfo action dialog](/assets/images/screenshots/dialog-errorinfo-2.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

The [ErrorInfo]({% link docs/reference/actionelements.md %}#errorinfo) interactive action displays a final error message to the user. Configuration of the action is nearly identical to that of the Info interactive action. The action always shows a cancel button, a back button is optional, and it automatically adds line breaks to wrap longer lines based upon the following:

* Spaces
* Periods
* Commas
* Forward slashes
* Backslashes

The following snippet creates an error dialog box to inform the user of a fatal error or issue; it includes some minor text formatting. For a complete list of supported text formatting tags, see the [Text Formatting]({% link docs/reference/textformatting.md %}) section.

~~~ xml
<Action Type="ErrorInfo" Name="SomethingBad" Title="Something went wrong">
  <![CDATA[I’m sorry, but the computer overlords are not happy with you today and this wizard cannot continue.<br><br>Have a nice day.]]>
</Action>
~~~

<i class="fa fa-info-circle"></i> Note the use of <![CDATA[ and ]]> tags to enclose the informational text. These instruct the XML parser to ignore the contained content which is key when using any embedded HTML tags as these disrupt the XML parser.
{:.note}