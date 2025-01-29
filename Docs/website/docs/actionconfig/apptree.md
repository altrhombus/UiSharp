---
layout: article
title: Application Tree
aside:
  toc: true
sidebar:
  nav: docs
---

This action displays a customizable tree to the interactive user where they can choose packages and applications to install. You can use multiple AppTree actions as necessary.

<i class="fa fa-info-circle"></i> The AppTree interactive action has no value outside of a task sequence.
{:.note}

<i class="fa fa-info-circle"></i> The AppTree interactive action does not install or initiate the installation of any software. It merely populates task sequence variables which are then later used by the appropriate and applicable task sequence steps to install the chosen software.
{:.note}

You define software items for the AppTree action in three phases.

1. Using Application or Package elements within the top-level Software element to define all possible software items. These elements define the absolute properties for a specific software item including the label (also known as the display name) as well as the ConfigMgr Application name or the ConfigMgr package ID and program name. The Application name or package ID and program name must exactly match what is defined in ConfigMgr. 

    The order of the Application and Package elements also defines the installation order as this dictates the order in which the action creates the resulting variables. Note however that order is only relevant within either applications or packages as a ConfigMgr task sequence installs all applications in one fell swoop and packages in one fell swoop.

    You must use a unique value for the Id attribute of each Application or Package element; you can use any unique value as long as it unique within the configuration file, including GUIDs. Visual Studio includes a GUID generator, and you can find many different ones on the web as well. GUIDs can, of course, increase the difficulty of reading and deciphering the intent of the XML file though so consider this when selecting your standard unique IDs.

    <i class="fa fa-info-circle"></i> The top-level Software element is a child of the UIpp element and thus a sibling to the Actions element.
    {:.note}

    ~~~ xml
    <UIpp Title="UI++" Icon="UI++2.ico"> 
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
        …
      </Actions>
    </UIpp>
    ~~~

2. Next, define SoftwareRef and SoftwareGroup elements inside a Set element. SoftwareGroup elements define groups to show within the tree and can contain nested SoftwareGroup elements as well as SoftwareRef elements. SoftwareRef elements are references to Application or Package elements created in step 1; UI++ matches them using the Id attribute. The order of SoftwareRef and SoftwareGroup elements determines display order only and not application installation order.

    The AppTree action populates one of two task sequence variable lists based on the software items chosen by the interactive user – one variable for selected applications and one for selected packages. These task sequence variable lists use a base variable name with a sequence number appended. This sequence number is two digits for applications and three digits for packages starting with 01 or 001, and incremented for each additional application or package selected. By default, the two base variables are **XApplications** and **XPackages**, but you can override these using the ApplicationVariableBase or PackageVariableBase attributes.

    ~~~ xml
    <Actions>
      …
      <Action Type="AppTree" Title="Please choose your software">
        <SoftwareSets>
          <Set Name="Default">
            <SoftwareGroup Id="952025F7-BC5D-4D1C-960C-002B77323479" Label="Required" Required="True">
              <SoftwareRef Id="Office365ProPlus" />
              <SoftwareRef Id=".NET4.7.2" Hidden="True"/>
            </SoftwareGroup>
            <SoftwareGroup Id="07827D9D-8B57-444E-AC86-08D6DF527DC9" Label="Recommended" Default="True">
              <SoftwareRef Id="AdobeAcrobatReaderDC2019" />
              <SoftwareRef Id="Vivaldi2.7.1628.30" />
            </SoftwareGroup>
            <SoftwareGroup Id="71354335-19C7-4E12-A3D4-1B48EC91E7B4" Label="Other">
              <SoftwareRef Id="VSCode1.3" />
              <SoftwareGroup Id="7DEFDE36-5414-4109-9337-17C317BDFC8A" Label="Requires License">
                <SoftwareRef Id="RoyalTSv5" />
                <SoftwareRef Id="SnagIt2019.1.3" />
              </SoftwareGroup>
            </SoftwareGroup>
          </Set>
        </SoftwareSets>
      </Action>
      …
    </Actions>
    ~~~

3. Supply the base variable list names to an Install Application and an Install Package step that occur later in the task sequence during the Windows portion. For more details on using base variables, see the official documentation for the [Install Application](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_InstallApplication) step and [Install Package](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_InstallPackage) step.

{% figure caption:"Using an Install Application Task" %}
![Using an Install Application Task](/assets/images/screenshots/tasksequence-installapplication.png)
{% endfigure %}
{: #screenshot }

{% figure caption:"Using an Install Package Task" %}
![A UI++ Preflight action dialog](/assets/images/screenshots/tasksequence-installpackage.png)
{% endfigure %}
{: #screenshot }
