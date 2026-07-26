using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
[RequireComponent(typeof(ContentSizeFitter))]
public class QuestGUIObjectStorage : MonoBehaviour
{
    public TMP_Text QuestTitleTextField;
    public TMP_Text StepTitleTextField;
    public TMP_Text StepInfoTextField;

    private void Reset()
    {
        ContentSizeFitter filter = GetComponent<ContentSizeFitter>();
        filter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text[] fields = GetComponentsInChildren<TMP_Text>();
        QuestTitleTextField = fields[0];
        StepTitleTextField = fields[1];
        StepInfoTextField = fields[2];

        VerticalLayoutGroup vGroup = GetComponent<VerticalLayoutGroup>();
        vGroup.childControlWidth = true;
        vGroup.childForceExpandHeight = false;
    }
}
