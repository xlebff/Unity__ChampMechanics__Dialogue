using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

/// <summary>
/// Data container for a single dialogue branch (answer option).
/// </summary>
[Serializable]
public class Branch
{
    /// <summary>Localized text displayed on the answer button.</summary>
    public LocalizedString Text;

    /// <summary>Additional UnityEvents invoked when the branch is selected.</summary>
    public List<UnityEvent> AdditionalScripts;

    /// <summary>If true, the dialogue window closes immediately after choosing this branch.</summary>
    public bool CloseDialogueWindow = false;
}

/// <summary>
/// Controls the flow of a dialogue: displays sentences, plays audio, handles branching,
/// and responds to player input.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DialogueController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Localization")]
    [SerializeField] private LocalizedStringTable textTable;
    [SerializeField] private LocalizedAssetTable audioTable;

    [Header("Input")]
    [SerializeField] private InputActionReference input;

    [Header("Dialogue Settings")]
    [SerializeField] private Target target;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool isRepeatable = false;

    [Header("Gui")]
    [SerializeField] private GameObject guiPrefab;

    [Header("Branching")]
    [SerializeField] private List<Branch> branches;

    #endregion

    #region Private Fields

    private StringTable _currentStringTable;
    private AssetTable _currentAudioTable;

    private DialogueGUI _gui;
    private int _index = 0;
    private int _dialogueLength;

    private bool _isBranching = false;

    #endregion

    #region Unity Lifecycle

    private void OnDisable()
    {
        input.action.performed -= NextSentence;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    #endregion

    #region Unity Event Handlers

    /// <summary>
    /// Called when another collider enters the trigger.
    /// Starts the dialogue if the player enters.
    /// </summary>
    /// <param name="other">The collider that entered.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _gui = Instantiate(guiPrefab, this.transform).GetComponent<DialogueGUI>();
        _gui.CreateDialogueUI();

        _dialogueLength = _currentStringTable.Count;

        input.action.performed += NextSentence;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        if (branches != null && branches.Count > 0)
            _isBranching = true;
    }

    /// <summary>
    /// Called when the selected locale changes.
    /// Refreshes the displayed text and audio tables for the new locale.
    /// </summary>
    /// <param name="locale">The new locale.</param>
    private void OnLocaleChanged(Locale locale)
    {
        _currentStringTable = textTable.GetTable();
        _currentAudioTable = audioTable.GetTable();
        _gui.ShowTextImmediately(_currentStringTable[_index].Value);
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Advances to the next sentence when the input action is performed.
    /// If the end is reached, handles branching or ends the dialogue.
    /// </summary>
    private void NextSentence(InputAction.CallbackContext _)
    {
        ++_index;

        if (_index >= _dialogueLength)
        {
            if (_isBranching) Branch();
            EndDialogue();
            return;
        }

        _gui.PrintText(_currentStringTable[_index].Value);
        StartCoroutine(PlayAudio(_index));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ends the dialogue: destroys the GUI, unsubscribes from input,
    /// and optionally marks the quest as complete (if not repeatable).
    /// </summary>
    private void EndDialogue()
    {
        _gui.DestroyDialogueUI();
        input.action.performed -= NextSentence;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        if (isRepeatable)
        {
            _index = 0;
            return;
        }

        if (target != null)
            QuestsController.Instance
                .ActionProcessing(new QuestAction(QuestStepType.Talk, target));

        this.enabled = false;
    }

    /// <summary>
    /// Starts the branching UI by creating answer buttons from the defined branches.
    /// </summary>
    private void Branch()
        => _gui.CreateAnswers(branches);

    #endregion

    #region Coroutines

    /// <summary>
    /// Loads and plays the audio clip associated with the current sentence index.
    /// </summary>
    /// <param name="index">The sentence index.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator PlayAudio(int index)
    {
        AssetTableEntry entry = _currentAudioTable.GetEntry(index);
        var loadOperation = _currentAudioTable.GetAssetAsync<AudioClip>(entry.Key);
        yield return loadOperation;
        if (loadOperation.IsDone)
        {
            audioSource.clip = loadOperation.Result;
            audioSource.Stop();
            audioSource.Play();
        }
    }

    #endregion
}