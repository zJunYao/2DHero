using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 通用虚拟摇杆面板。
/// 该面板只负责把 UI 的拖拽行为转换成移动方向，
/// 再交给 InputMgr 统一向外分发，不直接操作角色对象。
/// </summary>
public class JoystickPanel : BasePanel
{
    /// <summary>
    /// 摇杆最大拖拽半径。
    /// 摇杆头超出这个半径后会被限制在圆周上。
    /// </summary>
    [SerializeField]
    private float radius = 120f;

    /// <summary>
    /// 死区范围。
    /// 小幅度抖动不会产生移动输入，避免角色轻微乱动。
    /// </summary>
    [SerializeField]
    private float deadZone = 0.15f;

    /// <summary>
    /// 是否采用“按下处出现底盘”的跟手模式。
    /// 为 false 时，底盘会停留在预设摆放的位置。
    /// </summary>
    [SerializeField]
    private bool followPointer = true;

    /// <summary>
    /// 空闲时是否隐藏摇杆底盘背景。
    /// </summary>
    [SerializeField]
    private bool hideBackgroundWhenIdle = true;

    /// <summary>
    /// 摇杆底盘。
    /// </summary>
    private RectTransform bg;

    /// <summary>
    /// 摇杆头，用于表现当前方向。
    /// </summary>
    private RectTransform handle;

    /// <summary>
    /// 透明触摸区域。
    /// 负责接收按下、拖拽、抬起事件。
    /// </summary>
    private Image touchArea;

    /// <summary>
    /// Canvas 的 RectTransform。
    /// 用于把屏幕坐标转换成 UI 本地坐标。
    /// </summary>
    private RectTransform canvasRect;

    /// <summary>
    /// 当前操控摇杆的手指 ID。
    /// 用于避免多指触摸时相互干扰。
    /// </summary>
    private int pointerId = int.MinValue;

    /// <summary>
    /// 初始化控件引用并注册 UI 事件。
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        touchArea = GetControl<Image>("TouchArea");
        Image bgImage = GetControl<Image>("Bg");
        Image handleImage = GetControl<Image>("JoystickHandle");

        if (touchArea == null || bgImage == null || handleImage == null)
            return;

        bg = bgImage.rectTransform;
        handle = handleImage.rectTransform;
        canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;

        UIMgr.AddCustomEventListener(touchArea, EventTriggerType.PointerDown, OnPointerDown);
        UIMgr.AddCustomEventListener(touchArea, EventTriggerType.Drag, OnDrag);
        UIMgr.AddCustomEventListener(touchArea, EventTriggerType.PointerUp, OnPointerUp);
        UIMgr.AddCustomEventListener(touchArea, EventTriggerType.EndDrag, OnPointerUp);

        ResetJoystick();
    }

    /// <summary>
    /// 面板失活时清空输入，避免残留方向继续移动。
    /// </summary>
    private void OnDisable()
    {
        ResetJoystick();
    }

    /// <summary>
    /// 面板显示时复位摇杆。
    /// </summary>
    public override void ShowMe()
    {
        ResetJoystick();
    }

    /// <summary>
    /// 面板隐藏时复位摇杆。
    /// </summary>
    public override void HideMe()
    {
        ResetJoystick();
    }

    /// <summary>
    /// 按下时记录手指 ID，并决定底盘是否跟随按下位置移动。
    /// </summary>
    private void OnPointerDown(BaseEventData eventData)
    {
        PointerEventData data = eventData as PointerEventData;
        if (data == null || bg == null)
            return;

        pointerId = data.pointerId;
        if (followPointer && canvasRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                                                                    data.position,
                                                                    data.pressEventCamera,
                                                                    out Vector2 pos);
            bg.anchoredPosition = pos;
        }

        bg.gameObject.SetActive(true);
        UpdateAxis(data);
    }

    /// <summary>
    /// 拖拽时持续刷新移动方向。
    /// </summary>
    private void OnDrag(BaseEventData eventData)
    {
        PointerEventData data = eventData as PointerEventData;
        if (data == null || data.pointerId != pointerId || bg == null)
            return;

        UpdateAxis(data);
    }

    /// <summary>
    /// 抬起时仅处理当前这根操作中的手指，然后复位摇杆。
    /// </summary>
    private void OnPointerUp(BaseEventData eventData)
    {
        PointerEventData data = eventData as PointerEventData;
        if (data == null || data.pointerId != pointerId)
            return;

        ResetJoystick();
    }

    /// <summary>
    /// 计算摇杆本地坐标，限制最大半径，再转换成归一化方向。
    /// </summary>
    private void UpdateAxis(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bg,
                                                                data.position,
                                                                data.pressEventCamera,
                                                                out Vector2 localPoint);

        Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, radius);
        handle.anchoredPosition = clampedPoint;

        Vector2 axis = clampedPoint / radius;
        if (axis.magnitude < deadZone)
            axis = Vector2.zero;

        InputMgr.Instance.SetVirtualAxis(axis);
    }

    /// <summary>
    /// 重置摇杆表现并清空 UI 虚拟输入。
    /// </summary>
    private void ResetJoystick()
    {
        pointerId = int.MinValue;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (bg != null)
            bg.gameObject.SetActive(!hideBackgroundWhenIdle);

        InputMgr.Instance.ClearVirtualAxis();
    }
}
