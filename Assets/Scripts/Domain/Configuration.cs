using System;
using System.Collections.Generic;

[Serializable]
public class Configuration
{
    public List<InteractionTechniqueConf> interactionTechniques;
    public string primaryHand;
    public int userId;
    public string studyType;
}

public class InteractionTechniqueConf
{
    public string name;
    public int maxSupportedDistance;
    public TaskType supportedTaskTypes;
}
