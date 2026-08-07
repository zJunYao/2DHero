using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseView
{
    //引用GameObject
    public GameObject GameObject { get; set; }
    //引用Transform
    public Transform Transform { get; set; }
    public string Name { get; set; }
    //所有Button
    protected List<Button> m_AllButton = new List<Button>();
    //所有Toggle
    protected List<Toggle> m_AllToggle = new List<Toggle>();

    public virtual void Awake(params object[] paralist) { }
 
    public virtual void OnShow(params object[] paralist) { }

    public virtual bool OnMessage(UIMsgID msgID, params object[] paralist)
    {
        return true;
    }

    public virtual void OnDisable() { }
    
    public virtual void OnUpdate() { }
    
    public virtual void OnClose()
    {
        RemoveAllButtonListener();
        RemoveAllToggleListener();
    
        m_AllButton.Clear();
        m_AllToggle.Clear();
    }

    /// <summary>
    /// 移除所有的Button事件
    /// </summary>
    public void RemoveAllButtonListener()
    {
        foreach (Button btn in m_AllButton)
        {
            btn.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 移除所有的Toggle事件
    /// </summary>
    public void RemoveAllToggleListener()
    {
        foreach (Toggle toggle in m_AllToggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
    }

    /// <summary>
    /// 添加Button点击事件
    /// </summary>
    /// <param name="btn"></param>
    /// <param name="action"></param>
    public void AddButtonClickListener(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null)
        {
            if (!m_AllButton.Contains(btn))
            {
                m_AllButton.Add(btn);
            }
    
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            btn.onClick.AddListener(BtnPlaySound);
        }
    }

    /// <summary>
    /// 播放button声音
    /// </summary>
    void BtnPlaySound()
    {
        
    }

    /// <summary>
    /// 添加Toggle点击事件
    /// </summary>
    /// <param name="toggle"></param>
    /// <param name="action"></param>
    public void AddToggleClickListener(Toggle toggle, UnityEngine.Events.UnityAction<bool> action)
    {
        if (toggle != null)
        {
            if (!m_AllToggle.Contains(toggle))
            {
                m_AllToggle.Add(toggle);
            }
    
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
            toggle.onValueChanged.AddListener(TogglePlaySound);
        }
    }


    /// <summary>
    /// 播放Toggle声音
    /// </summary>
    void TogglePlaySound(bool isOn)
    {
        
    }

    /// <summary>
    /// 改变Image的Sprite
    /// </summary>
    /// <param name="path">图片路径</param>
    /// <param name="image">Image组件</param>
    /// <param name="setNativeSize">是否设置Image的NativeSize</param>
    /// <returns></returns>
    public bool ChangeImageSprite(string path, Image image, bool setNativeSize = false)
    {
        if (image == null)
            return false;
    
        Sprite sp = ResourceManager.Instance.LoadResource<Sprite>(path);
        if (sp != null)
        {
            if (image.sprite != null)
                image.sprite = null;
    
            image.sprite = sp;
    
            if (setNativeSize)
            {
                image.SetNativeSize();
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 异步加载图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="image"></param>
    /// <param name="setNativeSize"></param>
    public void ChangImageSpriteAsync(string path, Image image, bool setNativeSize = false)
    {
        if (image == null)
            return;
    
        ResourceManager.Instance.AsyncLoadResource(path, OnLoadSpriteFinish, LoadResPriority.RES_MIDDLE, image, setNativeSize);
    }
    
    void OnLoadSpriteFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        if (obj != null)
        {
            Sprite sp = obj as Sprite;
            Image image = param1 as Image;
            bool setNativeSize = (bool)param2;
    
            if (image.sprite != null)
                image.sprite = null;
    
            image.sprite = sp;
    
            if (setNativeSize)
            {
                image.SetNativeSize();
            }
        }
    }
}
