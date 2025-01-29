---
layout: article
title: Log File
aside:
  toc: true
sidebar:
  nav: docs
---

UI++ generates or appends to a log file named **UI++.log** every time it runs. This log records all relvant information including all significant activity that UI++ performs including XML parsing issues. If you launch UI++ and nothing happens, check this log file for details. Outside of what's in the configuration file, UI++ does not present error messages or other run-time issues to the interactive user.

The log file is in the standard ConfigMgr log file format and is best viewed using CMTrace (or the newer OneTrace included with Support Center). 

The following table lists the locations where UI++ creates its log file.

|  Environment  | Log Location |
|:---------------|:--------------|
| Within a task sequence, before the **Setup Windows and ConfigMgr** task | X:\Windows\Temp\SMSTSLog |
| Within a task sequence, after the **Setup Windows and ConfigMgr** task | %windir%\ccm\logs\SMSTSLog |
| Prestart command | X:\Windows\Temp\ |
| Outside of a task sequence | %TEMP% |


