using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : BaseManager<ObjectManager>
{  
    //对象池节点
    public Transform RecyclePoolTrs;
    //场景节点
    public Transform SceneTrs;
    // 对象池字典  键为资源路径计算出的 CRC  值为该资源对应的多个可复用实例记录
    protected Dictionary<uint, List<ResouceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResouceObj>>();
    // 暂存(缓存) ResouceObj 的字典
    protected Dictionary<int, ResouceObj> m_ResouceObjDic = new Dictionary<int, ResouceObj>();
    //建立 ResouceObj 的类对象池
    protected ClassObjectPool<ResouceObj> m_ResourceObjClassPool = null;
    //根据异步的guid储存ressourceObj，来判断是否正在异步加载
    protected Dictionary<long, ResouceObj> m_AsyncResObjs = new Dictionary<long, ResouceObj>();
    /// <summary>
    /// 初始化对象池节点
    /// </summary>
    /// <param name="recycleTrs"> 回收节点</param>
    /// <param name="sceneTrs">场景默认节点</param>
    public void Init(Transform recycleTrs,Transform sceneTrs)
    {
        m_ResourceObjClassPool = ObjectManager.Instance.GetOrCreatClassPool<ResouceObj>(1000);
        RecyclePoolTrs = recycleTrs;
        SceneTrs = sceneTrs;
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void ClearCache()
    {
        List<uint> tempList = new List<uint>();
        foreach (uint key in m_ObjectPoolDic.Keys)
        {
            List<ResouceObj> st = m_ObjectPoolDic[key];
            for (int i = st.Count - 1; i >= 0; i--)
            {
                ResouceObj resObj = st[i];
                if (!System.Object.ReferenceEquals(resObj.m_CloneObj, null) && resObj.m_bClear)
                {
                    // 清理
                    GameObject.Destroy(resObj.m_CloneObj);
                    m_ResouceObjDic.Remove(resObj.m_CloneObj.GetInstanceID());
                    resObj.Reset();
                    m_ResourceObjClassPool.Recycle(resObj);
                }
            }

            if (st.Count <= 0)
            {
                tempList.Add(key);
            }
        }
        //遍历结束后统一删除字典键
        for (int i = 0; i < tempList.Count; i++)
        {
            uint temp = tempList[i];
            if (m_ObjectPoolDic.ContainsKey(temp))
            {
                m_ObjectPoolDic.Remove(temp);
            }
        }
        tempList.Clear();
    }

    /// <summary>
    /// 清除某个资源在对象池中所有的对象
    /// </summary>
    /// <param name="crc"></param>
    public void ClearPoolObject(uint crc)
    {
        //查询 CRC 对应的对象列表
        List<ResouceObj> st = null;
        if (!m_ObjectPoolDic.TryGetValue(crc, out st) || st == null)
            return;

        //倒序遍历对象列表
        for (int i = st.Count - 1; i >= 0; i--)
        {
            ResouceObj resObj = st[i];
            if (resObj.m_bClear)
            {
                // 清理对象
                st.Remove(resObj);
                int tempID = resObj.m_CloneObj.GetInstanceID();
                GameObject.Destroy(resObj.m_CloneObj);

                //重置并回收
                resObj.Reset();
                m_ResouceObjDic.Remove(tempID);
                m_ResourceObjClassPool.Recycle(resObj);
            }
        }
        //删除空的 CRC 条目
        if (st.Count <= 0)
        {
            m_ObjectPoolDic.Remove(crc);
        }
    }

    /// <summary>
    /// 更据实例化对象获取离线数据
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public OfflineData FindOfflineData(GameObject obj)
    {
        OfflineData data = null;
        ResouceObj resObj = null;
        m_ResouceObjDic.TryGetValue(obj.GetInstanceID(), out resObj);
        if (resObj != null)
        {
            data = resObj.m_OfflineData;
        }
        return data;
    }
    /// <summary>
    /// 从对象池中获取对象
    /// 1.如果对象池中有可用的对象，则直接返回该对象
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    protected ResouceObj GetObjectFromPool(uint crc)
    {
        //ResourceManager 做引用计数
        ResourceManager.Instance.IncreaseResouceRef(crc);
        List<ResouceObj> st = null;
        if (m_ObjectPoolDic.TryGetValue(crc, out st) && st != null && st.Count > 0)
        {
            ResouceObj resObj = st[0];
            st.RemoveAt(0);
    
            GameObject obj = resObj.m_CloneObj;
    
            if (!System.Object.ReferenceEquals(obj, null))
            {
                //通过离线数据填充对象属性
                if (!System.Object.ReferenceEquals(resObj.m_OfflineData, null))
                {
                    resObj.m_OfflineData.ResetProp();
                }
                resObj.m_Already = false;
#if UNITY_EDITOR
                if (obj.name.EndsWith("(Recycle)"))
                {
                    obj.name = obj.name.Replace("(Recycle)", "");
                }
#endif
            }
    
            return resObj;
        }
    
        return null;
    }

    /// <summary>
    /// 取消异步加载
    /// </summary>
    /// <param name="guid"></param>
    public void CancleLoad(long guid)
    {
        ResouceObj resObj = null;
        if (m_AsyncResObjs.TryGetValue(guid, out resObj) && ResourceManager.Instance.CancleLoad(resObj))
        {
            m_AsyncResObjs.Remove(guid);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
    }

    /// <summary>
    /// 判断是否正在异步加载
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public bool IsingAsyncLoad(long guid)
    {
        return m_AsyncResObjs[guid] != null;
    }

    /// <summary>
    /// 判断对象是否由ObjectManager创建
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool IsObjectManagerCreat(GameObject obj)
    {
        ResouceObj resObj = m_ResouceObjDic[obj.GetInstanceID()];
        return resObj == null ? false : true;
    }

    /// <summary>
    /// 资源实例化预加载
    /// </summary>
    /// <param name="path">资源的完整路径</param>
    /// <param name="count">需要预加载的对象实例数量</param>
    /// <param name="clear">切换场景时是否清除对应缓存</param>
    public void PreloadGameObject(string path, int count = 1, bool clear = false)
    {
        List<GameObject> tempGameObjectList = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject obj = InstantiateObject(path, false, bClear: clear);
            tempGameObjectList.Add(obj);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = tempGameObjectList[i];
            ReleaseObject(obj);
            obj = null;
        }

        tempGameObjectList.Clear();
    }

    /// <summary>
    /// 同步加载实例化的gameobject对象（池对象）
    /// </summary>
    /// <param name="path">资源路径</param>
    /// <param name="setSceneObj">赋值场景对象</param>
    /// <param name="bClear">切换场景时是否清理</param>
    /// <returns></returns>
    public GameObject InstantiateObject(string path, bool setSceneObj = false, bool bClear = true)
    {
        uint crc = CRC32.Calculate(path);
        ResouceObj resouceObj = GetObjectFromPool(crc);
    
        if (resouceObj == null)
        {
            resouceObj = m_ResourceObjClassPool.Spawn(true);
            resouceObj.m_Crc = crc;
            resouceObj.m_bClear = bClear;
    
            // ResourceManager 提供加载方法
            ResouceObj loadedResouceObj = ResourceManager.Instance.LoadResource(path, resouceObj);
            // 此处还需要加载并填写 resouceObj.m_ResItem

            if (loadedResouceObj == null ||
                loadedResouceObj.m_ResItem == null ||
                loadedResouceObj.m_ResItem.m_Obj == null)
            {
                Debug.LogError("InstantiateObject load failed: " + path);
                resouceObj.Reset();
                m_ResourceObjClassPool.Recycle(resouceObj);
                return null;
            }

            resouceObj = loadedResouceObj;
            resouceObj.m_CloneObj = GameObject.Instantiate(resouceObj.m_ResItem.m_Obj) as GameObject;
            resouceObj.m_OfflineData = resouceObj.m_CloneObj.GetComponent<OfflineData>();
        }
    
        if (setSceneObj)
        {
            resouceObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }

        int tempID = resouceObj.m_CloneObj.GetInstanceID();
        if (!m_ResouceObjDic.ContainsKey(tempID))
        {
            m_ResouceObjDic.Add(tempID, resouceObj);
        }
    
        return resouceObj.m_CloneObj;
    }
   
   /// <summary>
   /// 异步加载实例化的gameobject对象（池对象）
   /// </summary>
   /// <param name="path">资源路径</param>
   /// <param name="dealFinish">异步实例化完成回调 </param>
   /// <param name="priority">异步加载优先级</param>
   /// <param name="setSceneObject">是否将实例挂到场景节点 SceneTrs下</param>
   /// <param name="param1">回调透传参数 1 </param>
   /// <param name="param2"> 回调透传参数 2</param>
   /// <param name="param3"> 回调透传参数 3</param>
   /// <param name="bClear">是否参与场景清理，默认值为 true </param>
    public long InstantiateObjectAsync(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, bool setSceneObject = false, object param1 = null, object param2 = null, object param3 = null, bool bClear = true)
    {
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        uint crc = CRC32.Calculate(path);
        ResouceObj resObj = GetObjectFromPool(crc);
        // 对象池中有可用的对象
        if (resObj != null)
        {
            if (setSceneObject)
            {
                resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
            }
        
            if (dealFinish != null)
            {
                dealFinish(path, resObj.m_CloneObj, param1, param2, param3);
            }
            return resObj.m_Guid;
        }

        //创建 ResouceObj 对象
        long guid = ResourceManager.Instance.CreatGuid();
        resObj = m_ResourceObjClassPool.Spawn(true);
        resObj.m_Crc = crc;
        resObj.m_SetSceneParent = setSceneObject;
        resObj.m_bClear = bClear;
        resObj.m_DealFinish = dealFinish;
        resObj.m_Param1 = param1;
        resObj.m_Param2 = param2;
        resObj.m_Param3 = param3;
        resObj.m_Guid = guid;
        // 建立异步请求索引
        m_AsyncResObjs.Add(guid, resObj);
        //调用 ResourceManager 加载方法
        ResourceManager.Instance.AsyncLoadResource(path, resObj, OnLoadResouceObjFinish, priority);
        return guid;
    }

    /// <summary>
    /// 加载资源完成回调
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="resObj">中间类</param>
    /// <param name="param1">参数1</param>
    /// <param name="param2">参数2</param>
    /// <param name="param3">参数3</param>
    void OnLoadResouceObjFinish(string path, ResouceObj resObj, object param1 = null, object param2 = null, object param3 = null)
    {   
        if (resObj == null)
        {
            return;
        }

        if (resObj.m_ResItem == null || resObj.m_ResItem.m_Obj == null)
        {
            Debug.LogError("Async resource load failed: " + path);

            if (m_AsyncResObjs.ContainsKey(resObj.m_Guid))
            {
                m_AsyncResObjs.Remove(resObj.m_Guid);
            }

            resObj.m_DealFinish?.Invoke(path, null, resObj.m_Param1, resObj.m_Param2, resObj.m_Param3);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
            return;
        }
        else
        {
            // 实例化
            resObj.m_CloneObj = GameObject.Instantiate(resObj.m_ResItem.m_Obj) as GameObject;
            resObj.m_OfflineData = resObj.m_CloneObj.GetComponent<OfflineData>();
        }

        //如果取消加载字典中有该对象，移除加载对象
        if (m_AsyncResObjs.ContainsKey(resObj.m_Guid))
        {
            m_AsyncResObjs.Remove(resObj.m_Guid);
        }
        
        //按需挂载场景父节点
        if (resObj.m_CloneObj != null && resObj.m_SetSceneParent)
        {
            resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }

        if (resObj.m_DealFinish != null)
        {
            //登记实例对象
            int tempID = resObj.m_CloneObj.GetInstanceID();
            if (!m_ResouceObjDic.ContainsKey(tempID))
            {
                m_ResouceObjDic.Add(tempID, resObj);
            }
            //执行最外层回调
            resObj.m_DealFinish(path, resObj.m_CloneObj, resObj.m_Param1, resObj.m_Param2, resObj.m_Param3);
        }
    }

   
    /// <summary>
    /// 释放对象
    /// </summary>
    /// <param name="obj">接收需要释放的 GameObject</param>
    /// <param name="maxCacheCount">限制同类对象的最大缓存数量</param>
    /// <param name="destoryCache">控制是否销毁缓存</param>
    /// <param name="recycleParent">控制回收时是否挂到统一回收节点。</param>
    public void ReleaseObject(GameObject obj, int maxCacheCount = -1, bool destoryCache = false, bool recycleParent = true)
    {
        if (obj == null)
            return;

        //通过字典查找
        ResouceObj resObj = null;
        int tempID = obj.GetInstanceID();
        if (!m_ResouceObjDic.TryGetValue(tempID, out resObj))
        {
            Debug.Log(obj.name + "对象不是ObjectManager创建的!");
            return;
        }

        if (resObj == null)
        {
            Debug.LogError("缓存的ResouceObj为空!");
            return;
        }

        //释放前检测重复调用
        if (resObj.m_Already)
        {
            Debug.LogError("该对象已经放回对象池了，检测自己是否清空引用");
        }

#if UNITY_EDITOR
        obj.name += "(Recycle)";
#endif

        List<ResouceObj> st = null;
        if (maxCacheCount == 0)
        {
            //不缓存，直接销毁
            m_ResouceObjDic.Remove(tempID);
            ResourceManager.Instance.ReleaseResouce(resObj, destoryCache);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
        else
        {
            //回收到缓存池
            if (!m_ObjectPoolDic.TryGetValue(resObj.m_Crc, out st) || st == null)
            {
                st = new List<ResouceObj>();
                m_ObjectPoolDic.Add(resObj.m_Crc, st);
            }

            if (resObj.m_CloneObj)
            {
                if (recycleParent)
                {
                    resObj.m_CloneObj.transform.SetParent(RecyclePoolTrs);
                }
                else
                {
                    resObj.m_CloneObj.SetActive(false);
                }
            }
            //限制缓存数量
            if (maxCacheCount < 0 || st.Count < maxCacheCount)
            {
                st.Add(resObj);
                resObj.m_Already = true;
                //ResourceManager 做引用计数
                ResourceManager.Instance.DecreaseResoucerRef(resObj);
            }
            else
            {
                m_ResouceObjDic.Remove(tempID);
                ResourceManager.Instance.ReleaseResouce(resObj, destoryCache);
                resObj.Reset();
                m_ResourceObjClassPool.Recycle(resObj);
            }
        }
    }

    #region 类对象池的使用
    private ObjectManager() { }
    // 对象池字典，key为对象类型，value为对象池
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();
    
    /// <summary>
    /// 创建类对象池。创建完成以后，外部可以保存 ClassObjectPool<T>，
    /// 然后调用 Spawn 和 Recycle 来创建和回收类对象。
    /// </summary>
    public ClassObjectPool<T> GetOrCreatClassPool<T>(int maxcount) where T : class, new()
    {
        Type type = typeof(T);
        object outObj = null;
        if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxcount);
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }
 
        return outObj as ClassObjectPool<T>;
    }
    #endregion
}
