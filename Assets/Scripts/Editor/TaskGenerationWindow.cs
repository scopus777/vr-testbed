using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Allows the generation of new tasks.
/// </summary>
public class TaskGenerationWindow : EditorWindow
{
    /// base settings
    // the Name of the task (only visible for the evaluator)
    private string taskName = "";
    // the prefab which is used for an task object (wrong objects in the selection task and
    // the object which has to be manipulated in the manipulation task
    private GameObject prefab;
    // the target object (the object that needs to be selected in a selection task or the
    // object which depicts the target position, rotation and scale in the manipulation task)
    private GameObject targetPrefab;
    // the distance given by the posDistance array (0.6f, 3 or 6 m)
    private int distance;
    // the radius of the circle generated at the given distance from the user
    // effectively determines the size of the area in which the object can be placed
    private float radius = 3f;
    // the time limit of the task
    private float timeLimit = 30;
    // the path to the file in which the tasks are saved
    private string targetFile = Path.Combine(Application.streamingAssetsPath, "tasks.json");
    
    /// selection task settings
    // number of objects
    private int objectCount = 1;
    // the minimum space between the objects
    private float density = 1;
    
    /// manipulation task settings
    // determines whether the position of the object needs to be changed
    private bool positioning = true;
    // determines whether the rotation of the object needs to be changed
    private bool rotating = false;
    // determines whether the scale of the object needs to be changed
    private bool scaling = false;
    // determines on which axes the object needs to be rotated and/or positioned
    // (scaling is always done on all axes)
    private int neededDoFs = 1;
    // the amount the object needs to be manipulated
    // the possible values 1,2 and 3 are mapped on ranged in the code
    private int manipulationAmount = 1;
    // how much meter the position task object can deviate from the position of the task object to be accepted
    private float positioningTolerance = 0.05f;
    // how much degree the rotation task object can deviate from the rotation of the task object to be accepted
    private float rotatingTolerance = 15f;
    // how much the size task object can deviate from the size of the task object to be accepted (in meters)
    private float scalingTolerance = 0.05f;
    
    // the user position which is used as a point of origin for the generation of the task
    // (actually the position of the objects in the y axis is dynamically adapted at the start of the task)
    private Vector3 userPosition = new Vector3(0f, 1.6f, 0f);
    // one of this distances is selected by the value of the distances variable
    private float[] posDistances = {0.6f, 3, 6};
    // how often the algorithm should try to generate an object with the given settings until it aborts
    private int attempts = 10;
    // determines how long the algorithm tries to find a task which suits the given settings
    private float maxGenerationTime = 10f;
    // determines how many objects needs to be placed around the target object
    // (distance is determined by the density variable)
    private int nearTargetObjectsCount = 4;

    private float actualDensity;
    private float prefabScale;
    private GameObject tmpSphere;
    private List<TaskObject> taskObjects = new List<TaskObject>();
    private List<GameObject> generatedObjects = new List<GameObject>();
    private TaskType currentTaskType = TaskType.None;
    private List<Task> currentTasks;
    private float generationStartTime;

    [MenuItem("Window/Task Generation")]
    public static void ShowWindow()
    {
        GetWindow(typeof(TaskGenerationWindow));
    }

    void OnGUI()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        taskName = EditorGUILayout.TextField("Task Name", taskName);
        prefab = (GameObject) EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject),true);
        targetPrefab = (GameObject) EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject),true);
        distance = EditorGUILayout.IntSlider("Distance", distance,1,3);
        radius = EditorGUILayout.FloatField("Radius", radius);
        timeLimit = EditorGUILayout.FloatField("TimeLimit", timeLimit);
        targetFile = EditorGUILayout.TextField("File Path", targetFile);
        
        EditorGUILayout.Separator();
        GUILayout.Label("Selection Task Generation", EditorStyles.boldLabel);
        objectCount = EditorGUILayout.IntField("Number of Objects", objectCount);
        density = EditorGUILayout.FloatField("Min Density", density);
        if (GUILayout.Button("Generate"))
            GenerateSelectionTask();
        
        EditorGUILayout.Separator();
        GUILayout.Label("Manipulation Task Generation", EditorStyles.boldLabel);
        positioning = EditorGUILayout.Toggle("Positioning", positioning);
        rotating = EditorGUILayout.Toggle("Rotating", rotating);
        scaling = EditorGUILayout.Toggle("Scaling", scaling);
        neededDoFs = EditorGUILayout.IntSlider("Needed DoFs", neededDoFs, 1, 3);
        manipulationAmount = EditorGUILayout.IntSlider("Manipulation Amount", manipulationAmount, 1, 3);
        positioningTolerance = EditorGUILayout.FloatField("Positioning Tolerance", positioningTolerance);
        rotatingTolerance = EditorGUILayout.FloatField("Rotating Tolerance", rotatingTolerance);
        scalingTolerance = EditorGUILayout.FloatField("Scaling Tolerance", scalingTolerance);
        if (GUILayout.Button("Generate"))
            GenerateManipulationTask();
        
        EditorGUILayout.Separator();
        if (GUILayout.Button("Add to File"))
            AddCurrentTaskToFile();
        if (GUILayout.Button("Remove Generated Objects"))
            RemoveGeneratedObjects(); 
    }

    /// <summary>
    /// Tries to generate objects for a selection task with the given settings. 
    /// </summary>
    void GenerateSelectionTask()
    {
        // correct density to respect the scale of the objects
        actualDensity = density + prefab.transform.lossyScale.x;
        RemoveGeneratedObjects();
        currentTaskType = TaskType.Selection;
        GenerateObjects(objectCount);
    }

    /// <summary>
    /// Generates the given number of objects and tries to ensure, that all objects satisfy the settings.
    /// It uses backtracking and randomly generates position. This is highly inefficient.
    /// </summary>
    /// <param name="count"></param>
    private void GenerateObjects(int count)
    {
        // generates a sphere with the given distance as the radius (distance * 2 because scale is twice the distance)
        tmpSphere = new GameObject("tmpSphere");
        tmpSphere.transform.position = Vector3.zero;
        float dist = posDistances[distance - 1] * 2;
        tmpSphere.transform.localScale = new Vector3(dist, dist, dist);
        tmpSphere.AddComponent<SphereCollider>();

        // set start time to abort generation if necessary 
        generationStartTime = Time.realtimeSinceStartup;

        // generate recursively objects on the outer surface of the sphere
        // the generation fails if the new object cannot satisfy the settings (e.g. not enough space between objects)
        if (GenerateObjectsRec(attempts, nearTargetObjectsCount, count))
        {
            if (currentTaskType.HasFlag(TaskType.Selection))
            {
                // randomly chose target object
                int targetIndex = 0;
                for (int i = 0; i < taskObjects.Count; i++)
                {
                    if (i == targetIndex)
                    {
                        generatedObjects.Add(Instantiate(targetPrefab, taskObjects[i].position + userPosition, Quaternion.identity));
                        taskObjects[i].prefabName = targetPrefab.name;
                        taskObjects[i].isTargetObject = true;
                    }
                    else
                    {
                        generatedObjects.Add(Instantiate(prefab, taskObjects[i].position + userPosition, Quaternion.identity));
                    }
                }
            }
            else
            {
                // for manipulation tasks only the start object is generated
                generatedObjects.Add(Instantiate(prefab, taskObjects[0].position + userPosition, Quaternion.identity));
            }
        }
        else
            Debug.LogWarning("Could not generate a suitable environment. Maybe you should change the properties.");

        DestroyImmediate(tmpSphere);
    }

    /// <summary>
    /// Generates recursively objects on the outer surface of the sphere.
    /// The generation fails if the new object cannot satisfy the
    /// settings (e.g. not enough space between objects).
    /// The method at first tries to generate a given number of objects near
    /// the target object (with the given distance to the target object).
    /// If no suitable placement of the object can be generated
    /// with the given numbers of tries the method return false.
    /// </summary>
    bool GenerateObjectsRec(int currentAttempts, int currentNearTargetCount, int count)
    {
        // Finish recursion if enough objects are generated.
        if (taskObjects.Count == count)
            return true;

        bool result = false;
        float currentNeededTime = Time.realtimeSinceStartup - generationStartTime;
        while (!result && currentAttempts > 0 && currentNeededTime < maxGenerationTime)
        {
            bool wasNearTarget = false;
            Vector3 pos;
            if (currentNearTargetCount > 0 && taskObjects.Count > 0 && count > currentNearTargetCount)
            {
                // get the direction vector to the position of the target object
                Vector3 dir = taskObjects[0].position;
                // calculate the angle between the vector to the target object and a vector to a
                // point laying on the sphere and with the given distance to the target object
                float alpha = 2 * Mathf.Asin(actualDensity / (2 * posDistances[distance - 1])) * (180 / Mathf.PI);
                // get a random vector perpendicular to the target object vector
                Vector3 rotVector = Vector3.Cross(dir, Random.insideUnitCircle);
                // rotate the target object vector around the random vector by the calculated angle
                pos = Quaternion.AngleAxis(alpha, rotVector) * dir;
                wasNearTarget = true;
                currentNearTargetCount--;
            }
            else
            {
                // generate a circle with the given radius around the users starting position
                Vector2 pointInCircle = GetRandomPointInCircle(new Vector2(0, 0), radius);
                // transfer the point to the given distance (the center of the circle now lies on the outer surface of the sphere)
                Vector3 targetPoint = new Vector3(pointInCircle.x, pointInCircle.y, posDistances[distance - 1]);
                // we need to reverse ray because we get no collider hit if the ray originates from inside the sphere
                Ray reverseRay = new Ray(Vector3.zero, targetPoint);
                reverseRay.origin = reverseRay.GetPoint(100);
                reverseRay.direction = -reverseRay.direction;
                // get the intersection point of the ray (from the circle point to the user position) and the sphere
                tmpSphere.GetComponent<SphereCollider>().Raycast(reverseRay, out var hit, Mathf.Infinity);
                pos = hit.point;
            }

            // check whether the new position satisfies all settings
            if (ValidatePosition(pos))
            {
                // add the new task object
                TaskObject taskObject = new TaskObject(prefab);
                taskObject.position = pos;
                taskObjects.Add(taskObject);
                // return true if all following objects can be generated
                if (GenerateObjectsRec(attempts, currentNearTargetCount, count))
                    result = true;
                // remove the new task object if no further object can be generated
                else
                    taskObjects.Remove(taskObject);
            }
            if (wasNearTarget)
                currentNearTargetCount++;
            currentAttempts--;
        }

        return result;
    }
    
    /// <summary>
    /// Generates a manipulation tasks. 
    /// </summary>
    private void GenerateManipulationTask()
    {
        RemoveGeneratedObjects();
        currentTaskType = TaskType.None;
        GenerateObjects(1);
        GameObject obj = generatedObjects[0];
        obj.transform.rotation = Random.rotation;
        taskObjects[0].rotation = obj.transform.rotation;
        GameObject targetObj = Instantiate(targetPrefab, obj.transform.position, obj.transform.rotation);
        generatedObjects.Add(targetObj);

        if (positioning)
        {
            currentTaskType |= TaskType.Positioning;
            float xDir = Random.Range(0.25f, 0.5f);
            float yDir = neededDoFs > 1 ? Random.Range(0.25f, 0.5f) : 0;
            float zDir = neededDoFs > 2 ? Random.Range(0.05f, 0.1f) : 0;
            var objPosition = obj.transform.position;
            Vector3 dir = new Vector3(objPosition.x > userPosition.x ? -xDir : xDir,
                objPosition.y > userPosition.y ? -yDir : yDir,
                getSign() * zDir).normalized;
            dir = (manipulationAmount / 3f) * dir;
            targetObj.transform.position = objPosition + dir;
        }

        if (rotating)
        {
            currentTaskType |= TaskType.Rotating;
            Vector3 rotVector = new Vector3(Random.value, neededDoFs > 1 ? Random.value : 0, neededDoFs > 2 ? Random.value : 0);
            targetObj.transform.rotation = Quaternion.AngleAxis(manipulationAmount == 1 ? Random.Range(45, 90) : (manipulationAmount == 2 ? Random.Range(90, 135) : Random.Range(135, 180)), rotVector) * obj.transform.rotation;
        }

        if (scaling)
        {
            currentTaskType |= TaskType.Scaling;
            float scaleFactor = manipulationAmount == 1 ? Random.Range(1.25f, 1.5f) : (manipulationAmount == 2 ? Random.Range(1.5f, 1.75f) : Random.Range(1.75f, 2f));
            targetObj.transform.localScale = (Random.value <= 0.5 ? targetObj.transform.localScale * scaleFactor : targetObj.transform.localScale / scaleFactor);
        }

        TaskObject taskObject = new TaskObject(targetObj, true) {prefabName = targetPrefab.name};
        taskObject.position = taskObject.position - userPosition;
        taskObjects.Add(taskObject);

        currentTaskType &= ~TaskType.None;
    }

    private float getSign()
    {
        return Random.value < 0.5 ? -1 : 1;
    }

    /// <summary>
    /// Adds the current task to the json file.
    /// </summary>
    void AddCurrentTaskToFile()
    {
        currentTasks = File.Exists(targetFile) ? JsonHelper.DeserializeFromFile<List<Task>>(targetFile) : new List<Task>();

        if (currentTaskType == TaskType.None)
        {
            Debug.Log("Please generate task first!");
            return;
        }

        // determine the position where the new task should be added (so all tasks are grouped by there task type)
        int index;
        TaskType lastTaskType = TaskType.None;
        for (index = 0; index < currentTasks.Count; index++)
        {
            if (currentTaskType == lastTaskType && currentTasks[index].taskTypes != lastTaskType)
                break;
            lastTaskType = currentTasks[index].taskTypes;
        }

        if (currentTaskType.HasFlag(TaskType.Selection))
        {
            SelectionTask taskToAdd = new SelectionTask
            {
                id = GetId(),
                distance = distance,
                actualDistance = posDistances[distance - 1],
                radius = radius,
                taskTypes = currentTaskType,
                timeLimit = timeLimit,
                taskObjects = taskObjects,
                minDensity = density,
                numberOfObjects = objectCount
            };
            currentTasks.Insert(index, taskToAdd);
        }
        else
        {
            ManipulationTask taskToAdd = new ManipulationTask
            {
                id = GetId(),
                distance = distance,
                actualDistance = posDistances[distance - 1],
                radius = radius,
                taskTypes = currentTaskType,
                timeLimit = timeLimit,
                startObject = taskObjects[0],
                endObject = taskObjects[1],
                manipulationAmount = manipulationAmount,
                neededDoFs = neededDoFs,
                PositioningTolerance = positioningTolerance,
                RotatingTolerance = rotatingTolerance,
                ScalingTolerance = scalingTolerance
            };
            currentTasks.Insert(index, taskToAdd);
        }
        
        JsonHelper.SerializeToFile(targetFile, currentTasks);
        
        Debug.Log("Successfully added task to file.");
    }

   
    /// <summary>
    /// Returns a random point in the given circle. It generates points in a square bounding
    /// the circle until a point lies in the circle.
    /// </summary>
    private Vector2 GetRandomPointInCircle(Vector2 c, float r)
    {
        Vector2 result = GetRandomPointInSquare(c, r);
        while (!PointInCircle(result,c,r))
            result = GetRandomPointInSquare(c, r);
        return result;
    }

    /// <summary>
    /// Returns a random point in a square.
    /// </summary>
    private Vector2 GetRandomPointInSquare(Vector2 c, float r)
    {
        return new Vector2(Random.Range(c.x - r, c.x + r), Random.Range(c.y - r, c.y + r));
    }

    /// <summary>
    /// Checks whether a point lies in a circle.
    /// </summary>
    private bool PointInCircle(Vector2 p, Vector2 c, float r)
    {
        float d = Mathf.Sqrt(Mathf.Pow(p.x - c.x, 2) + Mathf.Pow(p.y - c.y, 2));
        return d < r;
    }

    /// <summary>
    /// Removes all generated objects and task objects.
    /// </summary>
    private void RemoveGeneratedObjects()
    {
        generatedObjects.ForEach(DestroyImmediate);
        generatedObjects.Clear();
        taskObjects.Clear();
    }

    /// <summary>
    /// Checks whether the new position satisfies the settings.
    /// </summary>
    private bool ValidatePosition(Vector3 pos)
    {
        foreach (var obj in taskObjects)
            if (Vector3.Distance(pos, obj.position) <= actualDensity)
                return false;

        return true;
    }

    /// <summary>
    /// Determines the next task id.
    /// </summary>
    private string GetId()
    {
        if (!string.IsNullOrEmpty(taskName))
            return taskName;
        
        int id = 0;
        foreach (Task task in currentTasks)
            if (int.TryParse(task.id, out int currentId))
                id = Math.Max(currentId, id);
        return (++id).ToString();
    }
}
