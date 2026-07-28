using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResouceObj
{
    // 路径对应的 CRC
    public uint m_Crc = 0;
    //ResourceManager 管理的资源信息块，其中包含原始资源对象	
    public ResouceItem m_ResItem = null;
    // 实例化出来的 GameObject
    public GameObject m_CloneObj = null;
    // 切换场景时是否清理
    public bool m_bClear = true;
    // 对象唯一 ID
    public int m_Guid = 0;
    // 是否已经放回对象池
    public bool m_Already = false;
 
    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
        m_ResItem = null;
        m_Already = false;
    }
}

public enum LoadResPriority
{
    RES_HIGHT = 0, // 最高优先级
    RES_MIDDLE,    // 一般优先级
    RES_SLOW,      // 低优先级
    RES_NUM
}

//使用独立对象封装加载请求
public class AsyncLoadResParam
{
    // 回调参数列表
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();
    // 资源路径的CRC
    public uint m_Crc;
    // 资源路径
    public string m_Path;
    //是否需要以 Sprite 类型加载
    public bool m_Sprite = false;
    // 资源加载优先级
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;
    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = "";
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

/// <summary>
/// 回调参数封装类
/// </summary>
public class AsyncCallBack
{
    // 加载完成回调
    public OnAsyncObjFinish m_DealFinish = null;
    // 回调参数1
    public object m_Param1 = null;
    // 回调参数2
    public object m_Param2 = null;
    // 回调参数3
    public object m_Param3 = null;
    public void Reset()
    {
        m_DealFinish = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Param3 = null;
    }
}

/// <summary>
/// 异步加载资源完成回调委托
/// </summary>
/// <param name="path"></param>
/// <param name="obj"></param>
/// <param name="param1"></param>
/// <param name="param2"></param>
/// <param name="param3"></param>
public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);

public class ResourceManager : BaseManager<ResourceManager>
{
    private ResourceManager() { }
    // 是否从AssetBundle加载资源，true表示从AssetBundle加载，false表示从Editor API加载
    public bool m_LoadFormAssetBundle = true;
    //缓存已加载资源字典
    public Dictionary<uint, ResouceItem> AssetDic { get; set; } = new Dictionary<uint, ResouceItem>();
    //缓存引用计数为0的资源对象，达到最大缓存数量时，释放最久未使用的资源对象
    protected CMapList<ResouceItem> m_NoRefrenceAssetMapList = new CMapList<ResouceItem>();

    //中间类 回调类的对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = new ClassObjectPool<AsyncCallBack>(100);

    //MonoBehaviour对象，用于协程的启动和停止
    protected MonoBehaviour m_Startmono;
    private bool m_Initialized;
    private Coroutine m_AsyncLoadCoroutine;
    // 正在异步加载的资源列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    // 记录正在异步加载资源的字典
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    //最长连续卡着加载时间，单位微秒
    private const long MAXLOADRESTIME = 200000; 

    // 协程加载资源队列
    public void Init(MonoBehaviour mono)
    {
        if (m_Initialized)
        {
            if (m_Startmono != mono)
            {
                Debug.LogWarning("ResourceManager 已初始化，禁止更换 MonoBehaviour 宿主。");
            }

            return;
        }

        if (mono == null)
        {
            Debug.LogError("ResourceManager.Init mono is null");
            return;
        }

        for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            // 为当前优先级创建异步加载队列
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        m_Startmono = mono;
        m_AsyncLoadCoroutine = m_Startmono.StartCoroutine(AsyncLoadCor());
        m_Initialized = true;
    }

    /// <summary>
    /// 缓存太多清理没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        
    }

    /// <summary>
    ///  清空缓存 
    /// </summary>
    public void ClearCache()
    {
        List<ResouceItem> tempList = new List<ResouceItem>();

        foreach (ResouceItem item in AssetDic.Values)
        {
            if (item.m_Clear)
            {
                tempList.Add(item);
            }
        }
    
        foreach (ResouceItem item in tempList)
        {
            DestoryResouceItme(item, true);
        }
    
        tempList.Clear();
    }

    /// <summary>
    /// 预加载
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path)
    {
        //首先校验路径
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        uint crc = CRC32.Calculate(path);
        //第二个参数使用 0，因为预加载不代表资源正在被正常业务逻辑引用
        ResouceItem item = GetCacheResouceItem(crc, 0);
        if (item != null)
        {
            return;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!m_LoadFormAssetBundle)
        {
            // 通过 Editor API 加载
            item = AssetBundleManager.Instance.FindResouceItem(crc);
            if(item.m_Obj != null)
            {
                obj = item.m_Obj;
            }
            else
            {
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif
        if (obj == null)
        {
            // 通过 AssetBundle 加载
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    // 如果资源对象已经加载过了，就直接返回
                    obj = item.m_Obj;
                }
                else
                {
                    // 如果资源对象没有加载过，就从 AssetBundle 中加载资源对象
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }
        // 缓存资源对象
        CacheResource(path, ref item, crc, obj);
        //跳场景 不清空缓存
        item.m_Clear = false;
        ReleaseResouce(obj, false);
    }

    #region 资源同步加载
    /// <summary>
    /// 释放资源对象 分为两种情况：
    /// 1.如果资源对象的引用计数大于0，则将其引用计数减1，并将其最后使用时间更新为当前时间
    /// 2.如果资源对象的引用计数为0，则将其从缓存中移除，并释放其占用的内存空间
    /// 3.如果destroyCache为true，则直接销毁资源对象，不管引用
    /// </summary>
    /// <param name="item"> 资源项 </param>
    /// <param name="destroyCache"> 是否销毁缓存 </param>
    protected void DestoryResouceItme(ResouceItem item, bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
        {
            return;
        }
        // 不销毁时保留在 AssetDic，作为零引用缓存
        if (!destroyCache)
        {
            // m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }
        //从资源字典移除
        if (!AssetDic.Remove(item.m_Crc))
        {
            return;
        }
        //释放AssetBundle
        AssetBundleManager.Instance.ReleaseAsset(item);
        item.m_AssetBundle = null;
        item.m_Guid = 0;
        //清空资源对象引用
        if (item.m_Obj != null)
        {
            item.m_Obj = null;
 //清空资源对象引用后，调用Resources.UnloadUnusedAssets()释放内存
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }
    }

    private bool TryDecreaseRef(ResouceItem item, string source)
    {
        if (item == null)
        {
            return false;
        }

        if (item.RefCount <= 0)
        {
            Debug.LogError("资源重复释放，Crc=" + item.m_Crc + ", Source=" + source);
            return false;
        }

        item.RefCount--;
        item.m_LastUseTime = Time.realtimeSinceStartup;
        return true;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 通过 Editor API 加载资源
    /// 1.该方法只在编辑器模式下使用，打包后无法使用
    /// 2.该方法不需要加载 AssetBundle，直接通过资源路径加载资源     
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源 例如 Texture、AudioClip、Sprite 等
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("LoadResource path is null");
            return null;
        }
        uint crc = CRC32.Calculate(path);
        ResouceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }

        T obj = null;
#if UNITY_EDITOR
        if (!m_LoadFormAssetBundle)
        {
            // 通过 Editor API 加载
            item = AssetBundleManager.Instance.FindResouceItem(crc);
            if(item.m_Obj != null)
            {
                obj = item.m_Obj as T;
            }
            else
            {
                obj = LoadAssetByEditor<T>(path);
            }
        }
#endif
        if (obj == null)
        {
            // 通过 AssetBundle 加载
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    // 如果资源对象已经加载过了，就直接返回
                    obj = item.m_Obj as T;
                }
                else
                {
                    // 如果资源对象没有加载过，就从 AssetBundle 中加载资源对象
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            }
        }
        // 缓存资源对象
        CacheResource(path, ref item, crc, obj);
        return obj;
    }

    /// <summary>
    /// 不需要实例化的资源卸载
    /// </summary>
    /// <param name="obj"> 资源对象 </param>
    /// <param name="destoryObj"> 是否销毁资源对象 </param>
    /// <returns></returns>
    public bool ReleaseResouce(Object obj, bool destoryObj = false)
    {
        if (obj == null)
        {
            return false;
        }

        ResouceItem item = null;
        foreach (ResouceItem res in AssetDic.Values)
        {
            if (res.m_Guid == obj.GetInstanceID())
            {
                item = res;
            }
        }

        if (item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + obj.name + " 可能释放了多次");
            return false;
        }

        if (!TryDecreaseRef(item, obj.name))
        {
            return false;
        }

        DestoryResouceItme(item, destoryObj);
        return true;
    }

    /// <summary>
    /// 不需要实例化的资源卸载
    /// </summary>
    /// <param name="path"> 资源路径 </param>
    /// <param name="destoryObj"> 是否销毁资源对象 </param>
    /// <returns></returns>
    public bool ReleaseResouce(string path, bool destoryObj = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        uint crc = CRC32.Calculate(path);

        ResouceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + path + " 可能释放了多次");
            return false;
        }
        if (!TryDecreaseRef(item, path))
        {
            return false;
        }

        DestoryResouceItme(item, destoryObj);
        return true;
    }

    /// <summary>
    /// 缓存资源对象
    /// 1.如果缓存中有该资源对象，则更新该资源对象的引用计数和最后使用时间
    /// 2.如果缓存中没有该资源对象，则将该资源对象添加到缓存中，并设置该资源对象的引用计数和最后使用时间 
    /// </summary>
    /// <param name="path"> 资源路径 </param>
    /// <param name="item"> 资源项 </param>
    /// <param name="crc"> 资源路径的CRC </param>
    /// <param name="obj"> 资源对象 </param>
    /// <param name="addrefcount"> 要增加的引用计数 </param>
    void CacheResource(string path, ref ResouceItem item, uint crc, Object obj, int addrefcount = 1)
    {   
        // 缓存太多清理没有使用的资源
        WashOut();
        if (item == null)
        {
            Debug.LogError("ResouceItem is null, path: " + path);
        }
        if (obj == null)
        {
            Debug.LogError("ResouceLoad Fail : " + path);
        }

        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addrefcount;
        ResouceItem oldItem = null;
        if (AssetDic.TryGetValue(crc, out oldItem))
        {
            AssetDic[item.m_Crc] = item;
        }
        else
        {
            AssetDic.Add(crc, item);
        }
    } 

    /// <summary>
    /// 根据CRC获取缓存的资源对象
    /// 1.如果缓存中有该资源对象，则返回该资源对象，并将其引用计数加1
    /// 2.如果缓存中没有该资源对象，则返回null  
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="addrefcount"></param>
    /// <returns></returns>
    ResouceItem GetCacheResouceItem(uint crc, int addrefcount = 1)
    {
        ResouceItem item = null;
        if (AssetDic.TryGetValue(crc, out item))
        {
            item.RefCount += addrefcount;
            item.m_LastUseTime = Time.realtimeSinceStartup;

            // if (item.RefCount <= 1)
            // {
            //     m_NoRefrenceAssetMapList.Remove(item);
            // }
        }
        return item;    
    }
    #endregion
    
    #region 资源异步加载
    /// <summary>
    /// 异步加载 仅仅是加载资源，不需要实例化的资源 例如 Texture、AudioClip、Sprite 等
    /// </summary>
    /// <param name="path"> 资源路径 </param>
    /// <param name="dealFinish"> 加载完成回调 </param>
    /// <param name="priority"> 加载优先级 </param>
    /// <param name="param1"> 参数1 </param>
    /// <param name="param2"> 参数2 </param>
    /// <param name="param3"> 参数3 </param>
    /// <param name="crc"> 资源路径的CRC </param>
    public void AsyncLoadResource(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, object param1 = null, object param2 = null, object param3 = null, uint crc = 0)
    {
        // 如果CRC为0，则计算CRC
        if (crc == 0)
        {
            crc = CRC32.Calculate(path);
        }

        ResouceItem item = GetCacheResouceItem(crc);
        // 如果缓存中已经存在资源，则直接执行回调
        if (item != null)
        {
            if (dealFinish != null)
            {
                dealFinish(path, item.m_Obj, param1, param2, param3);
            }
        
            return;
        }

        // 如果缓存中不存在资源，则将加载请求添加到异步加载队列中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(crc, out para) || para == null)
        {
            // 该资源当前没有处于异步加载中
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Path = path;
            para.m_Crc = crc;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }

        //往回调列表里面添加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealFinish = dealFinish;
        callBack.m_Param1 = param1;
        callBack.m_Param2 = param2;
        callBack.m_Param3 = param3;
        para.m_CallBackList.Add(callBack);
    }
    //异步加载
    IEnumerator AsyncLoadCor()
    {
        List<AsyncCallBack> callBackList = null;
        // 记录上一次让出时间
        long lastYiledTime = System.DateTime.Now.Ticks;
        while (true)
        {
            bool haveYield = false;
            // 遍历所有优先级的异步加载队列，按优先级从高到低依次处理
            for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
            {
                // 如果当前优先级的异步加载队列为空，则跳过
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                    continue;

                // 取出队列中的第一个加载请求
                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBackList = loadingItem.m_CallBackList;

                try
                {
                    //加载资源
                    Object obj = null;
                    ResouceItem item = null;

#if UNITY_EDITOR
                    if (!m_LoadFormAssetBundle)
                    {
                        // 通过 Editor API 加载
                        obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                        //模拟异步加载
                        yield return new WaitForSeconds(0.5f);

                        item = AssetBundleManager.Instance.FindResouceItem(loadingItem.m_Crc);
                    }
#endif
                    // 通过 AssetBundle 加载
                    if (obj == null)
                    {
                        item = AssetBundleManager.Instance.LoadResouceAssetBundle(loadingItem.m_Crc);
                        if (item != null && item.m_AssetBundle != null)
                        {
                            AssetBundleRequest abRequest = null;
                            if (loadingItem.m_Sprite)
                            {
                                abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                            }else
                            {
                                abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                            }
                            // 等待异步加载完成
                            yield return abRequest;
                            if (abRequest.isDone)
                            {
                                obj = abRequest.asset;
                            }
                            lastYiledTime = System.DateTime.Now.Ticks;
                        }
                    }
                    // 缓存资源对象
                    CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj,callBackList.Count);

                    // 执行回调
                    for (int j = 0; j < callBackList.Count; j++)
                    {
                        AsyncCallBack callBack = callBackList[j];
                        if (callBack == null)
                        {
                            continue;
                        }

                        try
                        {
                            callBack.m_DealFinish?.Invoke(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2, callBack.m_Param3);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                        finally
                        {
                            callBack.Reset();
                            m_AsyncCallBackPool.Recycle(callBack);
                            callBackList[j] = null;
                        }
                    }
                }
                finally
                {
                    for (int j = 0; j < callBackList.Count; j++)
                    {
                        AsyncCallBack callBack = callBackList[j];
                        if (callBack != null)
                        {
                            callBack.Reset();
                            m_AsyncCallBackPool.Recycle(callBack);
                        }
                    }

                    callBackList.Clear();
                    m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                    loadingItem.Reset();
                    m_AsyncLoadResParamPool.Recycle(loadingItem);
                }

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            if (!haveYield || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                lastYiledTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }
    }
    #endregion

    #region 资源实例化同步加载
    /// <summary>
    /// 根据resobj增加资源引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int IncreaseResouceRef(ResouceObj resObj, int count = 1)
    {
        return resObj != null ? IncreaseResouceRef(resObj.m_Crc, count) : 0;
    }
    /// <summary>
    /// 根据path增加资源引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int IncreaseResouceRef(uint crc = 0, int count = 1)
    {
        ResouceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
        {
            return 0;
        }
    
        item.RefCount += count;
        item.m_LastUseTime = Time.realtimeSinceStartup;
        return item.RefCount;
    }
    /// <summary>
    /// 根据resobj减少资源引用计数
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResoucerRef(ResouceObj resObj, int count = 1)
    {
        return resObj != null ? DecreaseResoucerRef(resObj.m_Crc, count) : 0;
    }
    /// <summary>
    /// 根据path减少资源引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResoucerRef(uint crc, int count = 1)
    {
        ResouceItem item = null;
    
        if (!AssetDic.TryGetValue(crc, out item) || item == null)
        {
            return 0;
        }
    
        item.RefCount -= count;
        return item.RefCount;
    }

    /// <summary>
    /// ResouceObj 资源加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <returns></returns>
    public ResouceObj LoadResource(string path, ResouceObj resObj)
    {
        if (resObj == null)
        {
            return null;
        }

        uint crc = resObj.m_Crc == 0 ? CRC32.Calculate(path) : resObj.m_Crc;
        // 先尝试从缓存中获取资源对象
        ResouceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            resObj.m_ResItem = item;
            return resObj;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!m_LoadFormAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResouceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResouceAssetBundle(crc);
        
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }

        CacheResource(path, ref item, crc, obj);
 
        resObj.m_ResItem = item;
        item.m_Clear = resObj.m_bClear;
        
        return resObj;
    }

    /// <summary>
    /// 实例化资源对象的资源卸载
    /// </summary>
    /// <param name="resObj">实例化资源对象</param>
    /// <param name="destoryObj">是否销毁资源对象</param>
    /// <returns></returns>
    public bool ReleaseResouce(ResouceObj resObj, bool destoryObj = false)
    {
        if (resObj == null)
            return false;
 
        ResouceItem item = null;
    
        if (!AssetDic.TryGetValue(resObj.m_Crc, out item) || item == null)
        {
            Debug.LogError("AssetDic里不存在该资源: " + resObj.m_CloneObj.name + " 可能释放了多次");
        }
    
        GameObject.Destroy(resObj.m_CloneObj);
    
        item.RefCount--;
        DestoryResouceItme(item, destoryObj);
    
        return true;
    }

    #endregion
}

#region 双向链表
/// <summary>
/// 双向链表节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinkedListNode<T> where T : class, new()
{
    // 前一个节点
    public DoubleLinkedListNode<T> prev = null;
    // 后一个节点
    public DoubleLinkedListNode<T> next = null;
    // 当前节点
    public T t = null;
}
/// <summary>
/// 双向链表
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinedList<T> where T : class, new()
{
    // 表头
    public DoubleLinkedListNode<T> Head = null;
    // 表尾
    public DoubleLinkedListNode<T> Tail = null;
    // 双向链表节点对象池
    protected ClassObjectPool<DoubleLinkedListNode<T>> m_DoubleLinkNodePool = ObjectManager.Instance.GetOrCreatClassPool<DoubleLinkedListNode<T>>(500);
    // 双向链表节点个数
    protected int m_Count = 0;
    public int Count
    {
        get { return m_Count; }
    }

    /// <summary>
    /// 创建头部节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(T t)
    {
        DoubleLinkedListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToHeader(pList);
    }

    /// <summary>
    /// 将节点添加到链表头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;
        pNode.prev = null;
        if (Head == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }

        m_Count++;
        return Head;
    }

    /// <summary>
    /// 创建尾部节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(T t)
    {
        DoubleLinkedListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToTail(pList);
    }

    /// <summary>
    /// 将节点添加到链表尾部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;

        pNode.next = null;

        if (Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            Tail.next = pNode;
            pNode.prev = Tail;
            Tail = pNode;
        }

        m_Count++;
        return Tail;
    }

    /// <summary>
    /// 移除某个节点
    /// </summary>
    /// <param name="pNode"> 要移除的节点</param>
    public void RemoveNode(DoubleLinkedListNode<T> pNode)
    {
        // 节点为空直接返回
        if (pNode == null)
            return;
        // 如果是头节点
        if (pNode == Head)
            Head = pNode.next;
        // 如果是尾节点
        if (pNode == Tail)
            Tail = pNode.prev;
        // 如果是中间节点
        if (pNode.prev != null)
            pNode.prev.next = pNode.next;
        // 如果是中间节点
        if (pNode.next != null)
            pNode.next.prev = pNode.prev;
        // 回收节点
        pNode.next = pNode.prev = null;
        pNode.t = null;
    
        m_DoubleLinkNodePool.Recycle(pNode);
        m_Count--;
    }

    /// <summary>
    /// 将节点移动到链表头部
    /// 资源管理过程中，除了删除节点，还可能需要调整已有节点的位置。例如某个资源再次被使用时，可以将其对应节点移动到链表前端。因此新增
    /// </summary>
    /// <param name="pNode"></param>
    public void MoveToHead(DoubleLinkedListNode<T> pNode)
    {
        // 节点为空或者节点就是头节点，直接返回
        if (pNode == null || pNode == Head)
            return;
        // 节点既不是头节点也不是尾节点
        if (pNode.prev == null && pNode.next == null)
            return;
        // 节点是尾节点
        if (pNode == Tail)
            Tail = pNode.prev;
        // 节点是中间节点
        if (pNode.prev != null)
            pNode.prev.next = pNode.next;
        // 节点是中间节点
        if (pNode.next != null)
            pNode.next.prev = pNode.prev;
        // 将节点移动到链表头部
        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;
        // 如果链表中只有一个节点，移动后尾节点也要指向头节点
        if (Tail == null)
        {
            Tail = Head;
        }
    }

}

#endregion

#region 封装双向链表类
public class CMapList<T> where T : class, new()
{
    // 双向链表
    DoubleLinedList<T> m_DLink = new DoubleLinedList<T>();
    // 查找字典
    Dictionary<T, DoubleLinkedListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkedListNode<T>>();

    // 析构函数
    ~CMapList()
    {
        Clear();
    }

    /// <summary>
    /// 清空链表
    /// </summary>
    public void Clear()
    {
        while (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }
    
    /// <summary>
    /// 将元素插入到链表头部
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (m_FindMap.TryGetValue(t, out node) && node != null)
        {
            m_DLink.AddToHeader(node);
            return;
        }
        m_DLink.AddToHeader(t);
        m_FindMap.Add(t, m_DLink.Head);
    }

    /// <summary>
    /// 弹出链表尾部元素
    /// </summary>
    public void Pop()
    {
        if (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    /// <summary>
    /// 移除元素
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t)
    {
        DoubleLinkedListNode<T> node = null;
    
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
            return;
        }
    
        m_DLink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 获取链表尾部元素
    /// </summary>
    /// <returns></returns>
    public T Back()
    {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }
    
    /// <summary>
    /// 获取节点数量
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return m_FindMap.Count;
    }

    /// <summary>
    /// 查找元素是否存在
    /// </summary>
    /// <param name="t"> 要查找的元素 </param>
    /// <returns></returns>
    public bool Find(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;

        return true;
    }

    /// <summary>
    /// 刷新节点到表头
    /// </summary>
    /// <param name="t"> 要刷新的元素 </param>
    /// <returns></returns>
    public bool Reflesh(T t)
    {
        DoubleLinkedListNode<T> node = null;
    
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
            return false;
    
        m_DLink.MoveToHead(node);
        return true;
    }
}

#endregion
