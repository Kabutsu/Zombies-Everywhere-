[System.Serializable]
public class SerializedQuestList //equates to the Dictionary of Quests in QuestManager
{
    public SerializedQuest[] quests;
}

[System.Serializable]
public class SerializedQuest //equates to an individual Quest in QuestManager
{
    public string questName;
    public SerializedVariables variables;
    public SerializedQuestStep[] questSteps; //equates to the Stack of QuestSteps in a Quest
}

[System.Serializable]
public class SerializedVariables //equates to the variables Dictionary of a Quest in QuestManager
{
    public string[] names; //variable names
    public string[] values; //variable values as strings
}

[System.Serializable]
public class SerializedQuestStep //equates to an individual QuestStep in QuestManager
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
public class SerializedSkipIf //equates to the skipIf Dictionary
{
    public string[] names;
    public string[] values;
}

[System.Serializable]
public class SerializedPreconditions //equates to the preconditions Dictionary
{
    public string[] names;
    public string[] values;
}

[System.Serializable]
public class SerializedResult //equates to the result Dictionary
{
    public string[] names;
    public string[] values;
}
