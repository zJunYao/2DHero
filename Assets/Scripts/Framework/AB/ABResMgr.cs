using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 资源加载统一入口。
/// 开发阶段可切到编辑器直读模式，发布后走 AssetBundle 加载。
/// </summary>
public class ABResMgr : BaseManager<ABResMgr>
{
    // 为 true 时，在编辑器里直接从 Assets/Editor/ArtRes 加载资源。
    private bool isDebug = false;

    private ABResMgr() { }

    /// <summary>
    /// 设置是否启用编辑器调试加载模式。
    /// 仅在 Unity Editor 下生效，方便在不打 AB 包时测试 UI 预设。
    /// </summary>
    public void SetDebugMode(bool isDebug)
    {
#if UNITY_EDITOR
        this.isDebug = isDebug;
#endif
    }

    /// <summary>
    /// 异步加载资源。
    /// 编辑器调试模式下直接读取编辑器资源，否则走 AB 包加载流程。
    /// </summary>
    public void LoadResAsync<T>(string abName, string resName, UnityAction<T> callBack, bool isSync = false) where T : Object
    {
#if UNITY_EDITOR
        if (isDebug)
        {
            T res = EditorResMgr.Instance.LoadEditorRes<T>($"{abName}/{resName}");
            callBack?.Invoke(res as T);
        }
        else
        {
            ABMgr.Instance.LoadResAsync<T>(abName, resName, callBack, isSync);
        }
#else
        ABMgr.Instance.LoadResAsync<T>(abName, resName, callBack, isSync);
#endif
    }
}
