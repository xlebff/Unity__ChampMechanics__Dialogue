using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(QuestsController))]
public class QuestsGUI : MonoBehaviour, ICustomStart
{
    [SerializeField] private InputActionReference openQuestsMenuAction;
    [SerializeField] private FastShowAndHide questsMenu;
    [SerializeField] private Transform questsPanelTransform;
    [SerializeField] private GameObject questContainerPrefab;

    [Header("UntakenQuests")]
    [SerializeField] private LocalizedString availableQuestString;
    [SerializeField] private Color[] availableQuestColor = { Color.gray, Color.lightGray };

    [Header("ActiveQuests")]
    [SerializeField] private Color[] inProgressQuestColor = { Color.blue, Color.lightBlue };

    [Header("CompletedQuests")]
    [SerializeField] private LocalizedString completedQuestString;
    [SerializeField] private Color[] completedQuestColor = { Color.green, Color.lightGreen };

    private readonly Dictionary<Quest, QuestGUIObjectStorage> _questUiMap = new();

    private QuestsController _controller;
    private bool _isOpen;

    // Индексы для позиционирования новых элементов в иерархии
    private int _inProgressStartIndex;
    private int _availableStartIndex;
    private int _completedStartIndex;

    // ------------------------------------------------------------------------
    // ICustomStart
    // ------------------------------------------------------------------------
    public void CustomStart()
    {
        _controller = QuestsController.Instance;

        openQuestsMenuAction.action.performed += OnMenuButtonPressed;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        _controller.QuestStartNotify += OnQuestStarted;
        _controller.QuestEndNotify += OnQuestEnded;
        _controller.ProgressNotify += OnQuestProgress;

        // Создаём UI-элементы для всех групп
        InitQuestsGroup(_controller.GetInProgressQuestsList(), inProgressQuestColor, _inProgressStartIndex);
        _availableStartIndex = questsPanelTransform.childCount;
        InitQuestsGroup(_controller.GetAvailableQuestsList(), availableQuestColor, _availableStartIndex);
        _completedStartIndex = questsPanelTransform.childCount;
        InitQuestsGroup(_controller.GetCompletedQuestsList(), completedQuestColor, _completedStartIndex);

        // Заполняем тексты (локализация)
        RefreshAllQuestUI();
    }

    private void OnDestroy()
    {
        openQuestsMenuAction.action.performed -= OnMenuButtonPressed;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        _controller.QuestStartNotify -= OnQuestStarted;
        _controller.QuestEndNotify -= OnQuestEnded;
        _controller.ProgressNotify -= OnQuestProgress;
    }

    // ------------------------------------------------------------------------
    // Инициализация UI-элементов (две перегрузки для разных типов списков)
    // ------------------------------------------------------------------------
    private void InitQuestsGroup(List<QuestRecord> questsList, Color[] questsColor, int sectionStartIndex)
    {
        foreach (var record in questsList)
            CreateQuestUi(record.Quest, questsColor, sectionStartIndex);
    }

    private void InitQuestsGroup(List<Quest> questsList, Color[] questsColor, int sectionStartIndex)
    {
        foreach (var quest in questsList)
            CreateQuestUi(quest, questsColor, sectionStartIndex);
    }

    // Общий метод создания одного UI-элемента
    private void CreateQuestUi(Quest quest, Color[] colors, int siblingIndex)
    {
        var ui = Instantiate(questContainerPrefab, questsPanelTransform)
            .GetComponent<QuestGUIObjectStorage>();
        _questUiMap.Add(quest, ui);
        ui.transform.SetSiblingIndex(siblingIndex);
        ApplyColors(ui, colors);
    }

    // ------------------------------------------------------------------------
    // Обновление UI (тексты, цвета, позиция)
    // ------------------------------------------------------------------------
    private void RefreshAllQuestUI()
    {
        // Активные квесты
        foreach (var record in _controller.GetInProgressQuestsList())
        {
            UpdateQuestUi(record.Quest,
                stepTitle: record.Quest.Steps[record.Step].QuestStepName.GetLocalizedString(),
                stepInfo: record.Quest.Steps[record.Step].QuestStepDescription.GetLocalizedString(),
                colors: inProgressQuestColor);
        }

        // Доступные квесты
        foreach (var record in _controller.GetAvailableQuestsList())
        {
            UpdateQuestUi(record.Quest,
                stepTitle: availableQuestString.GetLocalizedString(),
                stepInfo: string.Empty,
                colors: availableQuestColor);
        }

        // Завершённые квесты
        foreach (var quest in _controller.GetCompletedQuestsList())
        {
            UpdateQuestUi(quest,
                stepTitle: completedQuestString.GetLocalizedString(),
                stepInfo: string.Empty,
                colors: completedQuestColor);
        }
    }

    // Единый метод обновления данных одного квеста в UI
    private void UpdateQuestUi(Quest quest, string stepTitle, string stepInfo, Color[] colors, int? newSiblingIndex = null)
    {
        if (!_questUiMap.TryGetValue(quest, out var ui))
        {
            Debug.LogError($"No UI for quest '{quest.QuestName.GetLocalizedString()}'");
            return;
        }

        ui.QuestTitleTextField.text = quest.QuestName.GetLocalizedString();
        ui.StepTitleTextField.text = stepTitle;
        ui.StepInfoTextField.text = stepInfo;
        ApplyColors(ui, colors);

        if (newSiblingIndex.HasValue)
            ui.transform.SetSiblingIndex(newSiblingIndex.Value);
    }

    private void ApplyColors(QuestGUIObjectStorage ui, Color[] colors)
    {
        ui.QuestTitleTextField.color = colors[0];
        ui.StepTitleTextField.color = colors[1];
        ui.StepInfoTextField.color = colors[1];
    }

    // ------------------------------------------------------------------------
    // Обработчики событий контроллера
    // ------------------------------------------------------------------------
    private void OnQuestEnded(object _, QuestMigratedEventArgs e)
    {
        UpdateQuestUi(e.Quest,
            stepTitle: completedQuestString.GetLocalizedString(),
            stepInfo: string.Empty,
            colors: completedQuestColor,
            newSiblingIndex: --_completedStartIndex);

        --_availableStartIndex;
        Debug.Log($"Quest '{e.Quest.QuestName.GetLocalizedString()}' marked as completed.");
    }

    private void OnQuestStarted(object _, QuestMigratedEventArgs e)
    {
        var step = e.Quest.Steps[0];
        UpdateQuestUi(e.Quest,
            stepTitle: step.QuestStepName.GetLocalizedString(),
            stepInfo: step.QuestStepDescription.GetLocalizedString(),
            colors: inProgressQuestColor,
            newSiblingIndex: 0); // ставим первым

        ++_availableStartIndex;
        ++_completedStartIndex;
        Debug.Log($"Quest '{e.Quest.QuestName.GetLocalizedString()}' added to active quests (GUI).");
    }

    private void OnQuestProgress(object _, QuestUpdatedEventArgs e)
    {
        var step = e.Quest.Steps[e.Step];
        UpdateQuestUi(e.Quest,
            stepTitle: step.QuestStepName.GetLocalizedString(),
            stepInfo: step.QuestStepDescription.GetLocalizedString(),
            colors: inProgressQuestColor,
            newSiblingIndex: 0); // поднимаем вверх
        Debug.Log($"Quest '{e.Quest.QuestName.GetLocalizedString()}' updated (GUI).");
    }

    // ------------------------------------------------------------------------
    // Локализация
    // ------------------------------------------------------------------------
    private void OnLocaleChanged(Locale _) => RefreshAllQuestUI();

    // ------------------------------------------------------------------------
    // Отображение / скрытие меню
    // ------------------------------------------------------------------------
    private void OnMenuButtonPressed(InputAction.CallbackContext _)
    {
        _isOpen = !_isOpen;
        if (_isOpen) questsMenu.Show();
        else questsMenu.Hide();
    }
}