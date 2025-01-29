---
layout: article
title: Configuration File
aside:
  toc: true
sidebar:
  nav: docs
---

You configure UI++ by creating an XML file. This XML file contains a series of [Action]({% link docs/actions/introduction.md %}) elements that define the exact UI++ behavior which is almost completely open-ended. By default, UI++ loads its configuration from a file named **ui++.xml**, but you can specify an alternate file using the [/config]({% link docs/running/commandline.md %}#config) parameter at the command-line.

UI++ does not check the configuration file against an XSD schema so ignores extraneous or out of place elements. This is not guaranteed though, so definitely test your configuration file outside of OSD and before you use it production. Also, keep in mind that XML is case-sensitive.