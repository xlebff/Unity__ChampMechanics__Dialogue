#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>Простейшая модель ветвления диалога.</summary>
[Serializable]
public class Branching
{
    [Tooltip("Индекс реплики, после которой появляется выбор")]
    public int At;
    [Tooltip("Индекс реплики для перехода (прыжок)")]
    public int To;
    [Tooltip("Текст ответа «прыгнуть» (пропустить часть реплик)")]
    public string JumpAnswerText = "Пропустить";
    [Tooltip("Текст ответа «остаться» (продолжить по порядку)")]
    public string NoJumpAnswerText = "Продолжить";
}

[RequireComponent(typeof(AudioSource))]
public class DialogueController : MonoBehaviour
{
    [Header("Localization")]
    [SerializeField] private LocalizedStringTable textTable = null!;
    [SerializeField] private LocalizedAssetTable audioTable = null!;

    [Header("Input")]
    [SerializeField] private InputActionReference input = null!;

    [Header("Dialogue Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private DialogueGUIStorage gui = null!;
    [SerializeField] private AudioSource? audioSource;

    [Header("Branching")]
    [SerializeField] private List<Branching> branchings = new List<Branching>();

    private StringTable? currentStringTable;
    private AssetTable? currentAudioTable;
    private int index = -1;
    private Coroutine? typingCoroutine;
    private string currentFullText = string.Empty;
    private bool isTyping;
    private bool isChoosing;
    private CancellationTokenSource? audioLoadCts;

    private void OnValidate()
    {
        AudioSource? temp = GetComponent<AudioSource>();
        if (temp != null && audioSource == null)
            audioSource = temp;
    }

    private void Start()
    {
        StartCoroutine(InitializeTablesAndBegin());
    }

    private void OnEnable()
    {
        input.action.performed += OnInputTriggered;
        audioTable.TableChanged += OnAudioTableChanged;
        textTable.TableChanged += OnStringTableChanged;
    }

    private void OnDisable()
    {
        input.action.performed -= OnInputTriggered;
        textTable.TableChanged -= OnStringTableChanged;
        audioTable.TableChanged -= OnAudioTableChanged;
        StopAllCoroutines();
        gui.ClearTextField();
        if (audioSource != null)
            audioSource.Stop();

        // Отменяем все текущие загрузки аудио
        audioLoadCts?.Cancel();
        audioLoadCts?.Dispose();
        audioLoadCts = null;
    }

    private IEnumerator InitializeTablesAndBegin()
    {
        // Дожидаемся загрузки обеих таблиц
        var stringTask = textTable.GetTableAsync().Task;
        var audioTask = audioTable.GetTableAsync().Task;

        while (!stringTask.IsCompleted || !audioTask.IsCompleted)
            yield return null;

        currentStringTable = stringTask.Result;
        currentAudioTable = audioTask.Result;

        NextDialogue();
    }

    private void OnStringTableChanged(StringTable table)
    {
        currentStringTable = table;
        DisplayCurrentDialogue();
    }

    private void OnAudioTableChanged(AssetTable table)
    {
        currentAudioTable = table;
        ReloadAudioForCurrentIndex();
    }

    private void OnInputTriggered(InputAction.CallbackContext _)
    {
        // Если активен выбор ответа — игнорируем кнопку продолжения
        if (isChoosing)
            return;

        // Если текст ещё печатается — завершаем печать мгновенно
        if (isTyping)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            gui.GetTextField().text = currentFullText;
            isTyping = false;
            typingCoroutine = null;
            return;
        }

        NextDialogue();
    }

    private async void NextDialogue()
    {
        try
        {
            ++index;

            if (currentStringTable == null)
                return;

            if (index >= currentStringTable.Count)
            {
                EndDialogue();
                return;
            }

            string key = index.ToString();

            // Отменяем предыдущую загрузку аудио и создаём новый токен
            audioLoadCts?.Cancel();
            audioLoadCts?.Dispose();
            audioLoadCts = new CancellationTokenSource();
            CancellationToken token = audioLoadCts.Token;

            if (currentAudioTable != null)
            {
                AudioClip? clip = null;
                try
                {
                    clip = await currentAudioTable.GetAssetAsync<AudioClip>(key).Task;
                }
                catch (OperationCanceledException) { /* отменено – ок */ }
                catch (Exception ex) { Debug.LogError($"Failed to load audio clip: {ex}"); }

                if (!token.IsCancellationRequested && clip != null && audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }

            string localizedText = currentStringTable.GetEntry(key).GetLocalizedString();
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(localizedText));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in NextDialogue: {ex}");
        }
    }

    private void DisplayCurrentDialogue()
    {
        if (currentStringTable == null || index < 0 || index >= currentStringTable.Count)
            return;

        string key = index.ToString();
        string localizedText = currentStringTable.GetEntry(key).GetLocalizedString();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        gui.GetTextField().text = localizedText;
        isTyping = false;
        currentFullText = localizedText;
    }

    private async void ReloadAudioForCurrentIndex()
    {
        if (currentAudioTable == null || currentStringTable == null || index < 0 || index >= currentStringTable.Count)
            return;

        string key = index.ToString();
        audioLoadCts?.Cancel();
        audioLoadCts?.Dispose();
        audioLoadCts = new CancellationTokenSource();
        CancellationToken token = audioLoadCts.Token;

        try
        {
            AudioClip? clip = await currentAudioTable.GetAssetAsync<AudioClip>(key).Task;
            if (!token.IsCancellationRequested && clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Debug.LogError($"Failed to reload audio: {ex}"); }
    }

    private IEnumerator TypeText(string fullText)
    {
        currentFullText = fullText;
        isTyping = true;
        var textField = gui.GetTextField();
        gui.ClearTextField();

        foreach (char c in fullText)
        {
            textField.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;

        // После завершения печати проверяем, есть ли ветвление для текущей реплики
        OnTypingComplete();
    }

    private void OnTypingComplete()
    {
        if (isChoosing)
            return;

        Branching? branching = branchings.Find(b => b.At == index);
        if (branching != null)
        {
            isChoosing = true;
            gui.ShowChoices(
                branching.JumpAnswerText,
                branching.NoJumpAnswerText,
                OnChoiceMade
            );
        }
    }

    private void OnChoiceMade(int choice)
    {
        if (!isChoosing)
            return;

        isChoosing = false;
        gui.HideChoices();

        Branching? branching = branchings.Find(b => b.At == index);
        if (branching == null)
        {
            // На всякий случай переходим дальше
            NextDialogue();
            return;
        }

        // choice == 0 → прыжок (переход на индекс To), иначе продолжение (index+1)
        int targetIndex = (choice == 0) ? branching.To : index + 1;
        index = targetIndex - 1; // NextDialogue сделает ++index и мы попадём куда нужно
        NextDialogue();
    }

    private void EndDialogue()
    {
        enabled = false;
    }
}