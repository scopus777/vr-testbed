using TMPro;

/// <summary>
/// Manages the status text visible to the evaluator
/// </summary>
public class StatusTextController : Singleton<StatusTextController>
{
    public TMP_Text statusText;
    public TMP_Text userActionRequest;

    private readonly string stringTemplateTasksPhase =
        "Current Technique: {0} \nOverall Progress: {1}/{2} \nTask ID: {3} \nPhase: {4} \nType: {5} \nTask Progress: {6}/{7}";
    
    private readonly string stringTemplateTrainingPhase =
        "Current Technique: {0} \nOverall Progress: {1}/{2} \nTask ID: {3} \nPhase: {4} \nType: {5} \nInstruction Progress: {6}/{7}";

    public void UpdateStatusTextTasksPhase(string currentTechniqueName, int currentTechniqueNumber, int techniqueCount,
        TaskController.TaskPhase phase, string id, TaskType taskType, int currentTaskNumber, int taskCount)
    {
        statusText.text = string.Format(stringTemplateTasksPhase, currentTechniqueName, currentTechniqueNumber, techniqueCount,
            id, phase, taskType.ToTaskTypeString(), currentTaskNumber, taskCount);
    }
    
    public void UpdateStatusTextTrainingsPhase(string currentTechniqueName, int currentTechniqueNumber, int techniqueCount,
        TaskController.TaskPhase phase, string id, TaskType taskType, int currentInstruction, int instructionCount)
    {
        statusText.text = string.Format(stringTemplateTrainingPhase, currentTechniqueName, currentTechniqueNumber, techniqueCount,
            id, phase, taskType.ToTaskTypeString(), currentInstruction, instructionCount);
    }

    public void UpdateStatusText(string text)
    {
        statusText.text = text;
    }

    public void UpdateUserActionRequest(string text)
    {
        userActionRequest.text = text;
    }
}