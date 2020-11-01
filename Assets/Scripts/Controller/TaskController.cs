using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Random = UnityEngine.Random;

/// <summary>
/// Controls the generation and execution of the tasks.
/// </summary>
public class TaskController : Singleton<TaskController>
{
    public Player player;
    public SteamVR_Action_Boolean grabPinchAction;
    public SteamVR_Action_Boolean grabGripAction;
    public List<GameObject> prefabs;
    public List<Material> prefabMaterials;
    public List<Material> transparentPrefabMaterials;
    public AudioSource correctAudioSource;
    public AudioSource incorrectAudioSource;
    public AudioSource timeOverAudioSource;
    public AudioSource instructionsAudioSource;
    public ParticleSystem correctAnimation;
    public GameObject preTaskObjectPrefab;
    public GameObject interactionTechniques;
    public Transform taskObjectsParent;
    public Transform returnRoom;
    public SteamVR_Action_Boolean debugMoveOnAction;
    public StartAreaValidation startAreaValidation;
    public bool debugMode;
    public GameObject floor;
    public int targetTaskCountManipulation = 90;
    public int targetTaskCountSelection = 81;

    public List<GameObject> currentGameObjects { get; set; }
    
    private List<InteractionTechniqueConf> chosenTechniques;
    private Dictionary<TaskTypeDistance,List<Task>> tasks;
    private List<TaskTypeDistance> taskTypeOrder;
    private List<Task> dummyTasks;
    private Task currentTask;
    private TaskPhase currentPhase;
    private GameObject currentTechnique;
    private GameObject noneTechnique;
    private Configuration config;
    private Vector3 userStartPosition;
    private int taskCount;
    private int techniqueCount;
    private bool taskIsFinishing;
    private int currentInstruction;
    private bool grabEnabled;
    private bool isMovingOn;
    
    void Awake()
    {
        currentGameObjects = new List<GameObject>();
        
        // load and validate the interaction techniques which are chosen for the current run
        config = JsonHelper.DeserializeFromFile<Configuration>(Path.Combine(Application.streamingAssetsPath, "config.json"));
        chosenTechniques = new List<InteractionTechniqueConf>();
        foreach (InteractionTechniqueConf it in config.interactionTechniques)
        {
            if (interactionTechniques.transform.Find(it.name) != null)
                chosenTechniques.Add(it);
            else
                Debug.LogWarning("Technique " + it + " is not supported");
        }

        // set technique count for status text
        techniqueCount = chosenTechniques.Count;

        // start with pause
        currentPhase = TaskPhase.Pause;

        // update status text
        StatusTextController.Instance.UpdateStatusText(currentPhase.ToString());

        // find the interim technique
        noneTechnique = interactionTechniques.transform.Find("None").gameObject;

        // add butten listener to finish task
        debugMoveOnAction.AddOnStateDownListener(FinishTaskActionCallback, GetPrimaryHand().handType);

        // add debug button to move on with a controller button
        if (debugMode)
            debugMoveOnAction.AddOnStateDownListener(MoveOnActionCallback, GetPrimaryHand().otherHand.handType);
    }
    
    public bool isSelectionStudy()
    {
        return config.studyType.Equals("selection");
    }

    private void MoveOnActionCallback(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        MoveOn();
    }

    private void FinishTaskActionCallback(SteamVR_Action_Boolean fromaction, SteamVR_Input_Sources fromsource)
    {
        if (taskIsFinishing || currentTask == null || currentTask.taskTypes.HasFlag(TaskType.Selection) || 
            currentGameObjects == null || currentGameObjects.Count == 0)
            return;
        
        // check whether the objects position, rotation and scale is similar to the end object
        ManipulationTask manipulationTask = (ManipulationTask) currentTask;
        Transform startObj = currentGameObjects[0].transform;
        Transform endObject = currentGameObjects[1].transform;
        float posDiff = Vector3.Distance(endObject.position, startObj.position);
        float rotDiff = Quaternion.Angle(endObject.rotation, startObj.rotation);
        float scaleDiff = Mathf.Abs(endObject.lossyScale.x - startObj.lossyScale.x);
        Debug.Log(posDiff + " " + rotDiff + " " + scaleDiff);
        bool isScaled = currentPhase == TaskPhase.Training || 
            Vector3.Distance(manipulationTask.startObject.scale, manipulationTask.endObject.scale) < 0.01f || 
            Vector3.Distance(startObj.localScale, manipulationTask.startObject.scale) > 0.01f;
        if (posDiff <= manipulationTask.PositioningTolerance && rotDiff <= manipulationTask.RotatingTolerance &&
            scaleDiff <= manipulationTask.ScalingTolerance && isScaled)
        {
            taskIsFinishing = true;
            GetInteractionTechnique().ReleaseObject();
            currentTechnique.BroadcastMessage("TaskSolved", startObj.position);
            
            if (currentPhase == TaskPhase.Tasks)
                MeasurementController.Instance.StopMeasurement(config, currentTechnique.name,
                (ManipulationTask) currentTask, posDiff, rotDiff, scaleDiff);
            FadeOutAndRemoveTaskObjects(FadeInNextTaskObjects);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
           MoveOn();

        if (Input.GetKeyDown(KeyCode.LeftControl) && currentPhase != TaskPhase.Pause)
            FinishTask();

        if (currentPhase == TaskPhase.Training)
            PlayAudioInstructions();

        if (currentTask != null && currentPhase == TaskPhase.Tasks && MeasurementController.Instance.GetElapsedSeconds() >= currentTask.timeLimit)
            FinishTaskOnTimeOut();
    }

    private void PlayAudioInstructions()
    {
        InteractionTechnique it = GetInteractionTechnique();
        AudioClip[] instructions = it.GetInstructions();
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentInstruction < instructions.Length - 1)
            {
                currentInstruction++;
                instructionsAudioSource.Stop();
                instructionsAudioSource.clip = instructions[currentInstruction];
                instructionsAudioSource.Play();
                UpdateStatusText();
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentInstruction > -1)
            {
                if (currentInstruction > 0)
                    currentInstruction--;
                instructionsAudioSource.Stop();
                instructionsAudioSource.clip = instructions[currentInstruction];
                instructionsAudioSource.Play();
                UpdateStatusText();

            }
        }
    }

    private void FinishTaskOnTimeOut()
    {
        if (currentTask.taskTypes.HasFlag(TaskType.Selection))
        {
            MeasurementController.Instance.StopMeasurement(config, currentTechnique.name, (SelectionTask) currentTask, success: false);
        }
        else
        {
            float posDiff = Vector3.Distance(currentGameObjects[0].transform.position,
                currentGameObjects[1].transform.position);
            float rotDiff = Quaternion.Angle(currentGameObjects[0].transform.rotation,
                currentGameObjects[1].transform.rotation);
            float scaleDiff = Mathf.Abs(currentGameObjects[0].transform.lossyScale.x -
                                        currentGameObjects[1].transform.lossyScale.x);
            MeasurementController.Instance.StopMeasurement(config, currentTechnique.name,
                (ManipulationTask) currentTask, posDiff, rotDiff, scaleDiff, false);
        }

        FinishTask();
    }

    private void FinishTask()
    {
        taskIsFinishing = true;
        GetInteractionTechnique().ReleaseObject();
        taskIsFinishing = false;
        FadeOutAndRemoveTaskObjects(FadeInNextTaskObjects);
        timeOverAudioSource.Play();
    }

    private void MoveOn()
    {
        if (isMovingOn)
            return;
        
        // move on to the training phase if the current phase is pause
        if (currentPhase == TaskPhase.Pause)
        {
            // do nothing if there is no more interaction technique
            if (chosenTechniques.Count == 0)
                return;
            
            currentPhase = TaskPhase.Training;
            currentInstruction = -1;
        }
        
        // move on to the task phase if the current phase is training
        else if (currentPhase == TaskPhase.Training)
        {
            if (!InitializeTechnique())
                return;
            currentPhase = TaskPhase.Tasks;
        }
        
        // abort current technique and move on to pause
        else if (currentPhase == TaskPhase.Tasks)
        {
            FinishTechnique();
            return;
        }

        isMovingOn = true;
        SetStartPosition();
        LoadNextTasks();
        FadeOutAndRemoveTaskObjects(FadeInNextTaskObjects, currentPhase == TaskPhase.Tasks || currentPhase == TaskPhase.Training);
    }

    private void AddTeleport(Sequence mySequence)
    {
        mySequence.AppendCallback(() => SteamVR_Fade.Start( Color.clear, 0 ));
        mySequence.AppendCallback(() => SteamVR_Fade.Start( Color.black, 0.4f ));
        mySequence.AppendInterval(0.4f);
        mySequence.AppendCallback(() => movePlayer());
        mySequence.AppendCallback(() => startAreaValidation.SetMaterialsAlpha(0));
        mySequence.AppendCallback(() => SteamVR_Fade.Start( Color.clear, 0.4f ));
    }

    private void movePlayer(){
		Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
        player.trackingOriginTransform.position = Vector3.zero + playerFeetOffset;
    }

    /// <summary>
    /// Loads the next tasks from a json file. 
    /// </summary>
    private void LoadNextTasks()
    {
        // activate next interaction technique, if we are currently in the training phase
        if (currentPhase == TaskPhase.Training)
        {
            noneTechnique.SetActive(false);
            if (currentTechnique != null)
                currentTechnique.SetActive(false);
            currentTechnique = interactionTechniques.transform.Find(chosenTechniques[0].name).gameObject;
            currentTechnique.SetActive(true);
        }

        // load the training tasks if we are in the training phase - otherwise load the next tasks
        List<Task> allTasks = JsonHelper.DeserializeFromFile<List<Task>>(Path.Combine(Application.streamingAssetsPath,
            currentPhase == TaskPhase.Training ? "trainingTasks.json" : GetTaskFileName()));

        // reduce the tasks to the tasks which are supported by the interaction technique and do correspond with study type
        tasks = new Dictionary<TaskTypeDistance, List<Task>>();
        InteractionTechniqueConf it = chosenTechniques[0];
        foreach (Task task in allTasks)
        {
            TaskTypeDistance ttk = new TaskTypeDistance(task.taskTypes, task.distance);
            if (TechniqueSupportsTask(currentPhase, it, task) && TaskInStudy(task))
            {
                if (!tasks.ContainsKey(ttk))
                    tasks.Add(ttk, new List<Task>());
                tasks[ttk].Add(task);
            }
        }

        // randomize the order of the task blocks (but if we are in training phase put manipulation tasks first)
        taskTypeOrder = new List<TaskTypeDistance>(tasks.Keys);
        taskTypeOrder = currentPhase == TaskPhase.Training
            ? taskTypeOrder.OrderByDescending(a => a.sortingValue).ToList()
            : taskTypeOrder.OrderBy(a => a.sortingValue).ToList();

        // randomize the order in the task blocks
        foreach (TaskTypeDistance taskType in taskTypeOrder)
            tasks[taskType] = tasks[taskType].OrderBy(a => Random.value).ToList();
        
        // set task count for status text
        taskCount = GetCurrentTaskCount();
        
        // load dummy tasks and skip tasks if necessary 
        if (currentPhase != TaskPhase.Training)
        {
            LoadDummyTasks(taskCount);
            SkipTasks();
        }
    }

    private void LoadDummyTasks(int currentTaskCount)
    {
        // load the dummy tasks
        List<Task> allTasks = JsonHelper.DeserializeFromFile<List<Task>>(Path.Combine(Application.streamingAssetsPath, GetDummyTaskFileName()));
        
        // reduce the tasks to the tasks which are supported by the interaction technique and do correspond with study type
        List<Task> possibleDummyTasks = new List<Task>();
        InteractionTechniqueConf it = chosenTechniques[0];
        foreach (Task task in allTasks)
            if (TechniqueSupportsTask(currentPhase, it, task) && TaskInStudy(task))
                possibleDummyTasks.Add(task);
        
        // determine needed number of dummy tasks
        int dummyTasksCount = (isSelectionStudy() ? targetTaskCountSelection : targetTaskCountManipulation) - currentTaskCount;

        // add needed number of dummy tasks to list
        dummyTasks = new List<Task>();
        for (int i = 0; i < dummyTasksCount; i++)
            dummyTasks.Add(possibleDummyTasks[i % possibleDummyTasks.Count]);
        dummyTasks = dummyTasks.OrderBy(a => Random.value).ToList();
        
        // reset dummy count
        taskCount = GetCurrentTaskCount();
    }

    /// <summary>
    /// Checks whether the interaction technique supports the required tasks types.
    /// </summary>
    private bool TechniqueSupportsTask(TaskPhase taskPhase, InteractionTechniqueConf technique, Task task)
    {
        // technique does not support required distance
        if (task.distance > technique.maxSupportedDistance)
            return false;

        // task does not support required task
        if (taskPhase != TaskPhase.Training && !technique.supportedTaskTypes.HasFlag(task.taskTypes))
            return false;

        if (taskPhase == TaskPhase.Training)
        {
            // task requires selection but technique does not support it
            if (task.taskTypes.HasFlag(TaskType.Selection) && !technique.supportedTaskTypes.HasFlag(TaskType.Selection))
                return false;

            // task requires manipulation task but technique does not support it
            // it is sufficient if technique supports any manipulation if a manipulation task type is required in a training task
            // the task will be adapted in the training phase so we don't need a training task for each task type and distance
            if (task.taskTypes > TaskType.Selection && technique.supportedTaskTypes <= TaskType.Selection)
                return false;
        }

        return true;
    }

    private bool TaskInStudy(Task task)
    {
        if (isSelectionStudy())
            return task.taskTypes.HasFlag(TaskType.Selection);
        return !task.taskTypes.HasFlag(TaskType.Selection);
    }

    /// <summary>
    /// Is called if an object is selected.
    /// </summary>
    public void ObjectSelected(TaskObject taskObject, GameObject gObj, Hand hand)
    {
        if (currentTask.taskTypes.HasFlag(TaskType.Selection))
        {
            // no object selected
            if (gObj == null && TaskStarted())
            {
                if (grabEnabled)
                {
                    MeasurementController.Instance.AddMiss();
                    incorrectAudioSource.Play();
                }
            }
            // pre task object selected
            else if (taskObject == null && gObj != null)
            {
                // display task objects
                foreach (var go in currentGameObjects)
                    go.SetActive(!go.activeSelf);

                // destroy pre task object
                if (hand.currentAttachedObject != null)
                    hand.DetachObject(gObj);
                DestroyImmediate(currentGameObjects[0]);
                currentGameObjects.RemoveAt(0);
                SyncObjectsInTechnique();

                // start measurements
                if (currentPhase == TaskPhase.Tasks)
                    MeasurementController.Instance.StartMeasurement();

                // inform techniques
                currentTechnique.BroadcastMessage("PreTaskObjectSelected", currentGameObjects[0],
                    SendMessageOptions.DontRequireReceiver);
            }
            else if (gObj != null)
            {
                if (taskObject.isTargetObject)
                {
                    currentTechnique.BroadcastMessage("TaskSolved", gObj.transform.position);
                    if (currentPhase == TaskPhase.Tasks)
                        MeasurementController.Instance.StopMeasurement(config, currentTechnique.name, (SelectionTask) currentTask);
                    FadeOutAndRemoveTaskObjects(FadeInNextTaskObjects);
                }
                else
                {
                    MeasurementController.Instance.AddMiss();
                    incorrectAudioSource.Play();
                }

                // detach object from the hand if we are in a selection tasks
                GetInteractionTechnique().ReleaseObject();
            }
        }
        else
        {
            // start measurements on object selection if we are in a manipulation task 
            if (currentPhase == TaskPhase.Tasks)
                MeasurementController.Instance.StartMeasurement();
        }
    }

    /// <summary>
    /// Is called if a object is released.
    /// </summary>
    public void ObjectReleased(TaskObject taskObject, GameObject gObj)
    {        
        // move object back to environment if necessary
        if (!currentTask.taskTypes.HasFlag(TaskType.Selection) && !gObj.GetComponent<Takeable>().preTaskObject)
        {
            GameObject currentObj = GetRealObject(gObj);
            MoveObjectBackToEnvironment(currentObj);
        }
    }

    /// <summary>
    /// Moves the object back to the environment if it was placed outside.
    /// </summary>
    private void MoveObjectBackToEnvironment(GameObject obj)
    {
        Vector3 dir = obj.transform.position - player.hmdTransform.position;
        if (!Physics.Raycast(player.hmdTransform.position, dir, out RaycastHit hitInfo, dir.magnitude,
            LayerMask.GetMask("Room")))
            return;

        Vector3 hitDir = hitInfo.point - player.hmdTransform.position;
        obj.transform.position = player.hmdTransform.position + (hitDir - (hitDir.normalized * obj.transform.lossyScale.x));
        SyncObjectsInTechnique();
    }

    /// <summary>
    /// Determines through the configuration whether the user is right handed.
    /// </summary>
    public bool IsRightHanded()
    {
        return config.primaryHand.Equals("right");
    }
    
    /// <summary>
    /// Determines through the configuration whether the user is right handed and returns the corresponding hand.
    /// </summary>
    public Hand GetPrimaryHand()
    {
        return config.primaryHand.Equals("right") ? player.rightHand : player.leftHand;
    }

    /// <summary>
    /// Activates or deactivates all current game objects.
    /// </summary>
    public void SetCurrentGameObjectsActive(bool active)
    {
        if (currentGameObjects.Count > 0)
        {
            // only change the active state of the pre task object if the task did not started yet
            if (currentTask.taskTypes.HasFlag(TaskType.Selection) && !TaskStarted())
                currentGameObjects[0].SetActive(active);
            // else change the active state of all task objects
            else
                foreach (GameObject go in currentGameObjects)
                    go.SetActive(active);

            SyncObjectsInTechnique();
        }
    }

    /// <summary>
    /// Plays the corresponding animation and sound.
    /// </summary>
    public void TaskSolved(Vector3 targetPosition, bool playAnimation = true)
    {
        correctAudioSource.Play();
        if (playAnimation)
        {
            correctAnimation.Clear();
            correctAnimation.transform.position = targetPosition;
            correctAnimation.Play();
        }
    }

    /// <summary>
    /// Updates the status text.
    /// </summary>
    private void UpdateStatusText()
    {
        if (currentPhase == TaskPhase.Tasks)
            StatusTextController.Instance.UpdateStatusTextTasksPhase(currentTechnique.name,
                (techniqueCount - chosenTechniques.Count) + 1,
                techniqueCount, currentPhase, currentTask.id, currentTask.taskTypes,
                taskCount - GetCurrentTaskCount(), taskCount);
        else
        {
            StatusTextController.Instance.UpdateStatusTextTrainingsPhase(currentTechnique.name,
                (techniqueCount - chosenTechniques.Count) + 1,
                techniqueCount, currentPhase, currentTask.id, currentTask.taskTypes, currentInstruction + 1,
                GetInteractionTechnique().GetInstructions().Length);
        }
    }

    /// <summary>
    /// Determines the overall number of tasks for the current interaction techniques.
    /// </summary>
    private int GetCurrentTaskCount()
    {
        int result = 0;
        tasks.ForEach(x => result += x.Value.Count);
        return result + (dummyTasks?.Count ?? 0);
    }

    /// <summary>
    /// Sets the start position to the current user position
    /// and moves the return room to this position.
    /// </summary>
    private void SetStartPosition()
    {
        userStartPosition = new Vector3(0, player.eyeHeight, 0);
        returnRoom.position = new Vector3(userStartPosition.x, 0, userStartPosition.z);
    }

    /// <summary>
    /// Fades out and removes all current task objects.
    /// </summary>
    /// <param name="callback">Is called afterwards</param>
    private void FadeOutAndRemoveTaskObjects(TweenCallback callback = null, bool addTeleport = false)
    {
        Sequence mySequence = DOTween.Sequence();
        mySequence.AppendCallback(() => grabEnabled = false);
        mySequence.AppendCallback(RemoveAllInteractableComponents);
        mySequence.AppendCallback(() =>
            currentTechnique.BroadcastMessage("FadeOutAndRemoveTaskObjectsStart", SendMessageOptions.DontRequireReceiver));
        mySequence.AppendCallback(() => SetHandsActive(false));
        mySequence.AddMaterialsFade(prefabMaterials, 0, 0.25f);
        mySequence.AddMaterialsFade(transparentPrefabMaterials, 0f, 0.25f, true);
        mySequence.AppendCallback(RemoveAllObjects);
        if (addTeleport)
            AddTeleport(mySequence);
        if (callback != null)
            mySequence.AppendCallback(callback);
        mySequence.AppendCallback(() => SetHandsActive(true));
        mySequence.AppendCallback(() =>
            currentTechnique.BroadcastMessage("FadeOutAndRemoveTaskObjectsEnd", SendMessageOptions.DontRequireReceiver));
        mySequence.AppendCallback(() => isMovingOn = false);
        mySequence.Play();
    }

    /// <summary>
    /// Removes all current task objects and informs the current interaction technique if necessary.
    /// </summary>
    private void RemoveAllObjects()
    {
        currentGameObjects.ForEach(Destroy);
        currentGameObjects.Clear();
        InformTechnique();
    }

    /// <summary>
    /// Removes all interactable components so the objects cannot be highlighted anymore.
    /// </summary>
    private void RemoveAllInteractableComponents()
    {
        currentGameObjects.ForEach(go => Destroy(go.GetComponent<Interactable>()));
    }

    /// <summary>
    /// Fades in the next task objects.
    /// </summary>
    private void FadeInNextTaskObjects()
    {
        Sequence mySequence = DOTween.Sequence();
        mySequence.AppendCallback(CreateNextTaskObjects);
        mySequence.AppendCallback(InformNextTaskObjectsCreated);
        mySequence.AddMaterialsFade(prefabMaterials, 1, 0.25f);
        mySequence.AddMaterialsFade(transparentPrefabMaterials, 0.5f, 0.25f, true);
        mySequence.AppendCallback(() => grabEnabled = true);
        mySequence.AppendCallback(() => taskIsFinishing = false);
        mySequence.Play();
    }

    /// <summary>
    /// Informs the current technique that the next task objects have been created.
    /// </summary>
    private void InformNextTaskObjectsCreated()
    {
        if (currentGameObjects.Count > 0)
            currentTechnique.SendMessage("NextTaskObjectsCreated", currentGameObjects[0],
                SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Instantiates the next task objects.
    /// </summary>
    private void CreateNextTaskObjects()
    {
        // remove current task type from the list if all corresponding tasks are solved
        if (taskTypeOrder.Count > 0 && tasks[taskTypeOrder[0]].Count == 0)
            taskTypeOrder.RemoveAt(0);

        // fade out and remove all current objects if the last task for the current interaction technique is solved
        // also remove the current interaction technique from the list to move on to the next technique
        if (taskTypeOrder.Count == 0)
        {
            if (dummyTasks.Count == 0)
            {
                FinishTechnique();
                RemoveSkipTaskFile();
                return;
            }
            currentTask = dummyTasks[0];
            dummyTasks.RemoveAt(0);
        }
        else
        {
            currentTask = tasks[taskTypeOrder[0]][0];
        }

        if (currentPhase != TaskPhase.Training)
            UpdateSkipTaskFile();

        if (currentTask is SelectionTask selectionTask)
        {
            // instantiate pre task object
            GameObject preTaskObject = Instantiate(preTaskObjectPrefab, taskObjectsParent);
            preTaskObject.transform.position = preTaskObject.transform.position + userStartPosition;
            currentGameObjects.Add(preTaskObject);
            
            // instantiate task objects
            foreach (var obj in selectionTask.taskObjects)
                InstantiateGameObject(obj);
        }
        else
        {
            // instantiate start and end object
            ManipulationTask manipulationTask = (ManipulationTask) currentTask;
            GameObject startObject = InstantiateGameObject(manipulationTask.startObject, activeOnStart: true);
            InstantiateGameObject(manipulationTask.endObject, startObject.transform, true);
        }

        // moves current item to the end of the list in task and task type order lists so the training is infinite
        if (currentPhase == TaskPhase.Training)
        {
            tasks[taskTypeOrder[0]].MoveFirstToEnd();
            taskTypeOrder.MoveFirstToEnd();
        }
        // otherwise remove the current task from the list
        else if (taskTypeOrder.Count > 0)
            tasks[taskTypeOrder[0]].RemoveAt(0);

        // inform technique about the new task objects if needed
        InformTechnique();
        
        // update status text
        UpdateStatusText();
    }

    /// <summary>
    /// Instantiates a task object. Deactivates it immediately because only the pre
    /// task object is visible in the beginning. The task objects are displayed after
    /// the pre task object is selected.
    /// </summary>
    private GameObject InstantiateGameObject(TaskObject obj, Transform startObject = null, bool activeOnStart = false)
    {
        GameObject prefab = prefabs.Find(p => p.name.Equals(obj.prefabName));
        GameObject created = Instantiate(prefab, obj.position + userStartPosition, obj.rotation, taskObjectsParent);
        created.transform.localScale = obj.scale;
        
        // Reset transform properties of the target object if not supported by the current technique
        if (startObject)
        {
            if (!chosenTechniques[0].supportedTaskTypes.HasFlag(TaskType.Positioning))
                created.transform.position = startObject.position;
            if (!chosenTechniques[0].supportedTaskTypes.HasFlag(TaskType.Rotating))
                created.transform.rotation = startObject.rotation;
            if (!chosenTechniques[0].supportedTaskTypes.HasFlag(TaskType.Scaling))
                created.transform.localScale = startObject.localScale;
        }
        
        if (created.GetComponent<Takeable>() != null)
            created.GetComponent<Takeable>().taskObject = obj;
        currentGameObjects.Add(created);
        created.SetActive(activeOnStart);
        return created;
    }

    /// <summary>
    /// Finishes the current technique and moves on to the pause phase.
    /// </summary>
    private void FinishTechnique()
    {
        RemoveAllObjects();
        currentPhase = TaskPhase.Pause;
        chosenTechniques.RemoveAt(0);
        currentTechnique.SetActive(false);
        StatusTextController.Instance.UpdateStatusText(currentPhase.ToString());
        noneTechnique.SetActive(true);
        MeasurementController.Instance.ResetMeasurementController();
    }

    private void SyncObjectsInTechnique()
    {
        currentTechnique.BroadcastMessage("SyncObjects", SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Informs interaction techniques about changes if necessary. 
    /// </summary>
    private void InformTechnique()
    {
        if (currentTechnique.name.Contains("World In Miniature"))
            currentTechnique.GetComponentInChildren<WorldInMiniatureTechnique>().UpdateObjects();
    }

    /// <summary>
    /// Returns the real object if necessary.
    /// </summary>
    private GameObject GetRealObject(GameObject proxyObject)
    {
        if (currentTechnique.name.Contains("World In Miniature"))
            return currentTechnique.GetComponentInChildren<WorldInMiniatureTechnique>().GetMappedObject(proxyObject);
        return proxyObject;
    }

    /// <summary>
    /// Adds a initialization phase if the technique needs it. Returns true if no initialization is needed
    /// or the technique is already initialized.
    /// </summary>
    private bool InitializeTechnique()
    {
        if (currentTechnique.name == "Go-Go + PRISM")
            return currentTechnique.GetComponentInChildren<GoGoTechnique>().InitTechnique();
        return true;
    }

    /// <summary>
    /// Enables or disables the hand components. Afterwards interacting
    /// with objects is not possible anymore.
    /// </summary>
    private void SetHandsActive(bool active)
    {
        if (player.leftHand)
            player.leftHand.enabled = active;
        if (player.rightHand)
            player.rightHand.enabled = active;
    }

    /// <summary>
    /// Determines whether a selection task is already started.
    /// </summary>
    private bool TaskStarted()
    {
        List<GameObject> gameObjects = currentGameObjects;
        if (currentTechnique.name.Contains("World In Miniature"))
            gameObjects =  currentTechnique.GetComponentInChildren<WorldInMiniatureTechnique>().GetProxyObjects();
        
        if (gameObjects.Count == 0)
            return false;

        List<GameObject> preTaskObject = gameObjects.Where(o => o.GetComponent<Takeable>().preTaskObject).ToList();
        if (preTaskObject.Count == 0)
            return true;

        return false;
    }

    public bool IsSelectionTask()
    {
        return currentTask.taskTypes.HasFlag(TaskType.Selection);
    }

    public InteractionTechnique GetInteractionTechnique(){
        InteractionTechnique interactionTechnique = null;
        if (currentTechnique != null) {
            interactionTechnique = currentTechnique.GetComponent<InteractionTechnique>();
            if (!interactionTechnique)
                interactionTechnique = currentTechnique.GetComponentInChildren<InteractionTechnique>();
        }
        return interactionTechnique;
    }

    /// <summary>
    /// Updates the skipTasks.json file which contains the tasks which are not finished yet.
    /// If the application runs into an error it can use this file to skip all tasks if restarted.
    /// </summary>
    private void UpdateSkipTaskFile()
    {
        if (debugMode)
            return;
        
        // add all ids of the remaining tasks
        List<string> idList = new List<string>();
        tasks.ForEach(t => t.Value.ForEach(l => idList.Add(l.id)));
        
        // add all ids of the remaining dummy tasks
        List<string> dummyIdList = new List<string>();
        dummyTasks.ForEach(d => dummyIdList.Add(d.id));
        
        JsonHelper.SerializeToFile(Path.Combine(Application.streamingAssetsPath, "skipTasks.json"),
            new TaskIdList(idList, dummyIdList));
    }

    /// <summary>
    /// Skips all tasks whose id is not present in the skipTasks.json.
    /// </summary>
    private void SkipTasks()
    {
        if (debugMode)
            return;
        
        string path = Path.Combine(Application.streamingAssetsPath, "skipTasks.json");
        if (!File.Exists(path))
            return;

        Debug.LogError("WARNING: Tasks will be skipped!");
        
        TaskIdList taskIdList = JsonHelper.DeserializeFromFile<TaskIdList>(Path.Combine(Application.streamingAssetsPath, "skipTasks.json"));
        List<string> taskIds = taskIdList.taskIds;
        List<string> dummyTaskIds = taskIdList.dummyTaskIds;

        // remove task if id is not present in the file
        foreach (KeyValuePair<TaskTypeDistance,List<Task>> pair in tasks) 
            pair.Value.RemoveAll(task => !taskIds.Contains(task.id));
        
        // remove TaskTypeDistance keys and the corresponding element in the taskTypeOrder list
        List<TaskTypeDistance> keysToRemove = new List<TaskTypeDistance>();
        foreach (KeyValuePair<TaskTypeDistance,List<Task>> pair in tasks)
            if (pair.Value.Count == 0)
                keysToRemove.Add(pair.Key);
        foreach (TaskTypeDistance key in keysToRemove)
        {
            tasks.Remove(key);
            taskTypeOrder.Remove(key);
        }
        
        // remove dummy task if id is not present in the file
        dummyTasks.RemoveRange(0, dummyTasks.Count - dummyTaskIds.Count);
    }

    /// <summary>
    /// Removes the skipTasks.json. Should be done if a technique is finished successfully.
    /// </summary>
    private void RemoveSkipTaskFile()
    {
        if (debugMode)
            return;
        
        File.Delete(Path.Combine(Application.streamingAssetsPath, "skipTasks.json"));
    }

    private string GetTaskFileName()
    {
        return isSelectionStudy() ? "tasks_selection.json" : "tasks_manipulation.json";
    }

    private string GetDummyTaskFileName()
    {
        return isSelectionStudy() ? "dummyTasks_selection.json" : "dummyTasks_manipulation.json";
    }

    public enum TaskPhase
    {
        Pause,
        Training,
        Tasks
    }

    class TaskTypeDistance : IEquatable<TaskTypeDistance>
    {
        public TaskType taskType { get; }
        public int distance { get; }
        
        public int sortingValue { get; }
        
        public TaskTypeDistance(TaskType taskType, int distance)
        {
            this.distance = distance;
            this.taskType = taskType;

            if (distance == 1)
                sortingValue = !taskType.HasFlag(TaskType.Scaling) ? 0 : 1;
            else
                sortingValue = !taskType.HasFlag(TaskType.Scaling) ? 2 : 3;
        }

        public bool Equals(TaskTypeDistance taskTypeDistance)
        {
            return taskTypeDistance.sortingValue.Equals(sortingValue);
        }

        public override int GetHashCode()
        {
            return sortingValue.GetHashCode();
        }
    }
}
