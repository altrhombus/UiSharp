---
layout: article
title: User Authentication
aside:
  toc: true
sidebar:
  nav: docs
---

{% figure caption:"A basic UserAuth action dialog" %}
  ![A basic UserAuth action dialog](/assets/images/screenshots/dialog-userauth-2.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

Use this action to present a user authentication dialog to the interactive user. It contains three fixed text input fields: User name, Password, and Domain. The action authenticates the entered user name and password against the domain specified (which should be in FQDN format).
If successful, the next action in the configuration XML is processed. Additionally, the action populates the following vairables with the appropriate values:

* **XAuthenticatedUser**
* **XAuthenticatedUserDisplayName** 
* **XAuthenticatedUserDomain** 

Use the **MaxRetryCount** attribute to set a maximum number of allowed authentication attempts.  If the user fails to authenticate in this many tires, UI++ returns an access denied (5) error code which you can validate in a task sequence using the **_SMSTSLastActionRetCode** task sequence variable or by checking the process's exit code using another method.

Using the **Group** attribute, you can specify one or more AD security groups using a semi-colon separated list. If specified, in addition to authentication, the authenticating user account must also be a member of one of the groups. This provides an authorization mechanism in addition to the authentication.

{% figure caption:"A UserAuth action dialog with customized prompts and a domain list" %}
  ![A UserAuth action dialog with customized prompts and a domain list](/assets/images/screenshots/dialog-userauth-3.png)
{% endfigure %}
{: .figure--floatright }
{: #screenshot }

Optionally, if authentication is successful, a list of all groups that the authenticated user is a member of is set as the value of a variable named **XAuthenticatedUserGroups**. This value is a comma-separated list of groups in the usual &lt;domain&gt;\&lt;group name&gt; format. You can use this variable just like any other variable (within UI++ or the rest of the task sequence) including within conditions.

If desired, you can individually customize the prompt, question, and hint used for each field as well as the regular expression using an embedded **Field** element. Additionally, instead of using a text box for the domain name, you can use a drop-down list to limit the choices given to the user.

#### Basic example

~~~ xml
<Action Type="UserAuth" Title="User Authentication" Domain="lab1.com" MaxRetryCount="5" Group="OSD Group" GetGroups="True" />
~~~

#### Example of using XAuthenticatedUserGroups in a condition

~~~ xml
<Action Type="Info" Name="myInfo" Title="Welcome %XAuthenticatedUser%" Condition='InStr ("%XAuthenticatedUserGroups%" &amp; ",", "Test,") > 0'>
  <![CDATA[Velkommen – Welkom - Tere tulemast – Bienvenue – Willkommen – Bienvenido – Välkommen]]>
</Action>
~~~

#### Example of selectively customizing the prompt, hint, question, and regular expression of the fields

~~~ xml
<Action Type="UserAuth" Title="User Authentication" Domain="lab1.configmgrftw.com" Group="OSD Group" GetGroups="False" MaxRetryCount="5">
  <Field Name="Username" Prompt="Custom Username Prompt" Hint="Custom Username Hint" RegEx="[\w\-_.]+"/>
  <Field Name="Domain" Question="Custom Domain Question" />
</Action>
~~~

#### Example using a domain list

~~~ xml
<Action Type="UserAuth" Title="User Authentication" Domain="lab300.configmgrftw.com" GetGroups="False" MaxRetryCount="5" DisableCancel="False">
  <Field Name="Domain" List="lab300.configmgrftw.com,lab200.configmgrftw.com" AutoComplete="true"/>
</Action>
~~~