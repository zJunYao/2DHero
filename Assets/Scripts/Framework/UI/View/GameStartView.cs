using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStartView : BaseView
{
    private GameStartPanel m_Panel;

    public override void Awake(params object[] paralist)
    {
        m_Panel = GameObject.GetComponent<GameStartPanel>();
        AddButtonClickListener(m_Panel.btn_start, onClickStart);
        AddButtonClickListener(m_Panel.btn_loading, onClickLoading);
        AddButtonClickListener(m_Panel.btn_exit, onClickExit);
    }

    void onClickStart()
    {
        Debug.Log("点击开始游戏");
    }

    void onClickLoading()
    {
        Debug.Log("点击加载游戏");
    }

    void onClickExit()
    {
        Debug.Log("点击退出游戏");
    }
    
}
