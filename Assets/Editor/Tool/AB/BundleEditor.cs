using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";
    //存储所有文件夹下的AB包 key：AB包名 value：AB包路径
    public static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤出所有需要打包进AB包中的预设体
    public static List<string> m_AllFilrAB = new List<string>();
    //单个prefab的ab包
    public static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();
    [MenuItem("Tools/打包")]
    public static void BuildAB()
    {
        m_AllFileDir.Clear();
        m_AllFilrAB.Clear();
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
            }
        }

        //找到m_AllPrefabPath下所有的预设体
        string[] allStr = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
        for(int i = 0; i < allStr.Length; i++)
        {
            //通过guid获取预设体路径
            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            EditorUtility.DisplayProgressBar("查找预设体", "正在查找：" + path, (float)i / allStr.Length);
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
        EditorUtility.ClearProgressBar();

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }
        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        string[] oldABName = AssetDatabase.GetAllAssetBundleNames();
        for(int i = 0; i < oldABName.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABName[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字：" + oldABName[i], (float)i / oldABName.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    static bool ContainAllFileAB(string path)
    {
        for(int i = 0; i < m_AllFilrAB.Count; i++)
        {
            if (path.Contains(m_AllFilrAB[i]) || path == m_AllFilrAB[i])
            {
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

}
