using System;
using UnityEngine;

[Serializable]
public enum QuestStepType
{
    Talk,
    Come,
    Take,
    Use
}

[Serializable]
public record QuestAction
{
    public QuestStepType QuestStepType;
    public string Target;
}

[Serializable]
public class QuestStep
{
    public string QuestStepName;
    public string QuestStepDescription;
    public QuestAction QuestAction;
    public Vector3 PointerPosition;
}

[CreateAssetMenu(fileName = "Quest", menuName = "Quests/Quest", order = 1)]
public class QuestsSO : ScriptableObject
{
    public string QuestName;
    public Vector3 StartPosition;
    public QuestStep[] Steps;
}