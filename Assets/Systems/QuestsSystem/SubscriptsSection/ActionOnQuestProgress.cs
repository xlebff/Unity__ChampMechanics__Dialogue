using System;
using UnityEngine;

[Serializable]
enum ProgressState
{
    Start,
    End,
    Progress
}

[Serializable]
enum ProgressAction
{
    Spawn,
    Despawn
}

public class ActionOnQuestProgress : MonoBehaviour, ICustomStart
{
    [SerializeField] private Quest quest;

    [SerializeField] private ProgressState state;
    [SerializeField]
    [Tooltip("Indexing from 0. Wonrking with " +
        "\"Progress\" state only.")]
    private int step;

    [SerializeField] private ProgressAction action;

    [SerializeField] private GameObject[] objs;

    private void OnValidate()
    {
        int maxStep = quest.Steps.Length - 1;

        if (quest != null &&
            state == ProgressState.Progress &&
            maxStep < step)
        {
            Debug.LogAssertion("Step value was greater than max steps " +
                "of the quest and was normalized.");
            step = maxStep;
        }
    }

    public void CustomStart()
    {
        QuestsController controller = QuestsController.Instance;

        switch (state)
        {
            case ProgressState.Start:
                controller.QuestStartNotify += MigrationListener;
                break;
            case ProgressState.End:
                controller.QuestEndNotify += MigrationListener;
                break;
            case ProgressState.Progress:
                controller.ProgressNotify += ProgressListener;
                break;
            default:
                Debug.LogError("Unexpected case occured.");
                break;
        }
    }

    private void ProgressListener(object _, QuestUpdatedEventArgs e)
    {
        if (!(e.Quest == quest && e.Step == step))
            return;

        DoAction();
    }

    private void MigrationListener(object _, QuestMigratedEventArgs e)
    {
        if (e.Quest != quest) return;

        DoAction();
    }

    private void DoAction()
    {
        bool activity = false;
        if (action == ProgressAction.Spawn) activity = true;

        foreach (GameObject o in objs)
        {
            o.SetActive(activity);
            Debug.Log($"{o.name} is {o.activeSelf} now.");
        }

        QuestsController.Instance.ProgressNotify -= ProgressListener;
        enabled = false;
    }
}
