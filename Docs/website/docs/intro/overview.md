---
layout: article
title: Overview
aside:
  toc: true
sidebar:
  nav: docs
lang: en
---
**UI++** does whatever you want it to do. Ultimately, its primary purpose in life is to gather info from a system or an interactive user and based upon this info, write values to one of the following:
- Task sequence variables
- The registry
- WMI

Writing the values to task sequence variables during a task sequence allows those values to be consumed later in the task sequence for things like naming the system, setting the time zone, or installing software. 

Writing the values to the registry or WMI allows those values to be consumed outside of a task sequence including by hardware inventory.

You initiate **UI++** like any other executable inside of a task sequence or within Windows. Once initiated, **UI++** performs a series of activities called actions based on a configuration file that you create and supply. This configuration file is an XML file that defines all behavior of **UI++**. Possible actions include any of the following which may occur in any order including multiple occurrences as necessary:
- Showing a Dialog
  - Error Info
  - User Info
  - User Input
  - Application Selection Tree
  - Active Directory Authentication
- Creating default task sequence variables based on the current system state
- Reading from the registry
- Writing to the registry
- Querying WMI
- Creating a WMI namespace, class, or object
- Setting a task sequence variable

**UI++** is mainly designed for use with Microsoft System Center Configuration Manager (ConfigMgr) and it should work fine with all versions of ConfigMgr (although I highly recommend using the latest version of ConfigMgr Current Branch). For use outside of a task sequence, UI++ should work on any supported versions and edition of Windows.