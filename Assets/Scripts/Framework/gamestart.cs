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
        ResourceManager.Instance.AsyncLoadResource("Assets/Editor/ArtRes/music/Begin.mp3", (path, obj, param1, param2, param3) =>
        {
            clip = obj as AudioClip;
            audioSource.clip = clip;
            audioSource.Play();
        }, LoadResPriority.RES_HIGHT, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            audioSource.Stop();
            audioSource.clip = null;
            ResourceManager.Instance.ReleaseResouce(clip,true);      
            clip = null;
        }
    }
}
