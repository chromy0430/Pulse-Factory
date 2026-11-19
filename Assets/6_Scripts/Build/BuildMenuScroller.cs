using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using PulseFactory.Audio;
using UnityEngine.EventSystems;

public sealed class BuildMenuScroller : MonoBehaviour, IBeginDragHandler
{
    [Required, SerializeField] private UnityEngine.UI.ScrollRect scroll;
    [Required, SerializeField] private UnityEngine.UI.LayoutElement viewportSize;
    [Required, SerializeField] private UnityEngine.UI.Button previous;
    [Required, SerializeField] private UnityEngine.UI.Button next;
    [Required, SerializeField] private RectTransform panel;
    [Required, SerializeField] private UnityEngine.UI.HorizontalLayoutGroup contentLayout;
    [SerializeField, Min(1)] private int maximumVisible = 8;
    [SerializeField, Min(1)] private float itemWidth = 96f;
    [SerializeField, Min(0)] private float spacing = 8f;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;
    public int VisibleCount { get; private set; }
    public int ItemCount { get; private set; }
    public int TargetFirst { get; private set; }
    private Tween movement;
    private bool moving;
    private float previousWidth;
    private float Stride => itemWidth + spacing;
    private int LastFirst => Mathf.Max(0, ItemCount - VisibleCount);

    private void OnEnable()
    {
        GameSounds.Bind(previous);
        GameSounds.Bind(next);
        previous.onClick.AddListener(Previous);
        next.onClick.AddListener(Next);
        scroll.onValueChanged.AddListener(Scrolled);
    }

    private void OnDisable()
    {
        previous.onClick.RemoveListener(Previous);
        next.onClick.RemoveListener(Next);
        scroll.onValueChanged.RemoveListener(Scrolled);
        Stop();
    }

    public void ApplyPresentation(float width, float gap)
    {
        itemWidth = width; spacing = gap; Resize();
    }

    public void Configure(int count)
    {
        ItemCount = count;
        TargetFirst = 0;
        Resize();
        scroll.horizontalNormalizedPosition = 0f;
        RefreshArrows();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || viewportSize == null || panel == null) return;
        var parent = panel.parent as RectTransform;
        if (parent != null && Mathf.Abs(parent.rect.width - previousWidth) > 1f) Resize();
    }

    private void Resize()
    {
        Stop();
        var parent = panel.parent as RectTransform;
        previousWidth = parent != null ? parent.rect.width : 1920f;
        int capacity = Mathf.Clamp(Mathf.FloorToInt((previousWidth - 144f + spacing) / Stride), 1, maximumVisible);
        VisibleCount = Mathf.Min(ItemCount, capacity);
        viewportSize.preferredWidth = Mathf.Max(itemWidth, VisibleCount * Stride - spacing);
        viewportSize.minWidth = viewportSize.preferredWidth;
        contentLayout.spacing = spacing;
        bool overflow = ItemCount > VisibleCount;
        previous.gameObject.SetActive(overflow);
        next.gameObject.SetActive(overflow);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        TargetFirst = Mathf.Clamp(TargetFirst, 0, LastFirst);
        scroll.horizontalNormalizedPosition = LastFirst == 0 ? 0f : (float)TargetFirst / LastFirst;
        RefreshArrows();
    }

    public void Step(int delta)
    {
        if (LastFirst == 0) return;
        if (!moving) TargetFirst = Mathf.RoundToInt(scroll.horizontalNormalizedPosition * LastFirst);
        TargetFirst = Mathf.Clamp(TargetFirst + delta, 0, LastFirst);
        Stop();
        scroll.StopMovement();
        moving = true;
        movement = scroll.DOHorizontalNormalizedPos((float)TargetFirst / LastFirst, duration)
            .SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() => { moving = false; RefreshArrows(); });
        RefreshArrows();
    }

    public void OnBeginDrag(PointerEventData eventData) => Stop();
    private void Previous() => Step(-1);
    private void Next() => Step(1);
    private void Scrolled(Vector2 value) { if (!moving) RefreshArrows(); }

    private void RefreshArrows()
    {
        previous.interactable = LastFirst > 0 && (moving ? TargetFirst > 0 : scroll.horizontalNormalizedPosition > 0.001f);
        next.interactable = LastFirst > 0 && (moving ? TargetFirst < LastFirst : scroll.horizontalNormalizedPosition < 0.999f);
    }

    private void Stop()
    {
        movement?.Kill();
        movement = null;
        moving = false;
    }
}
