---
layout: article
title: Command Line
aside:
  toc: true
sidebar:
  nav: docs
---

You can use the following optional arguments when calling UI++ from a command line.

<a name="config"></a>/config:&lt;filename&gt;
: The filename of the configuration file to use. This filename can include a path if necessary. If not specified, UI++ uses **ui++.xml** with no path specified.

  In addition to loading a local file, UI++ can load the configuration file from an HTTP or HTTPS location. To do this, specify the entire correct URL for the XML file including prefixing it with **http://** or *https://**. UI++ downloads the specified file from the URL to the current user’s TEMP directory and then loads the file from there.

  When using this command line argument note that you can use a task sequence variable to set its value. For example, if you have multiple configuration files, you can populate a task sequence variable named **MyConfig** with the filename and then use the following command line: 
  
  ~~~
  ui++.exe /config:%MyConfig%
  ~~~

/retry:&lt;count&gt;
: If you specify an HTTP or HTTPS location for the configuration file using the **/config** argument, then this switch specifies how many times UI++ retries downloading the configuration file in the event of a file download failure. UI++ pauses for five seconds between each attempt.

/fallback:&lt;filename&gt;
: If you specify an HTTP or HTTPS location for the configuration file using the **/config** argument and the download fails, UI++ loads the file specified by this argument.

<a name="disabletsvareditor"></a>/disabletsvareditor
: If you specify this switch, UI++ disables the variable editor and the dump variables to a text file ability.