using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : BaseManager<AssetBundleManager>
{
    private AssetBundleManager() { }
    // 资源关系依赖配置表，key为资源路径的CRC，value为资源对象
    protected Dictionary<uint, ResouceItem> m_ResouceItemDic = new Dictionary<uint, ResouceItem>();
    // 储存已加载的AB包，key为AB包的CRC，value为AB包对象
    protected Dictionary<uint, AssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();
    // AssetBundleItem类对象池
    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectManager.Instance.GetOrCreatClassPool<AssetBundleItem>(500);
    /// <summary>
    /// 加载AssetBundle配置文件
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        m_ResouceItemDic.Clear();
        string configPath = Application.streamingAssetsPath + "/abconfig";
        AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
        if (configAB == null)
        {
            Debug.LogError("Failed to load AssetBundleConfig! Path: " + configPath);
            return false;
        }

        TextAsset textAsset = configAB.LoadAsset<TextAsset>("assetbundleconfig");
        if (textAsset == null)
        {
            Debug.LogError("AssetBundleConfig is no exist!");
            return false;
        }

        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig ab_config = (AssetBundleConfig)bf.Deserialize(stream);
        stream.Close();
    
        for (int i = 0; i < ab_config.ABList.Count; i++)
        {
            ABBase abBase = ab_config.ABList[i];
            ResouceItem item = new ResouceItem();
            item.m_Crc = abBase.Crc;
            item.m_AssetName = abBase.AssetName;
            item.m_ABName = abBase.ABName;
            item.m_DependAssetBundle = abBase.ABDependce;
            if (m_ResouceItemDic.ContainsKey(item.m_Crc))
            {
                Debug.LogError("重复的Crc 资源名:" + item.m_AssetName + " ab包名: " + item.m_ABName);
            }
            else
            {
                m_ResouceItemDic.Add(item.m_Crc, item);
            }
        }
        return true;
    }

    /// <summary>
    /// 加载资源的AssetBundle包
    /// </summary>
    /// <param name="crc">资源路径的CRC</param>
    /// <returns></returns>
    public ResouceItem LoadResouceAssetBundle(uint crc)
    {
        ResouceItem item = null;
        // 查询时同时检查 CRC 是否存在以及资源项是否为空：
        if (!m_ResouceItemDic.TryGetValue(crc, out item) || item == null)
        {
            Debug.LogError(string.Format("LoadResourceAssetBundle error: can not find crc {0} in AssetBundleConfig", crc.ToString()));
            return item;
        }
        // 如果资源对象的AB包已经加载过了，就直接返回
        if (item.m_AssetBundle != null)
        {
            return item;
        }
        // 先加载依赖包
        if (item.m_DependAssetBundle != null)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                LoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        // 再加载资源对象的AB包
        item.m_AssetBundle = LoadAssetBundle(item.m_ABName);
        return item;
    }

    /// <summary>
    /// 加载单个资源的AssetBundle包  
    /// </summary>
    /// <param name="name">接收 AB 包名称</param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.Calculate(name);

        if (!m_AssetBundleItemDic.TryGetValue(crc, out item))
        {
            AssetBundle assetBundle = null;
            string fullPath = Application.streamingAssetsPath + "/" + name;
    
            if (File.Exists(fullPath))
            {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }
            if (assetBundle == null)
            {
                Debug.LogError("Load AssetBundle Error:" + fullPath);
            }
            item = m_AssetBundleItemPool.Spawn(true);
            item.assetBundle = assetBundle;
            item.RefCount++;
            m_AssetBundleItemDic.Add(crc, item);
        }
        else
        {
            item.RefCount++;
        }
    
        return item.assetBundle;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResouceItem item)
    {
        if (item == null)
        {
            return;
        }

        // 先释放依赖
        if(item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                UnLoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        // 再释放自身
        UnLoadAssetBundle(item.m_ABName);
    }

    /// <summary>
    /// 释放AB包
    /// </summary>
    /// <param name="name">AB包名称</param>
    private void UnLoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.Calculate(name);
        if (m_AssetBundleItemDic.TryGetValue(crc, out item) && item != null)
        {
            item.RefCount--;
            if (item.RefCount <= 0  && item.assetBundle != null)
            {
                item.assetBundle.Unload(true);
                item.Rest();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleItemDic.Remove(crc);
            }
        }
    }

    /// <summary>
    /// 查找资源项
    /// </summary>
    /// <param name="crc">资源路径的CRC</param>
    /// <returns></returns>
    public ResouceItem FindResouceItme(uint crc)
    {
        return m_ResouceItemDic[crc];
    }
    
}

/// <summary>
/// 为后续缓存和引用计数管理定义的数据结构
/// </summary>
public class AssetBundleItem
{
    // AB包对象
    public AssetBundle assetBundle = null;
    // 引用计数
    public int RefCount;

    /// 重置数据
    public void Rest()
    {
        assetBundle = null;
        RefCount = 0;
    }
}


/// <summary>
/// 资源项类
/// </summary>
public class ResouceItem
{
    #region  AssetBundle 配置信息
    // 资源路径的CRC
    public uint m_Crc = 0;
    // 该资源的文件名
    public string m_AssetName = string.Empty;
    // 该资源所在的AssetBundle名称
    public string m_ABName = string.Empty;
    // 该资源所依赖的AssetBundle列表
    public List<string> m_DependAssetBundle = null;
    // 该资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
    #endregion

    #region 资源缓存信息
    // 资源对象
    public Object m_Obj = null;
    // 资源对象的唯一标识
    public int m_Guid = 0;
    // 资源最后使用时间
    public float m_LastUseTime = 0.0f;
    // 资源引用计数
    protected int m_RefCount = 0;
    // 是否跳场景清掉
    public bool m_Clear = true;

    public int RefCount
    {
        get { return m_RefCount; }
        set
        {
            m_RefCount = value;
            if (m_RefCount < 0)
            {
                Debug.LogError("refcount < 0" + m_RefCount + "," + (m_Obj != null ? m_Obj.name : "name is null"));
            }
        }
    }
    #endregion
}
