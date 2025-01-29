---
layout: article
title: Software Discovery
aside:
  toc: true
sidebar:
  nav: docs
---

This action discovers software installed in the current Windows installation and sets a specified variable to **True** or **False** based on the existence of that software. Software is discovered from the Add/Remove Programs section of the registry and excludes system components; both 64-bit, as well as 32-bit software, is discovered on systems running 64-bit Windows but makes no explicit distinction between them. Discovery of software is first based on the display name of the software and then optionally on the version. The display name is matched using a case insensitive regular expression, and the version is matched using one of several operators as listed in the table below. These operators properly evaluate standard version numbers; e.g., 1.2.3.4, 5.6.7, and 8.9.

<i class="fa fa-info-circle"></i> Version numbers with a greater number of sub-versions are greater than those with a less number less sub-versions if the first sub-versions are otherwise equal; e.g., 5.1.50428.0 is greater than 5.1.50428.
{:.note}

| Operator | Meaning                  |
|:---------|:-------------------------|
| eq       |  Equals                  |
| gt       | Greater than             |
| gte      | Greater than or equal to |
| lt       | Less than                |
| lte      | Less than or equal to    |
| ne       | Not equal                |
| re       | Regular expression       |

The following example discovers the presence of three software items.

1. IIS Express based on a direct name match and a regular expression for the version. 
2. Microsoft Silverlight using a regular expression for the display name and the **gt** operator for the version. 
3. Snagit based on a regular expression name match; version is not considered. 

Each also sets the specified variable to either **True** or **False** based upon whether the discovery is successful or not. You can then use this variable in a [TSVarList]({% link docs/actionconfig/tsvarlist.md %}) action, an [UserAuth]({% link docs/actionconfig/userauth.md %}) action, or anywhere else this makes sense.

~~~ xml
<Action Type="SoftwareDiscovery">
  <Match DisplayName="IIS 10.0 Express" Version="10.0.\d{4}" VersionOperator="re" Variable="IISExpressFound" />
  <Match DisplayName="Microsoft Silver\w*" Version="5.1.50428" VersionOperator="gt" Variable="SilverlightFound" />
  <Match DisplayName="Snagit \d*" Variable="SnagItFound" />
</Action>
~~~