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
            resouceObj = ResourceManager.Instance.LoadResource(path, resouceObj);
            // 此处还需要加载并填写 resouceObj.m_ResItem
    
            if (resouceObj.m_ResItem.m_Obj != null)
            {
                resouceObj.m_CloneObj = GameObject.Instantiate(resouceObj.m_ResItem.m_Obj) as GameObject;
            }
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
