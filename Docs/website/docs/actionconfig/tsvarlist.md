---
layout: article
title: Variable List
aside:
  toc: true
sidebar:
  nav: docs
---

Using this Action, UI++ creates a sequence of variables populated with either application names or package IDs and program names. These lists of variables are suitable for use within either an  [Install Application](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_InstallApplication) step or an [Install Package](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_InstallPackage) step within a task sequence.

Specify applications and packages by referring to the Id of the software item defined using the [Software]({% link docs/reference/allelements.md %}#software) element and [Application]({% link docs/reference/allelements.md %}#application) and [Package]({% link docs/reference/allelements.md %}#package) sub-elements earlier in the configuration XML. Applications and packages are added to the correct list based upon their type and in the same order specified within the [Software]({% link docs/reference/allelements.md %}#software) element.

The following snippet populates two lists: one for applications with **XApplicationsA** as its variable base and one with **XPackagesA** for packages as its variable base. 

~~~ xml
<Software>
  <Application Id="Office365ProPlus" Label="Microsoft Office 365 Pro Plus" Name="Microsoft Office 365 Pro Plus"/>
  <Application Id="AdobeAcrobatReaderDC2019" Label="Adobe Acrobat Reader DC 2019" Name="Adobe Acrobat Reader DC 2019" />
  <Application Id="Vivaldi2.7.1628.30" Label="Vivaldi 2.7.1628.30" Name="Vivaldi 2.7.1628.30" />
  <Application Id="VSCode1.3" Label="Microsoft Visual Studio Code 1.3" Name="Microsoft Visual Studio Code 1.3" />
  <Application Id="RoyalTSv5" Label="RoyalTS V5" Name="RoyalTS V" />
  <Application Id="SnagIt2019.1.3" Label="TechSmith SnagIt 2019.1.3" Name="TechSmith SnagIt 2019.1.3" />
  <Package Id=".NET4.7.2" Label=".NET Framework 4.7.2" PkgID="ONE000100" ProgramName="Install .Net 4.7.2" />
</Software>
<Actions>
  <Action Type="TSVarList" PackageVariableBase="XPackagesA" ApplicationVariableBase="XApplicationsA">
    <SoftwareListRef Id="VSCode1.3" Condition='%VSCodeFound%'/>
    <SoftwareListRef Id="RoyalTSv5" />
    <SoftwareListRef Id="AdobeAcrobatReaderDC2019" Condition='Not %FoxItFound%'/>
    <SoftwareListRef Id=".NET4.7.2" />
  </Action>
</Actions>
~~~

To control the content of the lists, use conditions. A good use case for this is to use the variables created by a [SoftwareDiscovery]({% link docs/actionconfig/softwarediscovery.md %}) action to dynamically build the list and create a mapping of software to install for refresh scenarios where you would like to restore software previously installed on a system.