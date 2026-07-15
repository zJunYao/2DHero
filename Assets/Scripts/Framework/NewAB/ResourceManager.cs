using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : BaseManager<ResourceManager>
{
    private ResourceManager() { }


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