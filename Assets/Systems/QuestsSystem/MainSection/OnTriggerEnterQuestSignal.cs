using UnityEngine;

public class OnTriggerEnterQuestSygnal : MonoBehaviour
{
    [SerializeField] private QuestStepType type;
    [SerializeField] private string target;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        QuestsController.Instance
            .ActionProcessing(new QuestAction(
                type,
                target));

        Destroy(this);
    }
}
