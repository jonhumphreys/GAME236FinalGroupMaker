using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GroupRevealer : MonoBehaviour
{
    [Header("UI References")]
    public Transform PanelContainer;
    public GameObject GroupPanelPrefab;
    public Button RevealButton;
    public Image BackgroundImage;

    [Header("Logo Display")]
    public GroupLogoDisplay LogoDisplay;
    public Sprite[] GroupLogoSprites;

    [Header("Data Settings")]
    public string CsvFileName = "students.csv";

    [Header("Display Settings")]
    public int ColumnsPerRow = 4;

    [Header("Reveal Complete Settings")]
    public Color CompleteBackgroundColor = new Color(0f, 0.39f, 0f);
    public string InitialButtonText = "GO";
    public string NextButtonText = "NEXT";
    public string DoneButtonText = "DONE!";

    [Header("Animation Settings")]
    public float PanelAnimationDuration = 0.8f;
    public float PanelAnimationDelay = 0.1f;
    public Ease PanelAnimationEase = Ease.OutBack;
    public float PanelRotationAmount = 720f;
    public float OffScreenDistance = 2000f;
    public float CompleteBounceDelay = 0.05f;
    
    [Header("Startup Animation Targets")]
    public RectTransform TitleText;
    public RectTransform TitleFinalAnchor;
    public float TitleFlyDuration = 0.8f;
    public Ease TitleFlyEase = Ease.OutBack;

    public RectTransform RevealButtonRect;
    public RectTransform RevealButtonFinalAnchor;
    public float RevealButtonFlyDuration = 0.8f;
    public Ease RevealButtonFlyEase = Ease.OutBack;

    [Header("Startup Timing")]
    public float StartupDelay = 1f;       
    public float StartupStagger = 0.2f;     
    
    [Header("Fireworks Settings")]
    public GameObject FireworksParticlePrefab;
    public GameObject FlashParticlePrefab;
    public int FireworksCount = 10;
    public float FireworksSpawnDelay = 0.2f;
    public float FireworksMinX = -800f;
    public float FireworksMaxX = 800f;
    public float FireworksMinY = -400f;
    public float FireworksMaxY = 400f;
    public Transform FireworksParent;
    
    [Header("Screen Shake Settings")]
    public DoTweenScreenShake screenShake;

    private List<StudentGroup> studentGroups;
    private Dictionary<int, GroupPanelUI> groupPanels;
    private List<Vector3> panelOriginalPositions;
    private GridLayoutGroup gridLayoutGroup;
    private int currentStudentIndex;
    private int currentGroupIndex;
    private int maxStudentsPerGroup;
    private bool hasShownPanels;

    private void Start()
    {
        InitializeRevealState();
        CacheGridLayoutGroup();
        LoadStudentData();
        CreateGroupPanels();
        StorePanelOriginalPositions();
        HideAllPanels();
        SetupRevealButton();
        InitializeLogoDisplay();
        AnimateStartupUI();
    }

    public void RevealNextStudent()
    {
        if (!HasShownPanels())
        {
            HandleShowPanels();
            return;
        }

        UpdateRevealButtonText(NextButtonText);

        if (!HasStudentData())
        {
            Debug.LogWarning("No student data available!");
            return;
        }

        if (AllStudentsRevealed())
        {
            HandleRevealComplete();
            return;
        }

        RevealStudentInCurrentGroup();
        AdvanceToNextGroup();
    }

    public void OnShowLogoScreen(int groupNumber)
    {
        if (!IsValidGroupNumber(groupNumber))
        {
            Debug.LogError($"Group {groupNumber} not found!");
            return;
        }

        GroupPanelUI panel = GetGroupPanel(groupNumber);
        List<string> studentNames = panel.StudentNames;
        Sprite logoSprite = GetLogoSpriteForGroup(groupNumber);

        ShowLogoDisplay(groupNumber, studentNames, logoSprite);
    }
    
    public void OnLogoRevealed(int groupNumber, Sprite logoSprite)
    {
        if (!IsValidGroupNumber(groupNumber)) 
            return;

        DisplayLogoOnPanel(groupNumber, logoSprite);

        var panel = GetGroupPanel(groupNumber);
        
        string teamName;
        if (logoSprite != null)
            teamName = PrettyName(logoSprite.name);
        else
            teamName = $"Group {groupNumber}";
        
        panel.SetGroupTitle(teamName);
    }

    public void ResetReveal()
    {
        ResetRevealIndices();
        ResetRevealState();
        ClearAllPanels();
        ResetPanelPositions();
        HideAllPanels();
        EnableRevealButton();
        UpdateRevealButtonText(InitialButtonText);
        Debug.Log("Reveal reset");
    }
    
    private string PrettyName(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? raw : raw.Replace("_", " ").Trim();
    }

    private void AnimateStartupUI()
    {
        if (TitleText != null && TitleFinalAnchor != null)
        {
            Vector2 target = TitleFinalAnchor.anchoredPosition;
            TitleText.DOKill();
            TitleText.DOAnchorPos(target, TitleFlyDuration)
                .SetEase(TitleFlyEase)
                .SetDelay(StartupDelay);
        }

        if (RevealButtonRect != null && RevealButtonFinalAnchor != null)
        {
            Vector2 target = RevealButtonFinalAnchor.anchoredPosition;
            RevealButtonRect.DOKill();
            RevealButtonRect.DOAnchorPos(target, RevealButtonFlyDuration)
                .SetEase(RevealButtonFlyEase)
                .SetDelay(StartupDelay + StartupStagger); 
        }
    }

    private void InitializeRevealState()
    {
        currentStudentIndex = 0;
        currentGroupIndex = 0;
        maxStudentsPerGroup = 0;
        hasShownPanels = false;
        panelOriginalPositions = new List<Vector3>();
    }

    private void CacheGridLayoutGroup()
    {
        if (PanelContainer != null)
        {
            gridLayoutGroup = PanelContainer.GetComponent<GridLayoutGroup>();
        }
    }

    private void LoadStudentData()
    {
        string filePath = BuildDataFilePath();
        studentGroups = StudentDataLoader.LoadFromCSV(filePath);

        if (!HasStudentData())
        {
            Debug.LogError("No student data loaded!");
            return;
        }

        CalculateMaxStudentsPerGroup();
        LogLoadedData();
    }

    private string BuildDataFilePath()
    {
        return Path.Combine(Application.streamingAssetsPath, "Data", CsvFileName);
    }

    private void CalculateMaxStudentsPerGroup()
    {
        maxStudentsPerGroup = 0;
        foreach (StudentGroup group in studentGroups)
        {
            if (group.StudentCount > maxStudentsPerGroup)
            {
                maxStudentsPerGroup = group.StudentCount;
            }
        }
    }

    private void LogLoadedData()
    {
        Debug.Log($"Loaded {studentGroups.Count} groups. Max students per group: {maxStudentsPerGroup}");
    }

    private void CreateGroupPanels()
    {
        InitializeGroupPanelsDictionary();

        for (int i = 0; i < studentGroups.Count; i++)
        {
            CreateSingleGroupPanel(studentGroups[i]);
        }

        LogCreatedPanels();
    }

    private void InitializeGroupPanelsDictionary()
    {
        groupPanels = new Dictionary<int, GroupPanelUI>();
    }

    private void CreateSingleGroupPanel(StudentGroup group)
    {
        GameObject panelObj = InstantiatePanelObject();
        GroupPanelUI panelUI = GetPanelUIComponent(panelObj);

        if (panelUI == null)
        {
            Debug.LogError("GroupPanelPrefab must have a GroupPanelUI component!");
            return;
        }

        InitializeAndStorePanelUI(panelUI, group);
    }

    private GameObject InstantiatePanelObject()
    {
        return Instantiate(GroupPanelPrefab, PanelContainer);
    }

    private GroupPanelUI GetPanelUIComponent(GameObject panelObj)
    {
        return panelObj.GetComponent<GroupPanelUI>();
    }

    private void InitializeAndStorePanelUI(GroupPanelUI panelUI, StudentGroup group)
    {
        panelUI.Initialize(group.GroupNumber, group.StudentCount, this);
        groupPanels[group.GroupNumber] = panelUI;
    }

    private void LogCreatedPanels()
    {
        Debug.Log($"Created {groupPanels.Count} group panels");
    }

    private void StorePanelOriginalPositions()
    {
        panelOriginalPositions.Clear();
        
        Canvas.ForceUpdateCanvases();
        
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            if (panel != null)
            {
                RectTransform rectTransform = GetPanelRectTransform(panel);
                if (rectTransform != null)
                {
                    panelOriginalPositions.Add(rectTransform.localPosition);
                }
            }
        }
    }

    private void HideAllPanels()
    {
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            HidePanel(panel);
        }
    }

    private void ShowAllPanels()
    {
        DisableGridLayout();
        
        int index = 0;
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            ShowPanelWithAnimation(panel, index);
            index++;
        }
        
        ScheduleGridLayoutReEnable();
    }

    private void DisableGridLayout()
    {
        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.enabled = false;
        }
    }

    private void EnableGridLayout()
    {
        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.enabled = true;
        }
    }

    private void ScheduleGridLayoutReEnable()
    {
        float totalAnimationTime = CalculateTotalAnimationTime();
        DOVirtual.DelayedCall(totalAnimationTime, EnableGridLayout);
    }

    private float CalculateTotalAnimationTime()
    {
        int panelCount = groupPanels.Count;
        float lastPanelDelay = (panelCount - 1) * PanelAnimationDelay;
        return lastPanelDelay + PanelAnimationDuration;
    }

    private void HidePanel(GroupPanelUI panel)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void ShowPanel(GroupPanelUI panel)
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
        }
    }

    private void ShowPanelWithAnimation(GroupPanelUI panel, int panelIndex)
    {
        if (panel == null) 
            return;

        RectTransform rectTransform = GetPanelRectTransform(panel);
        if (rectTransform == null)
            return;

        ShowPanel(panel);
        SetupPanelForAnimation(rectTransform, panelIndex);
        AnimatePanelEntrance(rectTransform, panelIndex);
    }

    private RectTransform GetPanelRectTransform(GroupPanelUI panel)
    {
        return panel.GetComponent<RectTransform>();
    }

    private void SetupPanelForAnimation(RectTransform rectTransform, int panelIndex)
    {
        Vector3 startPosition = GenerateRandomOffScreenPosition();
        rectTransform.localPosition = startPosition;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, GenerateRandomRotation());
    }

    private Vector3 GenerateRandomOffScreenPosition()
    {
        int direction = Random.Range(0, 4);
        
        switch (direction)
        {
            case 0: // Top
                return new Vector3(Random.Range(-OffScreenDistance, OffScreenDistance), OffScreenDistance, 0f);
            case 1: // Bottom
                return new Vector3(Random.Range(-OffScreenDistance, OffScreenDistance), -OffScreenDistance, 0f);
            case 2: // Left
                return new Vector3(-OffScreenDistance, Random.Range(-OffScreenDistance, OffScreenDistance), 0f);
            case 3: // Right
                return new Vector3(OffScreenDistance, Random.Range(-OffScreenDistance, OffScreenDistance), 0f);
            default:
                return new Vector3(0f, OffScreenDistance, 0f);
        }
    }

    private float GenerateRandomRotation()
    {
        return Random.Range(-PanelRotationAmount, PanelRotationAmount);
    }

    private void AnimatePanelEntrance(RectTransform rectTransform, int panelIndex)
    {
        Vector3 targetPosition = panelOriginalPositions[panelIndex];
        float delay = CalculateAnimationDelay(panelIndex);

        AnimatePosition(rectTransform, targetPosition, delay);
        AnimateRotation(rectTransform, delay);
    }

    private float CalculateAnimationDelay(int panelIndex)
    {
        return panelIndex * PanelAnimationDelay;
    }

    private void AnimatePosition(RectTransform rectTransform, Vector3 targetPosition, float delay)
    {
        rectTransform.DOLocalMove(targetPosition, PanelAnimationDuration)
            .SetDelay(delay)
            .SetEase(PanelAnimationEase);
    }

    private void AnimateRotation(RectTransform rectTransform, float delay)
    {
        rectTransform.DOLocalRotate(Vector3.zero, PanelAnimationDuration)
            .SetDelay(delay)
            .SetEase(PanelAnimationEase);
    }

    private void ResetPanelPositions()
    {
        EnableGridLayout();
        
        int index = 0;
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            if (panel != null && index < panelOriginalPositions.Count)
            {
                RectTransform rectTransform = GetPanelRectTransform(panel);
                if (rectTransform != null)
                {
                    KillPanelTweens(rectTransform);
                    ResetPanelTransform(rectTransform, index);
                }
            }
            index++;
        }
    }

    private void KillPanelTweens(RectTransform rectTransform)
    {
        rectTransform.DOKill();
    }

    private void ResetPanelTransform(RectTransform rectTransform, int index)
    {
        rectTransform.localPosition = panelOriginalPositions[index];
        rectTransform.localRotation = Quaternion.identity;
    }

    private bool HasShownPanels()
    {
        return hasShownPanels;
    }

    private void HandleShowPanels()
    {
        ShowAllPanels();
        MarkPanelsAsShown();
        UpdateRevealButtonText(NextButtonText);
        Debug.Log("Group panels revealed");
    }

    private void MarkPanelsAsShown()
    {
        hasShownPanels = true;
    }

    private void ResetRevealState()
    {
        hasShownPanels = false;
    }

    private void SetupRevealButton()
    {
        if (RevealButton != null)
        {
            RevealButton.onClick.AddListener(RevealNextStudent);
        }
        else
        {
            Debug.LogError("Reveal button not assigned!");
        }
    }

    private void InitializeLogoDisplay()
    {
        if (LogoDisplay != null && HasStudentData())
        {
            LogoDisplay.Initialize(studentGroups.Count, this);
        }
    }

    private bool HasStudentData()
    {
        return studentGroups != null && studentGroups.Count > 0;
    }

    private bool AllStudentsRevealed()
    {
        return currentStudentIndex >= maxStudentsPerGroup;
    }

    private void HandleRevealComplete()
    {
        Debug.Log("All students have been revealed!");
        PlayAllPanelBounceAnimations();
        SetCompleteBackgroundColor();
        DisableRevealButton();
        UpdateRevealButtonText(DoneButtonText);
        EnableAllLogoButtons();
        SpawnFireworksSequence();
    }
    
    private void PlayAllPanelBounceAnimations()
    {
        int index = 0;
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            PlayPanelBounceWithDelay(panel, index);
            index++;
        }
    }

    private void PlayPanelBounceWithDelay(GroupPanelUI panel, int panelIndex)
    {
        if (panel == null) 
            return;

        float delay = CalculateBounceDelay(panelIndex);
        DOVirtual.DelayedCall(delay, () => TriggerPanelBounce(panel));
    }

    private float CalculateBounceDelay(int panelIndex)
    {
        return panelIndex * CompleteBounceDelay;
    }

    private void TriggerPanelBounce(GroupPanelUI panel)
    {
        if (panel != null)
        {
            panel.PlayCompleteBounce();
        }
    }

    private void SetCompleteBackgroundColor()
    {
        if (BackgroundImage != null)
        {
            BackgroundImage.color = CompleteBackgroundColor;
        }
    }

    private void DisableRevealButton()
    {
        if (RevealButton != null)
        {
            RevealButton.interactable = false;
        }
    }

    private void EnableRevealButton()
    {
        if (RevealButton != null)
        {
            RevealButton.interactable = true;
        }
    }

    private void UpdateRevealButtonText(string text)
    {
        if (RevealButton != null)
        {
            TextMeshProUGUI buttonText = RevealButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }
        }
    }

    private void RevealStudentInCurrentGroup()
    {
        StudentGroup currentGroup = GetCurrentGroup();

        if (CanRevealStudentInGroup(currentGroup))
        {
            Student student = GetCurrentStudent(currentGroup);
            AddStudentToPanel(currentGroup.GroupNumber, student);
        }
    }

    private StudentGroup GetCurrentGroup()
    {
        return studentGroups[currentGroupIndex];
    }

    private bool CanRevealStudentInGroup(StudentGroup group)
    {
        return currentStudentIndex < group.StudentCount;
    }

    private Student GetCurrentStudent(StudentGroup group)
    {
        return group.Students[currentStudentIndex];
    }

    private void AddStudentToPanel(int groupNumber, Student student)
    {
        if (groupPanels.ContainsKey(groupNumber))
        {
            groupPanels[groupNumber].AddStudentName(student.Name);
            Debug.Log($"Revealed: {student.Name} in Group {groupNumber}");
            print("shaking");
            screenShake.HitLight();
        }
    }

    private void AdvanceToNextGroup()
    {
        currentGroupIndex++;

        if (ShouldMoveToNextStudentRound())
        {
            MoveToNextStudentRound();
        }
    }

    private bool ShouldMoveToNextStudentRound()
    {
        return currentGroupIndex >= studentGroups.Count;
    }

    private void MoveToNextStudentRound()
    {
        currentGroupIndex = 0;
        currentStudentIndex++;
    }

    private void EnableAllLogoButtons()
    {
        foreach (var panel in groupPanels.Values)
        {
            panel.EnableLogoButton();
        }
    }

    private bool IsValidGroupNumber(int groupNumber)
    {
        return groupPanels.ContainsKey(groupNumber);
    }

    private GroupPanelUI GetGroupPanel(int groupNumber)
    {
        return groupPanels[groupNumber];
    }

    private Sprite GetLogoSpriteForGroup(int groupNumber)
    {
        if (GroupLogoSprites != null && IsValidSpriteIndex(groupNumber))
        {
            return GroupLogoSprites[groupNumber - 1];
        }
        return null;
    }

    private bool IsValidSpriteIndex(int groupNumber)
    {
        return groupNumber - 1 >= 0 && groupNumber - 1 < GroupLogoSprites.Length;
    }

    private void ShowLogoDisplay(int groupNumber, List<string> studentNames, Sprite logoSprite)
    {
        if (LogoDisplay != null)
        {
            LogoDisplay.ShowLogoScreen(groupNumber, studentNames, logoSprite);
        }
    }

    private void DisplayLogoOnPanel(int groupNumber, Sprite logoSprite)
    {
        groupPanels[groupNumber].ShowLogoOnPanel(logoSprite);
        Debug.Log($"Logo revealed and displayed on panel for Group {groupNumber}");
    }

    private void ResetRevealIndices()
    {
        currentStudentIndex = 0;
        currentGroupIndex = 0;
    }

    private void ClearAllPanels()
    {
        foreach (GroupPanelUI panel in groupPanels.Values)
        {
            panel.ClearStudents();
        }
    }
    
    public void SpawnFireworksSequence()
    {
        if (FireworksParticlePrefab == null)
        {
            Debug.LogWarning("Fireworks particle prefab not assigned!");
            return;
        }

        for (int i = 0; i < FireworksCount; i++)
        {
            ScheduleFireworkSpawn(i);
        }
    }

    private void ScheduleFireworkSpawn(int fireworkIndex)
    {
        float delay = CalculateFireworkDelay(fireworkIndex);
        DOVirtual.DelayedCall(delay, () => SpawnSingleFirework());
    }

    private float CalculateFireworkDelay(int fireworkIndex)
    {
        return fireworkIndex * FireworksSpawnDelay;
    }

    public void SpawnSingleFirework()
    {
        Vector3 spawnPosition = GenerateRandomFireworkPosition();
        CreateFireworkAtPosition(spawnPosition);
    }

    private Vector3 GenerateRandomFireworkPosition()
    {
        float sx = Random.Range(FireworksMinX, FireworksMaxX);
        float sy = Random.Range(FireworksMinY, FireworksMaxY);
        
        float zFromCam = Mathf.Abs(0f - Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(new Vector3(sx, sy, zFromCam));
    }

    private void CreateFireworkAtPosition(Vector3 position)
    {
        Transform parent = GetFireworksParent();
        
        GameObject firework = InstantiateFirework(position, parent);
        
        FireworkRandomizer randomizer = firework.GetComponent<FireworkRandomizer>();
        
        float scale;
        if (randomizer != null)
            scale = randomizer.chosenScale;
        else
            scale = 1f;

        GameObject flash = InstantiateFlash(position, parent);
        flash.transform.localScale = Vector3.one * scale;

        ScheduleFireworkDestruction(firework);
    }


    private Transform GetFireworksParent()
    {
        return FireworksParent != null ? FireworksParent : transform;
    }

    private GameObject InstantiateFirework(Vector3 position, Transform parent)
    {
        GameObject firework = Instantiate(FireworksParticlePrefab, position, Quaternion.identity, parent);
        
        if (parent == PanelContainer)
        {
            SetFireworkLocalPosition(firework, position);
        }
        
        return firework;
    }
    
    private GameObject InstantiateFlash(Vector3 position, Transform parent)
    {
        GameObject firework = Instantiate(FlashParticlePrefab, position, Quaternion.identity, parent);
        
        if (parent == PanelContainer)
        {
            SetFireworkLocalPosition(firework, position);
        }
        
        return firework;
    }

    private void SetFireworkLocalPosition(GameObject firework, Vector3 position)
    {
        RectTransform rectTransform = firework.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = position;
        }
    }

    private void ScheduleFireworkDestruction(GameObject firework)
    {
        float lifetime = GetParticleSystemLifetime(firework);
        DOVirtual.DelayedCall(lifetime, () => DestroyFirework(firework));
    }

    private float GetParticleSystemLifetime(GameObject firework)
    {
        ParticleSystem particleSystem = firework.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            return particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
        }
        return 3f; // Default fallback
    }

    private void DestroyFirework(GameObject firework)
    {
        if (firework != null)
        {
            Destroy(firework);
        }
    }
}