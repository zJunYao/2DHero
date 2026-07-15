using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : BaseManager<ResourceManager>
{
    private ResourceManager() { }
    // 是否从AssetBundle加载资源，true表示从AssetBundle加载，false表示从Editor API加载
    public bool m_LoadFormAssetBundle = true;
    //缓存已加载资源字典
    public Dictionary<uint, ResouceItem> AssetDic { get; set; } = new Dictionary<uint, ResouceItem>();
    //缓存引用计数为0的资源对象，达到最大缓存数量时，释放最久未使用的资源对象
    protected CMapList<ResouceItem> m_NoRefrenceAssetMapList = new CMapList<ResouceItem>();

    /// <summary>
    /// 缓存太多清理没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        // 当内存使用率超过阈值时，清除最早未使用的资源
        // {
        //     if (m_NoRefrenceAssetMapList.Size() <= 0)
        //         break;
    
        //     ResouceItem item = m_NoRefrenceAssetMapList.Back();
        //     DestoryResouceItme(item, true);
        //     m_NoRefrenceAssetMapList.Pop();
        // }
    }

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
        //从资源字典移除
        if (!AssetDic.Remove(item.m_Crc))
        {
            return;
        }
        //保留为无引用缓存
        if (!destroyCache)
        {
            m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }
        //释放AssetBundle
        AssetBundleManager.Instance.ReleaseAsset(item);
        //清空资源对象引用
        if (item.m_Obj != null)
        {
            item.m_Obj = null;
        }
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
            item = AssetBundleManager.Instance.FindResouceItme(crc);
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
    /// <param name="obj"></param>
    /// <param name="destoryObj"></param>
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

        //每执行一次释放，引用计数减一
        item.RefCount--;
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

            if (item.RefCount <= 1)
            {
                m_NoRefrenceAssetMapList.Remove(item);
            }
        }
        return item;    
    }

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
