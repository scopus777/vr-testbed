using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Saves the measurements in a comma separated file.
/// </summary>
public class MeasurementController : Singleton<MeasurementController>
{
    private static string extension = ".csv";
    private static string fileNameManipulation = "measurements_manipulation";
    private static string filePathManipulation = Path.Combine(Application.streamingAssetsPath, fileNameManipulation + extension);
    private static string fileNameSelection = "measurements_selection";
    private static string filePathSelection = Path.Combine(Application.streamingAssetsPath, fileNameSelection + extension);

    private Stopwatch currentStopwatch;
    private int currentMisses;
    private float currentTranslationFootprintPrimaryHand;
    private float currentRotationFootprintPrimaryHand;
    private float currentTranslationFootprintSecondaryHand;
    private float currentRotationFootprintSecondaryHand;
    private float currentTranslationFootprintHead;
    private float currentRotationFootprintHead;

    void Start()
    {
        if (File.Exists(filePathManipulation))
            File.Copy(filePathManipulation, GetBackupPath(fileNameManipulation));
        else
            CreateMeasurementFileManipulation();
        
        if (File.Exists(filePathSelection))
            File.Copy(filePathSelection, GetBackupPath(fileNameSelection));
        else
            CreateMeasurementFileSelection();
    }
    public void StartMeasurement()
    {
        if (currentStopwatch == null || !currentStopwatch.IsRunning)
        {
            currentStopwatch = new Stopwatch();
            currentStopwatch.Start();
            currentMisses = 0;
            currentTranslationFootprintPrimaryHand = 0;
            currentRotationFootprintPrimaryHand = 0;
            currentTranslationFootprintSecondaryHand = 0;
            currentRotationFootprintSecondaryHand = 0;
            currentTranslationFootprintHead = 0;
            currentRotationFootprintHead = 0;
        }
    }

    public void ResetMeasurementController()
    {
        currentStopwatch?.Stop();
        currentStopwatch = new Stopwatch();
        currentMisses = 0;
        currentTranslationFootprintPrimaryHand = 0;
        currentRotationFootprintPrimaryHand = 0;
        currentTranslationFootprintSecondaryHand = 0;
        currentRotationFootprintSecondaryHand = 0;
    }

    public void AddMiss()
    {
        currentMisses++;
    }

    public void AddFootprintPrimaryHand(float translation, float rotation)
    {
        currentTranslationFootprintPrimaryHand += translation;
        currentRotationFootprintPrimaryHand += rotation;
    }
    
    public void AddFootprintSecondaryHand(float translation, float rotation)
    {
        currentTranslationFootprintSecondaryHand += translation;
        currentRotationFootprintSecondaryHand += rotation;
    }
    
    public void AddFootprintHead(float translation, float rotation)
    {
        currentTranslationFootprintHead += translation;
        currentRotationFootprintHead += rotation;
    }

    public void StopMeasurement(Configuration config, string it, ManipulationTask task, float posDiff = 0,
        float rotDiff = 0, float scaleDiff = 0, bool success = true)
    {
        currentStopwatch.Stop();
        float neededTime = success ? currentStopwatch.ElapsedMilliseconds / 1000f : task.timeLimit;
        Debug.Log(neededTime + " " + currentTranslationFootprintPrimaryHand + " " + currentRotationFootprintPrimaryHand);
        AppendToFile(filePathManipulation, config.userId, config.primaryHand, it, task.id,
            task.taskTypes.ToTaskTypeString(), task.endObject.prefabName, task.actualDistance, task.radius, task.neededDoFs, task.manipulationAmount,
            success, neededTime, posDiff.ToString("0.####"),
            rotDiff.ToString("0.####"), scaleDiff.ToString("0.####"),
            currentTranslationFootprintPrimaryHand.ToString("0.####"),
            currentTranslationFootprintSecondaryHand.ToString("0.####"));
    }

    public void StopMeasurement(Configuration config, string it, SelectionTask task, bool success = true)
    {
        currentStopwatch.Stop();
        float neededTime = success ? currentStopwatch.ElapsedMilliseconds / 1000f : task.timeLimit;
        Debug.Log(neededTime + " " + currentTranslationFootprintPrimaryHand + " " + currentRotationFootprintPrimaryHand);
        AppendToFile(filePathSelection, config.userId, config.primaryHand, it, task.id,
            task.taskTypes.ToTaskTypeString(), task.taskObjects[0].prefabName, task.actualDistance, task.radius,
            task.numberOfObjects, task.minDensity,
            success, neededTime, currentMisses,
            currentTranslationFootprintPrimaryHand.ToString("0.####"),
            currentTranslationFootprintSecondaryHand.ToString("0.####"));
    }

    public float GetElapsedSeconds()
    {
        if (currentStopwatch != null && currentStopwatch.IsRunning)
            return currentStopwatch.ElapsedMilliseconds / 1000f;
        return 0;
    }

    private void CreateMeasurementFileManipulation()
    {
        File.WriteAllText(filePathManipulation,
            "UserID;PrimaryHand;InteractionTechnique;TaskID;Type;TaskObject;Distance;Radius;NeededDoFs;ManipulationAmount;Success;Time;Position Difference;RotationDifference;ScaleDifference;PrimaryHandTranslationFootprint;SecondaryHandTranslationFootprint");
    }

    private void CreateMeasurementFileSelection()
    {
        File.WriteAllText(filePathSelection,
            "UserID;PrimaryHand;InteractionTechnique;TaskID;Type;TaskObject;Distance;Radius;NumberOfObjects;MinDensity;Success;Time;Misses;PrimaryHandTranslationFootprint;SecondaryHandTranslationFootprint");
    }

    private void AppendToFile(string filePath, params object[] values)
    {
        File.AppendAllText(filePath, "\n" + String.Join(";", values));
    }

    private string GetBackupPath(string fileName)
    {
        int i = 1;
        string newFilePath;
        while (true)
        {
            newFilePath = Path.Combine(Application.streamingAssetsPath, fileName + "_backup" + i + extension);
            if (!File.Exists(newFilePath))
                break;
            i++;
        }

        return newFilePath;

    }
}
