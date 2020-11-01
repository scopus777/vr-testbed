using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/// <summary>
/// Sets the pose actions of all SteamVR_Action_Pose objects in the scene (attached to the hands)
/// to the given pose action. This is needed to continue the tracking of the controller if the action
/// set has changed. If this script is disabled the pose actions are reset to their old value.
/// </summary>
public class UpdatePoseActionOnStart : MonoBehaviour
{
    public SteamVR_Action_Pose poseAction = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
    
    private Dictionary<SteamVR_Behaviour_Pose, SteamVR_Action_Pose> behaviourPoses;
    
    void Start()
    {
        behaviourPoses = new Dictionary<SteamVR_Behaviour_Pose, SteamVR_Action_Pose>();
        SteamVR_Behaviour_Pose[] behaviourPoseList = FindObjectsOfType<SteamVR_Behaviour_Pose>();
        foreach (SteamVR_Behaviour_Pose behaviourPose in behaviourPoseList)
        {
            behaviourPoses.Add(behaviourPose,behaviourPose.poseAction);
            behaviourPose.poseAction = poseAction;
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<SteamVR_Behaviour_Pose, SteamVR_Action_Pose> behaviourPose in behaviourPoses)
            behaviourPose.Key.poseAction = behaviourPose.Value;
    }
}
