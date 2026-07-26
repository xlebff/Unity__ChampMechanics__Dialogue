using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the dialogue UI, including text display, answer buttons, and show/hide animations.
/// </summary>
[RequireComponent(typeof(FastShowAndHide))]
public class DialogueGUI : MonoBehaviour
{
    [SerializeField] private GameObject answerPrefab;

    private RectTransform _answersPanel;
    private TMP_Text _textField;
    private FastShowAndHide _showAndHide;
    private Coroutine _currentCoroutine;

    private const float DEFAULT_DELAY = 0.05f;

    #region Unity Lifecycle

    /// <summary>
    /// Called when the component is reset in the Unity Editor.
    /// Caches required child components for performance.
    /// </summary>
    private void Reset()
    {
        _answersPanel = GetComponentInChildren<RectTransform>();
        _textField = GetComponentInChildren<TMP_Text>();
        _showAndHide = GetComponent<FastShowAndHide>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the dialogue UI using the attached FastShowAndHide component.
    /// </summary>
    public void CreateDialogueUI()
        => _showAndHide.Show();

    /// <summary>
    /// Hides the dialogue UI and destroys all answer buttons.
    /// </summary>
    public void DestroyDialogueUI()
    {
        DestroyAnswers();
        _showAndHide.Hide();
    }

    /// <summary>
    /// Instantiates answer buttons for the given list of branches.
    /// Each button displays the branch's localized text and invokes its associated actions on click.
    /// </summary>
    /// <param name="branches">List of dialogue branches to create answers for.</param>
    public void CreateAnswers(List<Branch> branches)
    {
        foreach (Branch branch in branches)
        {
            GameObject answerObj = Instantiate(answerPrefab, _answersPanel);

            Button answerObjButton = answerObj.GetComponent<Button>();
            TMP_Text answerObjText = answerObj.GetComponentInChildren<TMP_Text>();

            if (answerObjButton == null || answerObjText == null)
            {
                Debug.LogError("Error during getting answer components. Aborting.");
                return;
            }

            answerObjText.text = branch.Text.GetLocalizedString();

            answerObjButton.onClick.AddListener(() =>
            {
                if (branch.CloseDialogueWindow) DestroyDialogueUI();
                DestroyAnswers();
                branch.AdditionalScripts.ForEach(m => m?.Invoke());
            });
        }
    }

    /// <summary>
    /// Destroys all answer button GameObjects currently in the answers panel.
    /// </summary>
    public void DestroyAnswers()
    {
        if (_answersPanel.childCount == 0) return;

        foreach (Transform child in _answersPanel)
            Destroy(child.gameObject);
    }

    /// <summary>
    /// Prints the given text to the dialogue text field character by character with a delay.
    /// If a previous printing coroutine is running, it is stopped and the new text is shown immediately.
    /// </summary>
    /// <param name="text">The full text to display.</param>
    /// <param name="delay">Delay between each character (default = 0.05f).</param>
    public void PrintText(string text, float delay = DEFAULT_DELAY)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
            _textField.text = text;
        }
        else _currentCoroutine = StartCoroutine(Printing(text, delay));
    }

    /// <summary>
    /// Immediately displays the given text without the typewriter effect.
    /// If a printing coroutine is active, it is stopped.
    /// </summary>
    /// <param name="text">The text to display immediately.</param>
    public void ShowTextImmediately(string text)
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        _textField.text = text;
    }

    #endregion

    #region Private Coroutines

    /// <summary>
    /// Coroutine that displays the given text one character at a time.
    /// </summary>
    /// <param name="text">The text to type out.</param>
    /// <param name="delay">Delay between each character.</param>
    /// <returns>IEnumerator for coroutine.</returns>
    private IEnumerator Printing(string text, float delay)
    {
        _textField.text = string.Empty;

        foreach (var c in text)
        {
            _textField.text += c;
            yield return new WaitForSeconds(delay);
        }

        _currentCoroutine = null;
    }

    #endregion
}