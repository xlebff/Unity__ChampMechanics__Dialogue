using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestUpdatedEventArgs : EventArgs
{
    public readonly Quest Quest;
    public readonly int Step;

    public QuestUpdatedEventArgs(Quest quest, int step)
    {
        Quest = quest;
        Step = step;
    }
}

public class QuestMigratedEventArgs : EventArgs
{
    public readonly Quest Quest;

    public QuestMigratedEventArgs(Quest quest)
    {
        Quest = quest;
    }
}

[Serializable]
public class QuestRecord
{
    public Quest Quest;
    [Tooltip("No need to edit! Only on extra.")] public int Step;
    [Tooltip("No need to fill! Just for demo!")] public GameObject Pointer;
}


public class QuestsController : MonoBehaviour, ICustomStart
{
    [SerializeField] private List<QuestRecord> unavailableQuestsList;
    [SerializeField] private List<QuestRecord> availableQuestsList;
    [SerializeField] private List<QuestRecord> inProgressQuestsList;
    [SerializeField] private List<Quest> completedQuestsList;

    [SerializeField] private GameObject questStartPointerPrefab;
    [SerializeField] private GameObject questStepPointerPrefab;

    public event EventHandler<QuestMigratedEventArgs> QuestStartNotify;
    public event EventHandler<QuestMigratedEventArgs> QuestEndNotify;
    public event EventHandler<QuestUpdatedEventArgs> ProgressNotify;

    public static QuestsController Instance { get; private set; }

    // ------------------------------------------------------------------------
    // ICustomStart
    // ------------------------------------------------------------------------
    public void CustomStart()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"Another Quests Controller was found and destroyed: {name}.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("Instance for Quests Controller was placed.");

        // Создаём указатели для всех доступных и активных квестов
        foreach (var record in availableQuestsList)
        {
            CreatePointer(record, questStartPointerPrefab, record.Quest.StartPosition);
            Debug.Log($"Untaken quest {record.Quest.QuestName.GetLocalizedString()} found and placed.");
        }

        foreach (var record in inProgressQuestsList)
        {
            var pos = record.Quest.Steps[record.Step].PointerPosition;
            CreatePointer(record, questStepPointerPrefab, pos);
            Debug.Log($"Taken quest {record.Quest.QuestName.GetLocalizedString()} found and placed.\n" +
                      $"Current step: {record.Step + 1}.");
        }
    }

    // ------------------------------------------------------------------------
    // Публичные методы для внешнего вызова
    // ------------------------------------------------------------------------
    public void TryStartQuest(QuestAction action)
    {
        var record = availableQuestsList.FirstOrDefault(
            q => q.Quest.StartQuestAction.QuestStepType == action.QuestStepType &&
                 q.Quest.StartQuestAction.Target == action.Target);

        if (record == null)
        {
            Debug.Log("No quests with this start action found. Aborting.");
            return;
        }

        StartQuest(record);
        Debug.Log($"Quest {record.Quest.QuestName.GetLocalizedString()} started.");
    }

    public void ActionProcessing(QuestAction action)
    {
        var record = inProgressQuestsList.FirstOrDefault(
            q => q.Quest.Steps[q.Step].QuestAction.Target == action.Target &&
                 q.Quest.Steps[q.Step].QuestAction.QuestStepType == action.QuestStepType);

        if (record == null)
        {
            Debug.Log($"Action {action.QuestStepType} on {action.Target} was proceed; " +
                      "no quest demand on it found. Falling to starting.");
            TryStartQuest(action);
            return;
        }

        // Продвигаем шаг
        record.Step++;

        if (record.Step >= record.Quest.Steps.Length)
        {
            Debug.Log($"Step {record.Step + 1} of the quest {record.Quest.QuestName.GetLocalizedString()} " +
                      $"reached steps limit ({record.Quest.Steps.Length}).");
            EndQuest(record, action);
            return;
        }

        // Обновляем указатель на новый шаг
        UpdatePointer(record, record.Quest.Steps[record.Step].PointerPosition);
        ProgressNotify?.Invoke(this, new QuestUpdatedEventArgs(record.Quest, record.Step));
        Debug.Log($"Quest {record.Quest.QuestName.GetLocalizedString()} is on {record.Step + 1}.");
    }

    // ------------------------------------------------------------------------
    // Внутренние методы управления квестами
    // ------------------------------------------------------------------------
    private void StartQuest(QuestRecord record)
    {
        record.Step = 0;
        inProgressQuestsList.Add(record);
        availableQuestsList.Remove(record);

        DestroyPointer(record);
        CreatePointer(record, questStepPointerPrefab, record.Quest.Steps[0].PointerPosition);

        QuestStartNotify?.Invoke(this, new QuestMigratedEventArgs(record.Quest));
        Debug.Log($"Quest {record.Quest.QuestName.GetLocalizedString()} has been taken.");
    }

    private void EndQuest(QuestRecord record, QuestAction action)
    {
        completedQuestsList.Add(record.Quest);
        inProgressQuestsList.Remove(record);
        DestroyPointer(record);

        QuestEndNotify?.Invoke(this, new QuestMigratedEventArgs(record.Quest));
        Debug.Log($"Quest {record.Quest.QuestName.GetLocalizedString()} is over.");

        // Активируем новые доступные квесты (с условием)
        var toActivate = unavailableQuestsList
            .Where(q => q.Quest.Condition.Target == action.Target &&
                        q.Quest.Condition.QuestStepType == action.QuestStepType)
            .ToList(); // важно – материализуем, чтобы удалять из исходного списка

        foreach (var quest in toActivate)
        {
            availableQuestsList.Add(quest);
            unavailableQuestsList.Remove(quest);
            Debug.Log($"Quest {quest.Quest.QuestName.GetLocalizedString()} is available.");
        }
    }

    // ------------------------------------------------------------------------
    // Вспомогательные методы для работы с указателями
    // ------------------------------------------------------------------------
    private void CreatePointer(QuestRecord record, GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Pointer prefab is null for quest {record.Quest.QuestName.GetLocalizedString()}");
            return;
        }
        record.Pointer = Instantiate(prefab, position, Quaternion.identity);
    }

    private void DestroyPointer(QuestRecord record)
    {
        if (record.Pointer != null)
            Destroy(record.Pointer);
        record.Pointer = null;
    }

    private void UpdatePointer(QuestRecord record, Vector3 newPosition)
    {
        DestroyPointer(record);
        CreatePointer(record, questStepPointerPrefab, newPosition);
    }

    // ------------------------------------------------------------------------
    // Геттеры для других систем (без изменений)
    // ------------------------------------------------------------------------
    public List<QuestRecord> GetAvailableQuestsList() => availableQuestsList;
    public List<QuestRecord> GetInProgressQuestsList() => inProgressQuestsList;
    public List<Quest> GetCompletedQuestsList() => completedQuestsList;
}