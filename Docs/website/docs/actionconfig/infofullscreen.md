---
layout: article
title: Info (Full-Screen)
aside:
  toc: true
sidebar:
  nav: docs
---

<i class="fa fa-info-circle"></i> The **InfoFullScreen** dialog action is currently a preview feature that works but may not be complete and is subject to change.
{:.note}

The [InfoFullScreen]({% link docs/reference/actionelements.md %}#infofullscreen) action displays information to the end-user using a full-screen window instead of a standard UI++ dialog. This full-screen window displays an optional image, a single line of text, and an infinite progress bar.

{% figure caption:"An InfoFullScreen action full-screen window" %}
  ![An Info action dialog with a timeout](/assets/images/screenshots/dialog-infofullscreen.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

To dismiss the full-screen window, press **Ctrl** + **Alt** + **F10**. This is not currently configurable.

The following snippet creates an [InfoFullScreen]({% link docs/reference/actionelements.md %}#infofullscreen) action full-screen dialog.

~~~ xml
<Action Type="InfoFullScreen" Image="http://home.configmgrftw.com/images/ElkwaterUnited-big.png">Glorious things are happening in the background.</Action>
~~~
