using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueGUIStorage : MonoBehaviour
{
    [SerializeField] private TMP_Text textField;
    [SerializeField] private Button[] choises = new Button[2];

    public TMP_Text GetTextField() => textField;

    public void ClearTextField() => textField.text = string.Empty;

    public void HideChoices()
    {
        foreach (var choise in choises)
        {
            choise.gameObject.SetActive(false);
        }
    }

    public void ShowChoices(string option1, string option2, Action<int> onChosen)
    {
        choises[0].GetComponentInChildren<TMP_Text>().text = option1;
        choises[0].onClick.AddListener(() => onChosen(0));
        choises[1].GetComponentInChildren<TMP_Text>().text = option2;
        choises[1].onClick.AddListener(() => onChosen(1));

        foreach (var choise in choises)
        {
            choise.gameObject.SetActive(true);
        }
    }
}
