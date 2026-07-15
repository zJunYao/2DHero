using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEditor; // 添加这一行
using UnityEngine;

public class BundleEditor
{
    //打包目标路径
    private static string m_BundleTargetPath = Application.streamingAssetsPath;
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";
    //存储所有文件夹下的AB包 key：AB包名 value：AB包路径
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤出所有需要打包进AB包中的预设体
    private static List<string> m_AllFilrAB = new List<string>();
    //单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();
    //存储所有的有效路径
    private static List<string> m_ConfigFil = new List<string>();
    [MenuItem("Tools/打包")]
    public static void BuildAB()
    {
        m_AllFileDir.Clear();
        m_AllFilrAB.Clear();
        m_AllPrefabDir.Clear();
        m_ConfigFil.Clear();

        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        foreach (ABConfig.FileDirABName fileDir in abConfig.m_FileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包名重复,请检查ABConfig：" + fileDir.ABName);
                continue;
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFilrAB.Add(fileDir.Path);
                m_ConfigFil.Add(fileDir.Path);
            }
        }

        //找到m_AllPrefabPath下所有的预设体
        if(abConfig.m_AllPrefabPath.Count > 0)
        {
            string[] allStr = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
            for(int i = 0; i < allStr.Length; i++)
            {
                //通过guid获取预设体路径
                string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
                EditorUtility.DisplayProgressBar("查找预设体", "正在查找：" + path, (float)i / allStr.Length);
                m_ConfigFil.Add(path);
                //判断是否需要打包进AB包
                if (!ContainAllFileAB(path))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    string [] allDepends = AssetDatabase.GetDependencies(path);
                    List<string> allDependsPath = new List<string>();
                    for(int j = 0; j < allDepends.Length; j++)
                    {
                        Debug.Log("依赖：" + allDepends[j]);
                        if(!ContainAllFileAB(allDepends[j]) && !allDepends[j].EndsWith(".cs"))
                        {
                            m_AllFilrAB.Add(allDepends[j]);
                            allDependsPath.Add(allDepends[j]);
                        }
                    }
                    if(m_AllPrefabDir.ContainsKey(prefab.name))
                    {
                        Debug.LogError("存在相同名字的prefab" + prefab.name);
                    }
                    else
                    {
                        m_AllPrefabDir.Add(prefab.name, allDependsPath);
                    }
                }
            }   
        }
        EditorUtility.ClearProgressBar();

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }
        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BuildAssetBundle();


        string[] oldABName = AssetDatabase.GetAllAssetBundleNames();
        for(int i = 0; i < oldABName.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABName[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字：" + oldABName[i], (float)i / oldABName.Length);
        }
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// 检查指定的资源路径是否已经包含在需要打包到AssetBundle的资源路径列表中
    /// </summary>
    /// <param name="path">需要检查的资源路径</param>
    /// <returns>如果路径已存在于打包列表中返回true，否则返回false</returns>
    static bool ContainAllFileAB(string path)
    {
        for(int i = 0; i < m_AllFilrAB.Count; i++)
        {
            if (path.Contains(m_AllFilrAB[i]) && (path.Replace(m_AllFilrAB[i],"")[0] != '/') || path == m_AllFilrAB[i])
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 检查指定的AssetBundle名称是否存在于依赖列表中
    /// </summary>
    /// <param name="abName">需要检查的AssetBundle名称</param>
    /// <param name="strs">依赖AssetBundle名称列表</param>
    /// <returns>如果依赖列表中包含指定的AssetBundle名称返回true，否则返回false</returns>
    static bool ContainABName(string abName,string[] strs)
    {
        for(int i = 0; i < strs.Length; i++)
        {
            if(abName == strs[i])
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 验证资源路径是否在配置的有效路径列表中
    /// </summary>
    /// <param name="path">要验证的资源路径</param>
    /// <returns>如果路径包含在配置的有效路径中则返回true，否则返回false</returns>
    static bool VaildPath(string path)
    {
        for(int i = 0; i < m_ConfigFil.Count; i++)
        {
            if(path.Contains(m_ConfigFil[i]))
            {
                //预设 和文件名下的资源
                return true;
            }
        }
        return false;
    }

    static void SetABName(string name ,string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if(assetImporter == null)
        {
            Debug.LogError("路径不存在" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
            assetImporter.SaveAndReimport();   
        }
    }

    static void SetABName(string name ,List<string> paths)
    {
        for(int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    static void BuildAssetBundle()
    {
        string[] allABName = AssetDatabase.GetAllAssetBundleNames();
        //key 为全路径 value 为AB包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for(int i = 0; i < allABName.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allABName[i]);
            for(int j = 0; j < allBundlePath.Length; j++)
            {
                if(allBundlePath[j].EndsWith(".cs"))
                {
                    continue;
                }
                Debug.Log("此AB包：" + allABName[i] + " 包含资源：" + allBundlePath[j]);
                if(VaildPath(allBundlePath[j]))
                {
                     resPathDic.Add(allBundlePath[j], allABName[i]);
                }
            }
        }

        //删除多余的AB包
        DeleteAB();
        //生成AB包配置表
        WriteData(resPathDic);


        BuildPipeline.BuildAssetBundles(m_BundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }

    /// <summary>
    /// 清理不再需要的AssetBundle文件
    /// </summary>
    /// <remarks>
    /// 该函数用于删除打包目标路径下不再是有效AB包的文件，保持AB包目录的整洁
    /// 只保留当前项目中实际使用的AB包文件和.meta文件
    /// </remarks>
    static void DeleteAB()
    {
        string [] allBundlePath = AssetDatabase.GetAllAssetBundleNames();
        //删除AB包
        DirectoryInfo diretion = new DirectoryInfo(m_BundleTargetPath);
        FileInfo[] files = diretion.GetFiles("*", SearchOption.AllDirectories);
        for(int i = 0; i < files.Length; i++)
        {
            if(ContainABName(files[i].Name, allBundlePath) || files[i].Name.EndsWith(".meta"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包被删或者改名了：" + files[i].Name);
                if(File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                }
            }
        }

    }

    static void WriteData(Dictionary<string, string> resPathDic)
    {
        //TODO 生成AB包配置表
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            ABBase ab = new ABBase();
            ab.Path = path;
            ab.Crc = CRC32.Calculate(path);
            ab.ABName = resPathDic[path];
            ab.AssetName = path.Substring(path.LastIndexOf("/") + 1, path.Length - path.LastIndexOf("/") - 1);
            ab.ABDependce = new List<string>();
            string[] allDepends = AssetDatabase.GetDependencies(path);
            for(int i = 0; i < allDepends.Length; i++)
            {
                string tempPath = allDepends[i];
                if(tempPath == path || tempPath.EndsWith(".cs"))
                {
                    continue;
                }
                string abName = "";
                if(resPathDic.TryGetValue(tempPath, out abName))
                {
                    if(abName == resPathDic[path])
                    {
                        continue;
                    }
                    if(!ab.ABDependce.Contains(abName))
                    {
                        ab.ABDependce.Add(abName);
                    }
                }
            }
            config.ABList.Add(ab);
        }
        //序列化前处理：空依赖列表设为null，避免生成空节点
        foreach (var ab in config.ABList)
        {
            if (ab.ABDependce != null && ab.ABDependce.Count == 0)
            {
                ab.ABDependce = null;
            }
        }

        //写入XML
        string configPath = Application.dataPath + "/AssetBundleConfig.xml";
        if(File.Exists(configPath))
        {
            File.Delete(configPath);
        }
        FileStream fileStream = new FileStream(configPath, FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xmlSerializer = new XmlSerializer(config.GetType());
        xmlSerializer.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        //写入二进制
        foreach (var ab in config.ABList)
        {
            ab.Path = "";
        }
        string binaryPath = "Assets/Editor/Tool/AB/Data/AssetBundleConfig.bytes";
        if(File.Exists(binaryPath))
        {
            File.Delete(binaryPath);
        }
        FileStream fs = new FileStream(binaryPath, FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
    }
}
