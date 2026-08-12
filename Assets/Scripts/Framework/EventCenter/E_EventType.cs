using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件类型枚举。
/// 事件参数类型由具体触发和监听的地方约定。
/// </summary>
public enum E_EventType
{
    /// <summary>
    /// 怪物死亡事件，参数：Monster。
    /// </summary>
    E_Monster_Dead,

    /// <summary>
    /// 玩家获得奖励事件，参数：int。
    /// </summary>
    E_Player_GetReward,

    /// <summary>
    /// 测试事件，参数：无。
    /// </summary>
    E_Test,

    /// <summary>
    /// 场景异步加载进度变化事件，参数：float。
    /// </summary>
    E_SceneLoadChange,

    /// <summary>
    /// 场景开始加载事件，参数：无。
    /// </summary>
    E_SceneLoadEnter,

    /// <summary>
    /// 场景加载并激活完成事件，参数：无。
    /// </summary>
    E_SceneLoadOver,

    /// <summary>
    /// 技能 1 输入事件，参数：无。
    /// </summary>
    E_Input_Skill1,

    /// <summary>
    /// 技能 2 输入事件，参数：无。
    /// </summary>
    E_Input_Skill2,

    /// <summary>
    /// 技能 3 输入事件，参数：无。
    /// </summary>
    E_Input_Skill3,

    /// <summary>
    /// 水平移动轴事件，参数范围通常为 -1 ~ 1。
    /// </summary>
    E_Input_Horizontal,

    /// <summary>
    /// 垂直移动轴事件，参数范围通常为 -1 ~ 1。
    /// </summary>
    E_Input_Vertical,

    /// <summary>
    /// 通用移动轴事件，参数：Vector2。
    /// 主要用于同时接收键盘轴或 UI 虚拟摇杆轴。
    /// </summary>
    E_Input_MoveAxis,
}
