# vr-testbed
This application can be used in studies to test the performance of interaction techniques in different tasks. For this purpose, it is possible to generate the tasks in consideration of multiple independent variables as well as to perform the tasks in VR to collect different measurements. The tasks can consist of manipulation (change of position, rotation and/or scaling) or selection of objects. The possible independent variables are listed further down. The possible measurements are the time required, the number of incorrect selections, the precision achieved when manipulating an object, and the positional and rotational footprint (amount of movement required to perform the task).

# Execution
For the execution of the application, the built version can be used (*Build.rar*) or the project can be executed in the play mode of Unity. In Unity the scene `EvaluationEnvironment` needs to be loaded. The generation of tasks is only possible in the editor. Unity 2019.1.14f1 was used.

**Attention:** The built version was not tested extensively. Furthermore, the application was developed for the usage of the VIVE controllers. For other controllers the SteamVR key bindings need to be adapted (*Window > SteamVR Input > Open binding UI*).

# Configuration
The application is configured using the *config.json* file in the StreamingAssets folder. The following parameters can be set here:
**userId**: The ID by which the results of the current user can be identified in the measurement file (*StreamingAssets/measurements_(manipulation|selection).csv*)
**primaryHand**: Indicates the handiness of the user which is necessary for some techniques.
**interactionTechniques**: The techniques that will be tested.

The following properties describe an interaction technique:
**name**: Name of the technique which must fit the name of the corresponding technique in the scene EvaluationEnvironment.
**maxSupportedDistance**: The distance which is supported by the technique. Only the tasks which are generated for the given distance are used. 
**supportedTaskTypes**: Determines which task types the technique supports (selection, positioning, rotating and/or scaling). Only the tasks which are generated for the given task types are used.

The tasks are saved in the files *tasks_manipulation.json* and *tasks_selection.json* in the folder StreamingAssets. The files *dummyTasks_manipulation.json* and *dummyTasks_selection.json* contain dummy tasks. If a technique does not support all tasks (e.g. when the technique does not support all distances and tasks) these dummy tasks are used to ensure each technique uses the same amount of tasks. The target number of tasks is determined by the Target Task Count properties of the `TaskController` in the scene `EvaluationEnvironment`. The file *trainingTasks.json* contains the tasks used in the training file.

# Test process
The testing process of a technique is divided into two phases. The first phase enables the tester to learn the technique. For this purpose, the tasks from the file trainingTasks.json can be carried out without time restrictions. In this phase, the evaluator has the possibility to play audio explanations of the techniques previously generated with the Google Text-To-Speech API. These are divided into up to four parts. With the right arrow key, the next part is played and with the left arrow key the previous one. If the space bar is pressed the system switches to the test phase. Here the tester sequentially executes the tasks from the corresponding file (*tasks_manipulation.json* or *tasks_selection.json*). If all tasks are completed or the space bar is pressed the system switches to pause mode, where no technique and no tasks are loaded. Pressing the space bar again switches to the training phase of the next technique. 

**Attention:** If the space bar was pressed to abort the testing process the ids of all tasks which were not finished are saved in the file *skipTasks.json*. The next test run (independent of the chosen technique) only loads these tasks. This is usefull, problems arise during a testing process but the file needs to be removed if all tasks have to be loaded. 

In the upper left corner the evaluator always sees the name of the current technique, the overall progress, the task id, the task type, and the task progress. 

# Task Generation
If the project is opened in Unity, tasks can be generated via the menu *Window > Task Generation*. The various settings are explained in the TaskGenerationWindow script. In summary, the following variables can be influenced when generating tasks:
- form of the objects (manipulation and selection)
- distance to the objects (manipulation and selection)
- number of objects (selection)
- density of the objects (selection)
- type of manipulation that means positioning, rotating and/or scaling (manipulation)
- degrees of freedom (manipulation)
- manipulation amount (manipulation)
- needed precision (manipulation)
