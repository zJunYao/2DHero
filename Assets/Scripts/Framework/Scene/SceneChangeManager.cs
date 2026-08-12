using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : BaseManager<SceneChangeManager>
{
    private SceneChangeManager() {}
    //当前场景名称
    public string SceneName { get; set; }
    //场景是否已经加载
    public bool AlreadyLoadScene { get; set; }
    //加载场景进度
    public static int LoadingProgress = 0;


    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="name">场景名</param>
    public void LoadScene(string name)
    {
        LoadingProgress = 0;
        MonoMgr.Instance.StartCoroutine(LoadSceneAsyn(name));
    }

    private IEnumerator LoadSceneAsyn(string name)
    {
        EventCenter.Instance.EventTrigger(E_EventType.E_SceneLoadEnter);
        ClearCache();
        AlreadyLoadScene = false;
         // 后续场景切换逻辑
        AsyncOperation unLoadScene = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Single);
        while (unLoadScene != null && !unLoadScene.isDone)
        {
            yield return new WaitForEndOfFrame();
        }
        LoadingProgress = 0;
        int targetProgress = 0;
        //异步加载传入的目标场景
        AsyncOperation asyncScene = SceneManager.LoadSceneAsync(name);
        if (asyncScene != null && !asyncScene.isDone)
        {
            asyncScene.allowSceneActivation = false;
            while (asyncScene.progress < 0.9f)
            {
                targetProgress = (int)(asyncScene.progress * 100);
                yield return new WaitForEndOfFrame();
                //过渡动画
                while (LoadingProgress < targetProgress)
                {
                    ++LoadingProgress;
                    yield return new WaitForEndOfFrame();
                }
            }
            //剩余 10%
            targetProgress = 100;
            while (LoadingProgress < targetProgress)
            {
                ++LoadingProgress;
                yield return new WaitForEndOfFrame();
            }

            asyncScene.allowSceneActivation = true;
            while (!asyncScene.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        SceneName = name;
        AlreadyLoadScene = true;
        EventCenter.Instance.EventTrigger(E_EventType.E_SceneLoadOver);
        yield return null;
    }

    /// <summary>
    /// 清理缓存 切场景
    /// </summary>
    private void ClearCache()
    {
        ObjectManager.Instance.ClearCache();
        ResourceManager.Instance.ClearCache();
    }

    /// <summary>
    /// 设置场景配置
    /// </summary>
    /// <param name="name"></param>
    void SetSceneSetting(string name)
    {
        // 根据场景名从配置表读取并应用场景配置
    }
}
