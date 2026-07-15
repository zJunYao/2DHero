using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ResourceTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestLoadAB();
    }
    void TestLoadAB()
    {
        AssetBundle configAB = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/abconfig");
        TextAsset textAsset = configAB.LoadAsset<TextAsset>("AssetBundleConfig"); 
        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig config =(AssetBundleConfig)bf.Deserialize(stream);
        stream.Close();

        string path = "Assets/Editor/ArtRes/ui/BeginPanel.prefab";
        uint crc = CRC32.Calculate(path);
        ABBase abBase = null;
        for (int i = 0; i < config.ABList.Count; i++)
        {
            if (config.ABList[i].Crc == crc)
            {
                abBase = config.ABList[i];
                break;
            }
        }

        if (abBase.ABDependce != null)
        {
            for (int i = 0; i < abBase.ABDependce.Count; i++)
            {
                AssetBundle.LoadFromFile(
                    Application.streamingAssetsPath + "/" + abBase.ABDependce[i]
                );
            }
        }
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" +abBase.ABName);
        GameObject prefab = assetBundle.LoadAsset<GameObject>(abBase.AssetName);
        GameObject obj = GameObject.Instantiate(prefab);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
