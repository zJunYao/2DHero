using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum UIMsgID
{
    None = 0,
}

public class UIManager : BaseManager<UIManager>
{
    private UIManager() { }
    //UI节点
    public RectTransform m_UiRoot;
    //窗口节点
    private RectTransform m_Root;
    //UI相机
    private Camera m_UICamera;
    //EventSystem 节点
    private EventSystem m_EventSystem;
    //分辨率
    private float m_CanvasRate = 0;
    //View 地址
    private const string UIPREFABPATH = "Assets/_Resource/UI/UIPanel/";
    //注册的字典
    private Dictionary<string, System.Type> m_RegisterDic = new Dictionary<string, System.Type>();
    //缓存已打开窗口
    private Dictionary<string, BaseView> m_ViewDic = new Dictionary<string, BaseView>();
    // 打开的窗口列表
    private List<BaseView> m_ViewList = new List<BaseView>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="uiRoot"></param>
    /// <param name="root"></param>
    /// <param name="uiCamera"></param>
    /// <param name="eventSystem"></param>
    public void Init(RectTransform uiRoot, RectTransform viewRoot, Camera uiCamera, EventSystem eventSystem)
    {
        m_UiRoot = uiRoot;
        m_Root = viewRoot;
        m_UICamera = uiCamera;
        m_EventSystem = eventSystem;
        //获取分辨率
        m_CanvasRate = Screen.height / (m_UICamera.orthographicSize * 2);
    }

    /// <summary>
    /// 整体显隐 UI 只操作根节点
    /// </summary>
    public void ShowOrHideUI(bool show)
    {
        if (m_UiRoot != null)
        {
            m_UiRoot.gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// 更新事件
    /// </summary>
    public void OnUpdate()
    {
        for (int i = 0; i < m_ViewList.Count; i++)
        {
            if (m_ViewList[i] != null)
            {
                m_ViewList[i].OnUpdate();
            }
        }
    }

    /// <summary>
    /// 使用 EventSystem 设置默认选中对象
    /// </summary>
    /// <param name="obj"></param>
    public void SetNormalSelectObj(GameObject obj)
    {
        if (m_EventSystem == null)
        {
            m_EventSystem = EventSystem.current;
        }
        m_EventSystem.firstSelectedGameObject = obj;
    }

    /// <summary>
    /// 注册窗口
    /// </summary>
    /// <param name="name">窗口名称</param>
    /// <param name="type">窗口类型</param>
    public void RegisterView<T>(string name) where T : BaseView
    {
        m_RegisterDic[name] = typeof(T);
    }

    /// <summary>
    /// 发送消息给窗口
    /// </summary>
    /// <param name="name">窗口名称</param>
    /// <param name="msgID">消息ID</param>
    /// <param name="paralist">参数列表</param>
    /// <returns></returns>
    public bool SendMessageToWnd(string name, UIMsgID msgID = 0, params object[] paralist)
    {
        BaseView view = FindWndByName<BaseView>(name);
    
        if (view != null)
        {
            return view.OnMessage(msgID, paralist);
        }
    
        return false;
    }

    /// <summary>
    /// 通过名称查找窗口
    /// </summary>
    /// <typeparam name="T">窗口类型</typeparam>
    /// <param name="name">窗口名称</param>
    /// <returns></returns>
    public T FindWndByName<T>(string name) where T : BaseView
    {
        BaseView view = null;
    
        if (m_ViewDic.TryGetValue(name, out view))
        {
            return (T)view;
        }
    
        return null;
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    /// <param name="viewName">窗口名称</param>
    /// <param name="is_show_top">是否将窗口置于最上层</param>
    /// <param name="paralist">参数列表</param>
    /// <returns></returns>
    public BaseView OpenView(string viewName, bool is_show_top = true, params object[] paralist)
    {
        BaseView view = FindWndByName<BaseView>(viewName);
        if (view == null)
        {
            System.Type type = null;
            if (m_RegisterDic.TryGetValue(viewName, out type))
            {
                view = System.Activator.CreateInstance(type) as BaseView;
            }
            else
            {
                Debug.LogError("找不到窗口对应的脚本，窗口名是：" + viewName);
            }

            GameObject obj = ObjectManager.Instance.InstantiateObject(UIPREFABPATH + viewName,false,false); 
            if (obj == null)
            {
                Debug.Log("创建窗口Prefab失败：" + viewName);
                return null;
            }
            if (!m_ViewDic.ContainsKey(viewName))
            {
                m_ViewList.Add(view);
                m_ViewDic.Add(viewName, view);
            }
            
            view.GameObject = obj;
            view.Transform = obj.transform;
            view.Name = viewName;
            view.Awake(paralist);
            obj.transform.SetParent(m_Root, false);

            //置最上层
            if (is_show_top)
            {
                view.Transform.SetAsLastSibling();
            }
            //显示
            view.OnShow(paralist);
        }
        else
        {
            ShowView(view, is_show_top, paralist);
        }
        return view;
    }

    /// <summary>
    /// 更据窗口名字显示窗口
    /// </summary>
    /// <param name="name"></param>
    /// <param name="is_show_top"></param>
    /// <param name="paralist"></param>
    public void ShowView(string name, bool is_show_top = true, params object[] paralist)
    {
        BaseView view = FindWndByName<BaseView>(name);
        ShowView(view, is_show_top, paralist);
    }

    /// <summary>
    /// 更据窗口对象显示窗口
    /// </summary>
    /// <param name="view"></param>
    /// <param name="is_show_top"></param>
    /// <param name="paralist"></param>
    public void ShowView(BaseView view, bool is_show_top = true, params object[] paralist)
    {
        if (view != null)
        {
            if (view.GameObject != null && !view.GameObject.activeSelf)
            {
                view.GameObject.SetActive(true);
            }
    
            if (is_show_top)
            {
                view.Transform.SetAsLastSibling();
            }
    
            view.OnShow(paralist);
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    /// <param name="name">名字</param>
    /// <param name="destory">是否缓存</param>
    public void CloseView(string name, bool destory = false)
    {
        BaseView view = FindWndByName<BaseView>(name);
        CloseView(view, destory);
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    /// <param name="view">页面</param>
    /// <param name="destory">是否缓存</param>
    public void CloseView(BaseView view, bool destory = false)
    {
        if (view != null)
        {
            view.OnDisable();
            view.OnClose();
    
            if (m_ViewDic.ContainsKey(view.Name))
            {
                m_ViewDic.Remove(view.Name);
                m_ViewList.Remove(view);
            }
    
            if (destory)
            {
                ObjectManager.Instance.ReleaseObject(view.GameObject, 0, true);
            }
            else
            {
                ObjectManager.Instance.ReleaseObject(view.GameObject, recycleParent: false);
            }
    
            view.GameObject = null;
            view = null;
        }
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    public void HideWnd(string name)
    {
        BaseView view = FindWndByName<BaseView>(name);
        HideWnd(view);
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    /// <param name="view"></param>
    public void HideWnd(BaseView view)
    {
        if (view != null)
        {
            if (view.GameObject != null && view.GameObject.activeSelf)
            {
                view.GameObject.SetActive(false);
            }
    
            view.OnDisable();
        }
    }

    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    public void CloseAllWnd()
    {
        for (int i = m_ViewList.Count - 1; i >= 0; i--)
        {
            CloseView(m_ViewList[i]);
        }
    }

    /// <summary>
    /// 切换到唯一窗口
    /// </summary>
    public void SwitchStateByName(string name, bool is_show_top = true, params object[] paralist)
    {
        CloseAllWnd();
        OpenView(name, is_show_top, paralist);
    }
    
}
