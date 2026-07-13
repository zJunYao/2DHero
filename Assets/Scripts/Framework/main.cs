using UnityEngine;

/// <summary>
/// 摇杆面板测试入口。
/// 把该脚本挂到场景中的任意对象上，运行后会显示 JoystickPanel，
/// 并在控制台输出当前摇杆方向，便于验证 UI 输入是否正常。
/// </summary>
public class main : MonoBehaviour
{
    /// <summary>
    /// 是否在开始时显示摇杆面板。
    /// </summary>
    [SerializeField]
    private bool showJoystickOnStart = true;

    /// <summary>
    /// 是否在控制台输出摇杆方向。
    /// </summary>
    [SerializeField]
    private bool logMoveAxis = true;

    private void Start()
    {
#if UNITY_EDITOR
        // 为了方便直接测试编辑器里的预设，这里打开编辑器调试加载模式。
        // 这样 UIMgr 在加载 JoystickPanel 时，会从 Assets/Editor/ArtRes/ui 直接读取。
        ABResMgr.Instance.SetDebugMode(true);
#endif

        // 开启输入系统，让 InputMgr 开始持续分发输入事件。
        InputMgr.Instance.StartOrCloseInputMgr(true);

        if (showJoystickOnStart)
        {
            // 显示通用摇杆面板。
            // 这里放在 System 层，避免被普通 UI 遮挡。
            UIMgr.Instance.ShowPanel<JoystickPanel>(E_UILayer.System);
        }
    }

    private void OnEnable()
    {
        // 监听完整移动轴事件，便于一次性拿到 x、y 两个方向值做测试。
        EventCenter.Instance.AddEventListener<Vector2>(E_EventType.E_Input_MoveAxis, OnMoveAxis);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<Vector2>(E_EventType.E_Input_MoveAxis, OnMoveAxis);
    }

    /// <summary>
    /// 打印摇杆方向，确认输入是否正确进入事件系统。
    /// </summary>
    private void OnMoveAxis(Vector2 axis)
    {
        if (!logMoveAxis)
            return;

        // 只在有明显输入时打印，避免静止时疯狂刷日志。
        if (axis.sqrMagnitude > 0.001f)
            Debug.Log($"Joystick Axis => {axis}");
    }
}
