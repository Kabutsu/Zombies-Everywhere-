[System.Serializable]
public class SerializedQuestList
{
    public SerializedQuest[] quests;
}

[System.Serializable]
public class SerializedQuest
{
    public string questName;
    public SerializedVariables variables;
    public SerializedQuestStep[] questSteps;
}

[System.Serializable]
public class SerializedVariables
{
    public string[] names;
    public string[] values;
}

[System.Serializable]
public class SerializedQuestStep
{
    public string tag;
    public string description;
    public string generalDescription;
    public SerializedSkipIf skipIf;
    public SerializedPreconditions preconditions;
    public bool optional;
    public string[] dialogue;
    public SerializedResult result;
}

[System.Serializable]
public class SerializedSkipIf
{
    public string[] names;
    public string[] values;
}

[System.Serializable]
public class SerializedPreconditions
{
    public string[] names;
    public string[] values;
}

[System.Serializable]
public class SerializedResult
{
    public string[] names;
    public string[] values;
}
