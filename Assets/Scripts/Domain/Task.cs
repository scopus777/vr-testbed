using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Task
{
    public string id;
    public TaskType taskTypes;
    public float timeLimit;
    public int distance;
    public float actualDistance;
    public float radius;
    
    protected Task(){}
}

[Serializable]
public class SelectionTask : Task
{
    public List<TaskObject> taskObjects;
    public int numberOfObjects;
    public float minDensity;
}

[Serializable]
public class ManipulationTask : Task
{
    public TaskObject startObject;
    public TaskObject endObject;
    public int neededDoFs;
    public int manipulationAmount;
    public float PositioningTolerance;
    public float RotatingTolerance;
    public float ScalingTolerance;
}

[Flags]
public enum TaskType
{
    None = 0,
    Selection = 1,
    Positioning = 2,
    Rotating = 4,
    Scaling = 8
}

[Serializable]
public class TaskObject
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public bool isTargetObject;

    public TaskObject()
    {
    }

    public TaskObject(GameObject prefab, bool isTargetObject = false)
    {
        prefabName = prefab.name;
        position = prefab.transform.position;
        rotation = prefab.transform.rotation;
        scale = prefab.transform.localScale;
        this.isTargetObject = isTargetObject;
    }
}

[Serializable]
public class TaskList
{
    public List<Task> tasks;

    public TaskList(){}

    public TaskList(List<Task> tasks)
    {
        this.tasks = tasks;
    }
}

public class TaskIdList
{
    public List<string> taskIds;
    public List<string> dummyTaskIds;

    public TaskIdList(List<string> taskIds, List<string> dummyTaskIds)
    {
        this.taskIds = taskIds;
        this.dummyTaskIds = dummyTaskIds;
    }
}
