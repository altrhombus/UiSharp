---
layout: article
title: File Read
aside:
  toc: true
sidebar:
  nav: docs
---

This action does one simple thing: reads the first line from a text file and places the value of that line in a specified  variable. It will also optionally delete that line from the text file. This is very useful for generating unique system names during OSD from a list of names, prefixes, or suffixes listed in that text file.

The below snippet does the following:
1. Reads the first line from the file named **TextFile1.txt** located in the root of the P drive
2. Places the value of that line in the **MyValue** variable
3. Deletes the line from the file

The P drive, of course, needs to be accessible and the context in which UI++ runs must have read and write access to the file. You can easily do this using a [Connect to Network Folder](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_ConnectToNetworkFolder) step in the task sequence before it executes UI++.

~~~ xml
<Action Type="FileRead" DeleteLine="True" Variable="MyValue" Filename="P:\TextFile1.txt" />
~~~