using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gamestart : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip clip;
    void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
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


        const string path = "Assets/Editor/ArtRes/music/Begin.mp3";

        ResourceManager.Instance.PreloadRes(path);

        uint crc = CRC32.Calculate(path);
        bool success =
            ResourceManager.Instance.AssetDic.TryGetValue(crc, out ResouceItem item) &&
            item != null &&
            item.m_Obj is AudioClip;

        if (success)
        {
            Debug.Log($"预加载成功：{path}");
        }
        else
        {
            Debug.LogError($"预加载失败：{path}");
        }
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
