using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 输入管理器。
/// 负责统一处理键盘、鼠标以及 UI 虚拟摇杆输入，
/// 最终通过事件中心把输入结果广播出去。
/// </summary>
public class InputMgr : BaseManager<InputMgr>
{
    private Dictionary<E_EventType, InputInfo> inputDic = new Dictionary<E_EventType, InputInfo>();

    // 当前遍历到的输入信息缓存。
    private InputInfo nowInputInfo;

    // 是否开启输入检测。
    private bool isStart;

    // 用于“录入按键”时，把下一次按下的输入信息回传给外部。
    private UnityAction<InputInfo> getInputInfoCallBack;

    // 是否已经开始等待下一次输入检测。
    private bool isBeginCheckInput = false;

    // UI 虚拟摇杆当前提供的移动方向。
    private Vector2 uiAxis;

    // 当前是否优先采用 UI 虚拟摇杆作为移动输入来源。
    private bool useUIAxis;

    private InputMgr()
    {
        // 挂到公共 Mono 更新器上，这样普通单例也能收到逐帧更新。
        MonoMgr.Instance.AddUpdateListener(InputUpdate);
    }

    /// <summary>
    /// 开启或关闭输入管理器的检测功能。
    /// </summary>
    public void StartOrCloseInputMgr(bool isStart)
    {
        this.isStart = isStart;
    }

    /// <summary>
    /// 配置某个行为对应的键盘输入信息。
    /// </summary>
    public void ChangeKeyboardInfo(E_EventType eventType, KeyCode key, InputInfo.E_InputType inputType)
    {
        if (!inputDic.ContainsKey(eventType))
        {
            inputDic.Add(eventType, new InputInfo(inputType, key));
        }
        else
        {
            inputDic[eventType].keyOrMouse = InputInfo.E_KeyOrMouse.Key;
            inputDic[eventType].key = key;
            inputDic[eventType].inputType = inputType;
        }
    }

    /// <summary>
    /// 配置某个行为对应的鼠标输入信息。
    /// </summary>
    public void ChangeMouseInfo(E_EventType eventType, int mouseID, InputInfo.E_InputType inputType)
    {
        if (!inputDic.ContainsKey(eventType))
        {
            inputDic.Add(eventType, new InputInfo(inputType, mouseID));
        }
        else
        {
            inputDic[eventType].keyOrMouse = InputInfo.E_KeyOrMouse.Mouse;
            inputDic[eventType].mouseID = mouseID;
            inputDic[eventType].inputType = inputType;
        }
    }

    /// <summary>
    /// 移除指定行为的输入配置。
    /// </summary>
    public void RemoveInputInfo(E_EventType eventType)
    {
        if (inputDic.ContainsKey(eventType))
            inputDic.Remove(eventType);
    }

    /// <summary>
    /// 获取下一次被按下的输入信息，常用于按键重绑定。
    /// </summary>
    public void GetInputInfo(UnityAction<InputInfo> callBack)
    {
        getInputInfoCallBack = callBack;
        MonoMgr.Instance.StartCoroutine(BeginCheckInput());
    }

    /// <summary>
    /// 由 UI 虚拟摇杆写入移动轴。
    /// 一旦有有效输入，就优先覆盖键盘 Horizontal/Vertical。
    /// </summary>
    public void SetVirtualAxis(Vector2 axis)
    {
        uiAxis = Vector2.ClampMagnitude(axis, 1f);
        useUIAxis = uiAxis.sqrMagnitude > 0.0001f;
    }

    /// <summary>
    /// 清空 UI 虚拟摇杆输入，恢复为原本的键盘轴输入。
    /// </summary>
    public void ClearVirtualAxis()
    {
        uiAxis = Vector2.zero;
        useUIAxis = false;
    }

    private IEnumerator BeginCheckInput()
    {
        // 等一帧，避免当前触发逻辑和输入录制流程互相影响。
        yield return 0;
        isBeginCheckInput = true;
    }

    /// <summary>
    /// 统一输入检测入口。
    /// 包含按键监听、鼠标监听、录入输入以及移动轴事件分发。
    /// </summary>
    private void InputUpdate()
    {
        if (isBeginCheckInput)
        {
            if (Input.anyKeyDown)
            {
                InputInfo inputInfo = null;

                Array keyCodes = Enum.GetValues(typeof(KeyCode));
                foreach (KeyCode inputKey in keyCodes)
                {
                    if (Input.GetKeyDown(inputKey))
                    {
                        inputInfo = new InputInfo(InputInfo.E_InputType.Down, inputKey);
                        break;
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        inputInfo = new InputInfo(InputInfo.E_InputType.Down, i);
                        break;
                    }
                }

                getInputInfoCallBack.Invoke(inputInfo);
                getInputInfoCallBack = null;
                isBeginCheckInput = false;
            }
        }

        if (!isStart)
            return;

        foreach (E_EventType eventType in inputDic.Keys)
        {
            nowInputInfo = inputDic[eventType];
            if (nowInputInfo.keyOrMouse == InputInfo.E_KeyOrMouse.Key)
            {
                switch (nowInputInfo.inputType)
                {
                    case InputInfo.E_InputType.Down:
                        if (Input.GetKeyDown(nowInputInfo.key))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    case InputInfo.E_InputType.Up:
                        if (Input.GetKeyUp(nowInputInfo.key))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    case InputInfo.E_InputType.Always:
                        if (Input.GetKey(nowInputInfo.key))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (nowInputInfo.inputType)
                {
                    case InputInfo.E_InputType.Down:
                        if (Input.GetMouseButtonDown(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    case InputInfo.E_InputType.Up:
                        if (Input.GetMouseButtonUp(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    case InputInfo.E_InputType.Always:
                        if (Input.GetMouseButton(nowInputInfo.mouseID))
                            EventCenter.Instance.EventTrigger(eventType);
                        break;
                    default:
                        break;
                }
            }
        }

        // 如果 UI 摇杆正在提供方向，则优先使用 UI 轴；
        // 否则回退到原本的键盘 Horizontal/Vertical 轴。
        Vector2 moveAxis = useUIAxis
            ? uiAxis
            : new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        EventCenter.Instance.EventTrigger(E_EventType.E_Input_Horizontal, moveAxis.x);
        EventCenter.Instance.EventTrigger(E_EventType.E_Input_Vertical, moveAxis.y);

        // 额外提供一个完整 Vector2 事件，方便角色移动逻辑直接消费。
        EventCenter.Instance.EventTrigger(E_EventType.E_Input_MoveAxis, moveAxis);
    }
}
