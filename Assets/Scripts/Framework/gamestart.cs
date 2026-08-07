using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class gamestart : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip clip;
    private GameObject m_obj;
    void Awake()
    {   
        GameObject.DontDestroyOnLoad(this.gameObject);
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"), transform.Find("SceneTrs"));
    }
    // Start is called before the first frame update
    void Start()
    {
        //clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Editor/ArtRes/music/Begin.mp3");
        // audioSource.clip = clip;
        // audioSource.Play();

        // ResourceManager.Instance.AsyncLoadResource("Assets/Editor/ArtRes/music/Begin.mp3", (path, obj, param1, param2, param3) =>
        // {
        //     clip = obj as AudioClip;
        //     audioSource.clip = clip;
        //     audioSource.Play();
        // }, LoadResPriority.RES_HIGHT, false);


        // const string path = "Assets/Editor/ArtRes/music/Begin.mp3";

        // ResourceManager.Instance.PreloadRes(path);

        // uint crc = CRC32.Calculate(path);
        // bool success =
        //     ResourceManager.Instance.AssetDic.TryGetValue(crc, out ResouceItem item) &&
        //     item != null &&
        //     item.m_Obj is AudioClip;

        // if (success)
        // {
        //     Debug.Log($"预加载成功：{path}");
        // }
        // else
        // {
        //     Debug.LogError($"预加载失败：{path}");
        // }

        //-----------------------------------------------------------------------------------------
        //obj = ObjectManager.Instance.InstantiateObject("Assets/_Resource/model/mount/zq67/Prefab/zq67_1.prefab", true, false);
        //ObjectManager.Instance.InstantiateObjectAsync("Assets/_Resource/model/mount/zq67/Prefab/zq67_1.prefab",OnLoadFinish, LoadResPriority.RES_HIGHT,true);
        //ObjectManager.Instance.PreloadGameObject("Assets/_Resource/model/mount/zq67/Prefab/zq67_1.prefab", 20, false);

        //---------------------------------------------------------------------------------------------------------
        UIManager.Instance.Init(transform.Find("UIRoot") as RectTransform, transform.Find("UIRoot/Root") as RectTransform, transform.Find("UICamera").GetComponent<Camera>(), transform.Find("UIRoot/EventSystem").GetComponent<EventSystem>());
        RegiserUI();

        UIManager.Instance.OpenView("GameStartView.prefab");
    }

    void RegiserUI()
    {
        UIManager.Instance.RegisterView<GameStartView>("GameStartView.prefab");
    }

    void OnLoadFinish( string path, Object obj, object param1 = null, object param2 = null , object param3 = null)
    {
        m_obj = obj as GameObject;
        Debug.Log("异步加载完成");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            audioSource.Stop();
            audioSource.clip = null;
            ResourceManager.Instance.ReleaseResouce(clip);      
            clip = null;
        }else if (Input.GetKeyDown(KeyCode.S))
        {
            long startTime = System.DateTime.Now.Ticks;
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Editor/ArtRes/music/Begin.mp3");
            Debug.Log($"预加载耗时" + (System.DateTime.Now.Ticks - startTime));
            audioSource.clip = clip;
            audioSource.Play();
        }else if (Input.GetKeyDown(KeyCode.D))
        {
            long startTime = System.DateTime.Now.Ticks;
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Editor/ArtRes/music/Begin.mp3");
            Debug.Log($"加载耗时" + (System.DateTime.Now.Ticks - startTime));
            audioSource.clip = clip;
            audioSource.Play();
        }else if (Input.GetKeyDown(KeyCode.F))
        {
            ObjectManager.Instance.ReleaseObject(m_obj,0, true);
            m_obj = null;
        }else if (Input.GetKeyDown(KeyCode.G))
        {
            ObjectManager.Instance.ReleaseObject(m_obj);
            m_obj = null;
        }else if (Input.GetKeyDown(KeyCode.H))
        {
           ObjectManager.Instance.InstantiateObjectAsync("Assets/_Resource/model/mount/zq67/Prefab/zq67_1.prefab",OnLoadFinish, LoadResPriority.RES_HIGHT,true);
        }
    }

    private void OnApplicationQuit()
    {
    #if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
        Debug.Log("清空编辑器缓存");
    #endif
    }
}
