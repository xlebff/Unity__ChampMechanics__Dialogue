using System;
using UnityEngine;
using UnityEngine.Localization;

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
    public Target Target;

    public QuestAction(QuestStepType come, Target target)
    {
        this.QuestStepType = come;
        this.Target = target;
    }
}

[Serializable]
public class QuestStep
{
    public LocalizedString QuestStepName;
    public LocalizedString QuestStepDescription;
    public QuestAction QuestAction;
    public Vector3 PointerPosition;
}

[CreateAssetMenu(fileName = "Quest", menuName = "Quests/Quest", order = 1)]
public class Quest : ScriptableObject
{
    public LocalizedString QuestName;
    public QuestAction Condition;
    public Vector3 StartPosition;
    public QuestAction StartQuestAction;
    public QuestStep[] Steps;
}