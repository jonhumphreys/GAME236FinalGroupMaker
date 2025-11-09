using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GroupPanelUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI GroupTitleText;

    [Tooltip("Deprecated by StudentTextSlots; not used by this script")]
    public TextMeshProUGUI StudentsText;

    [Header("Per-Student Slots (size 4)")]
    public TextMeshProUGUI[] StudentTextSlots = new TextMeshProUGUI[4];

    public Image BackgroundImage;
    public Button LogoButton;
    public Image LogoImage;

    [Header("Color Settings")]
    public Color CompleteColor = Color.white;
    public Color DefaultColor = Color.grey;

    [Header("Animation Settings")]
    public float CompleteBounceScale = 1.2f;
    public float BounceDuration = 0.3f;
    public Ease BounceEase = Ease.OutBack;

    [Header("Per-Name Highlight")]
    public Color HighlightColor = Color.red;
    public float ScaleUp = 1.2f;
    public float ScaleUpTime = 0.12f;
    public float ScaleDownTime = 0.18f;

    private int groupNumber;
    private List<string> studentNames;
    private int totalStudentsInGroup;
    private GroupRevealManager revealManager;
    private Vector3 originalScale;

    private void Awake()
    {
        InitializeStudentList();
        DisableLogoButton();
        HideLogoImage();
        StoreOriginalScale();
        EnsureSlotsAreValid();
    }

    public void Initialize(int groupNum, int totalStudents, GroupRevealManager manager)
    {
        StoreGroupData(groupNum, totalStudents, manager);
        UpdateGroupTitle();
        ClearStudents();
        SetBackgroundColor(DefaultColor);
        SetupLogoButtonListener();
    }

    public void AddStudentName(string studentName)
    {
        studentNames.Add(studentName);
        int slotIndex = FindNextEmptySlotIndex();
        if (slotIndex >= 0)
        {
            SetSlotText(slotIndex, studentName);
            AnimateSlot(slotIndex);
        }
        else
        {
            Debug.LogWarning($"Group {groupNumber}: all 4 slots are full. Name '{studentName}' cannot be displayed.");
        }

        CheckIfComplete();
    }

    public void EnableLogoButton()
    {
        if (CanEnableLogoButton())
        {
            SetLogoButtonInteractable(true);
        }
    }
    
    public void SetGroupTitle(string title, bool pop = true)
    {
        if (GroupTitleText == null) return;

        GroupTitleText.text = title;

        if (pop)
        {
            var rt = GroupTitleText.rectTransform;
            rt.DOKill();
            rt.localScale = Vector3.one;
            DOTween.Sequence()
                .Append(rt.DOScale(1.15f, 0.12f).SetEase(Ease.OutBack))
                .Append(rt.DOScale(1f, 0.14f).SetEase(Ease.OutQuad));
        }
    }

    public void ShowLogoOnPanel(Sprite logoSprite)
    {
        if (IsValidLogoSprite(logoSprite))
        {
            AssignLogoSprite(logoSprite);
            ShowLogoImage();
        }
    }

    public void ClearStudents()
    {
        ClearStudentList();
        ClearAllStudentSlots();
        SetBackgroundColor(DefaultColor);
        DisableLogoButton();
        HideLogoImage();
        ResetScale();
    }

    public void PlayCompleteBounce()
    {
        AnimateBounce();
    }

    public int GroupNumber => groupNumber;
    public int StudentCount => studentNames.Count;
    public List<string> StudentNames => new List<string>(studentNames);

    private void InitializeStudentList()
    {
        studentNames = new List<string>();
    }

    private void StoreOriginalScale()
    {
        originalScale = transform.localScale;
    }

    private void StoreGroupData(int groupNum, int totalStudents, GroupRevealManager manager)
    {
        groupNumber = groupNum;
        totalStudentsInGroup = totalStudents;
        revealManager = manager;
    }

    private void UpdateGroupTitle()
    {
        if (GroupTitleText != null)
        {
            GroupTitleText.text = $"Group {groupNumber}";
        }
    }

    private void CheckIfComplete()
    {
        if (IsGroupComplete())
        {
            MarkGroupAsComplete();
        }
    }

    private bool IsGroupComplete()
    {
        return studentNames.Count >= totalStudentsInGroup;
    }

    private void MarkGroupAsComplete()
    {
        SetBackgroundColor(CompleteColor);
        Debug.Log($"Group {groupNumber} is complete!");
    }

    private void SetBackgroundColor(Color color)
    {
        if (BackgroundImage != null)
        {
            BackgroundImage.color = color;
        }
    }

    private void SetupLogoButtonListener()
    {
        if (LogoButton != null)
        {
            LogoButton.onClick.RemoveAllListeners();
            LogoButton.onClick.AddListener(OnLogoButtonClicked);
        }
    }

    private void OnLogoButtonClicked()
    {
        if (revealManager != null)
        {
            revealManager.OnShowLogoScreen(groupNumber);
        }
    }

    private bool CanEnableLogoButton()
    {
        return LogoButton != null && IsGroupComplete();
    }

    private void SetLogoButtonInteractable(bool interactable)
    {
        if (LogoButton != null)
        {
            LogoButton.interactable = interactable;
        }
    }

    private void DisableLogoButton()
    {
        SetLogoButtonInteractable(false);
    }

    private bool IsValidLogoSprite(Sprite logoSprite)
    {
        return LogoImage != null && logoSprite != null;
    }

    private void AssignLogoSprite(Sprite logoSprite)
    {
        if (LogoImage != null)
        {
            LogoImage.sprite = logoSprite;
        }
    }

    private void ShowLogoImage()
    {
        if (LogoImage != null)
        {
            LogoImage.gameObject.SetActive(true);
        }
    }

    private void HideLogoImage()
    {
        if (LogoImage != null)
        {
            LogoImage.gameObject.SetActive(false);
        }
    }

    private void ClearStudentList()
    {
        studentNames.Clear();
    }

    private void ClearAllStudentSlots()
    {
        if (StudentTextSlots == null) return;
        foreach (var slot in StudentTextSlots)
        {
            if (slot != null) slot.text = "";
        }
    }

    private void AnimateBounce()
    {
        Sequence sequence = DOTween.Sequence();
        Vector3 targetScale = originalScale * CompleteBounceScale;
        sequence.Append(transform.DOScale(targetScale, BounceDuration).SetEase(BounceEase));
        sequence.Append(transform.DOScale(originalScale, BounceDuration).SetEase(BounceEase));
        sequence.Play();
    }

    private void ResetScale()
    {
        transform.DOKill();
        transform.localScale = originalScale;
    }

    private void EnsureSlotsAreValid()
    {
        if (StudentTextSlots == null || StudentTextSlots.Length != 4)
        {
            // Ensure array exists with 4 entries to avoid null checks everywhere
            var newArr = new TextMeshProUGUI[4];
            if (StudentTextSlots != null)
            {
                for (int i = 0; i < Mathf.Min(StudentTextSlots.Length, 4); i++)
                    newArr[i] = StudentTextSlots[i];
            }
            StudentTextSlots = newArr;
        }
    }

    private int FindNextEmptySlotIndex()
    {
        if (StudentTextSlots == null) return -1;
        for (int i = 0; i < StudentTextSlots.Length; i++)
        {
            var slot = StudentTextSlots[i];
            if (slot != null && string.IsNullOrEmpty(slot.text))
                return i;
        }
        return -1;
    }

    private void SetSlotText(int index, string value)
    {
        var slot = GetSlot(index);
        if (slot == null)
        {
            Debug.LogWarning($"Group {groupNumber}: slot {index} is not assigned.");
            return;
        }
        slot.text = value;
    }

    private TextMeshProUGUI GetSlot(int index)
    {
        if (StudentTextSlots == null || index < 0 || index >= StudentTextSlots.Length) return null;
        return StudentTextSlots[index];
    }

    private void AnimateSlot(int index)
    {
        var slot = GetSlot(index);
        if (slot == null) return;

        // Color flash + scale pop
        Color original = slot.color;

        // Scale pop built from two DOScale calls for widest compatibility
        var rt = slot.rectTransform;
        rt.DOKill();
        slot.DOKill();

        DOTween.Sequence()
            .Join(slot.DOColor(HighlightColor, 0.15f).SetLoops(2, LoopType.Yoyo))
            .Join(rt.DOScale(ScaleUp, ScaleUpTime).SetEase(Ease.OutBack))
            .Append(rt.DOScale(1f, ScaleDownTime).SetEase(Ease.OutQuad))
            .OnComplete(() => slot.color = original);
    }
}
