using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : BaseManager<ObjectManager>
{  
    #region 对象池的使用
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
