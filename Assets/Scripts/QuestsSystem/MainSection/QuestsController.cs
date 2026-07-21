using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
class QuestsStorage
{
    public QuestsSO Quest;
    public int Step;
    public GameObject Pointer;
}

public class QuestsController : MonoBehaviour
{
    [SerializeField] private List<QuestsStorage> UntakenQuestsList;
    [SerializeField] private List<QuestsStorage> TakenQuestsList;
    [SerializeField] private List<QuestsStorage> DoneQuestsList;

    [SerializeField] private GameObject QuestStartPointerPrefab;
    [SerializeField] private GameObject QuestStepPointerPrefab;

    public static QuestsController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"Another Quests Controller was found and destroyed: {name}.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("Instance for Quests Controller was placed.");

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        foreach (var quest in UntakenQuestsList)
        {
            quest.Pointer = 
                Instantiate(QuestStartPointerPrefab,
                quest.Quest.StartPosition, 
                Quaternion.identity);

            Debug.Log($"Untaken quest {quest.Quest.QuestName} found and placed.");
        }

        foreach (var quest in TakenQuestsList)
        {
            quest.Pointer =
                Instantiate(QuestStepPointerPrefab,
                quest.Quest.Steps[quest.Step].PointerPosition,
                Quaternion.identity);

            Debug.Log($"Taken quest {quest.Quest.QuestName} found and placed.\n" +
                $"Current step: {quest.Step}.");
        }
    }

    public void StartQuest(QuestsSO quest)
    {
        QuestsStorage questRecord = UntakenQuestsList.FirstOrDefault(qs => qs.Quest == quest);
        if (questRecord == null)
        {
            Debug.LogError($"No quest {quest} was found.");
            return; 
        }

        questRecord.Step = 0;
        TakenQuestsList.Add(questRecord);
        UntakenQuestsList.Remove(questRecord);

        Destroy(questRecord.Pointer);
        questRecord.Pointer =
            Instantiate(QuestStepPointerPrefab,
            questRecord.Quest.Steps[0].PointerPosition,
            Quaternion.identity);

        Debug.Log($"Quest {quest.QuestName} has been taken.");
    }

    public void ActionProcessing(QuestAction action)
    {
        QuestsStorage questRecord = TakenQuestsList.FirstOrDefault(
            qsa => qsa.Quest.Steps[qsa.Step].QuestAction.Target == action.Target &&
            qsa.Quest.Steps[qsa.Step].QuestAction.QuestStepType == action.QuestStepType);


        if (questRecord == null)
        {
            Debug.Log($"Action {action.QuestStepType} on {action.Target} was proceed; " +
                $"no quest demand on it found.");
            return;
        }
        
        ++questRecord.Step;

        if (questRecord.Step >= questRecord.Quest.Steps.Length)
        {
            Destroy(questRecord.Pointer);
            Debug.Log($"Step {questRecord.Step + 1} of the quest {questRecord.Quest.QuestName} reached steps limit" +
                $" ({questRecord.Quest.Steps.Length}).");
            // sth like "QuestEnd()";
            return;
        }

        Destroy(questRecord.Pointer);
        questRecord.Pointer =
            Instantiate(QuestStepPointerPrefab,
            questRecord.Quest.Steps[questRecord.Step].PointerPosition,
            Quaternion.identity);

        Debug.Log($"Quest {questRecord.Quest.QuestName} is on {questRecord.Step + 1}.");
    }
}
