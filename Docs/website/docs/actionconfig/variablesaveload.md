---
layout: article
title: Variable Saving and Loading
aside:
  toc: true
sidebar:
  nav: docs
---

The [Vars]({% link docs/reference/actionelements.md %}#vars) action loads variables from a data file or saves the current variables to a data file. Saving only works if UI++ executes outside of a task sequence; loading works within a task sequence or outside of it. If used to load variables within a task sequence, all variables and their values become task sequence variables available and manipulatable as all task sequence variables are. Read-only variables (those beginning with an underscore) and variables beginning with an ‘X’ are never saved or loaded.

This action is useful for persisting variables and their values for whatever reason between executions of UI++. The primary use case is to enable UI++ to run before a refresh task sequence executed from Software Center; this is usually done using a program dependency for the task sequence. Doing this allows UI++ to show its interface without the use of ServiceUI.exe. Running UI++ in this way though prevents UI++ from saving variables as task sequence variables within the task sequence. Saving the variables during this initial execution of UI++ allows them to be re-loaded as the first step of UI++ run in a task sequence, however.

To save the existing variables (whose values are set based upon user input and other actions during the execution of UI++) to a data file, add the following element as the last action within the UI++ configuration file.

~~~ xml
<Action Type="Vars" Direction="Save"/>
~~~

To load the variables, create a separate configuration file with the following element as the first action in a configuration file (you can add other actions as needed as well; remember though that adding any user interface during a portion of the task sequence run from within Windows results in no actual user interface shown to the interactive user and the task sequence hanging as it waits for user input based).

~~~ xml
<Action Type="Vars" Direction="Load"/>
~~~

To use the above approach for a refresh task sequence, do the following.

1. Place both configuration files and UI++ into a package.

2. Create a program within this package to run UI++ using the first configuration file (use the /config parameter to specify the first configuration file). Make sure to set the program to run **Only when a user is logged on**, to **Run with administrative rights**, and to **Allow users to interact with this program** on the **Environment** tab of the program.
                      
3. Create a second program to run UI++ without any user interaction that loads the variables saved by the first program. Use the [/config]({% link docs/running/commandline.md %}#config) parameter again to specify the second configuration file.

4. On the **Advanced* tab of the properties page for the refresh task sequence, select **Run another program first** and choose the package created in step 1 and program created in step 2. Select **Always run this program first**.
  
5. Within the refresh task sequence, at or new the beginning, add an [Install Package](https://docs.microsoft.com/en-us/sccm/osd/understand/task-sequence-steps#BKMK_InstallPackage) step to run the second program created in step 3.