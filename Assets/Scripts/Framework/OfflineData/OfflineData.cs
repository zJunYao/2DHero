using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineData : MonoBehaviour
{
    public Rigidbody m_Rigidbody;
    public Collider m_Collider;
 
    public Transform[] m_AllPoint;
    public int[] m_AllPointChildCount;
    public bool[] m_AllPointActive;
 
    public Vector3[] m_Pos;
    public Vector3[] m_Scale;
    public Quaternion[] m_Rot;
 
    /// <summary>
    /// 重置属性
    /// </summary>
    public virtual void ResetProp()
    {
        int allPointCount = m_AllPoint.Length;
        for (int i = 0; i < allPointCount; i++)
        {
            Transform t = m_AllPoint[i];
            if (t != null)
            {
                t.localScale = m_Scale[i];
                t.rotation = m_Rot[i];
                t.position = m_Pos[i];
                
                //还原激活状态
                if(m_AllPointActive[i])
                {
                    if(!t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(true);
                    }
                }
                else
                {
                    if(t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                    }
                }

                if(t.childCount > m_AllPointChildCount[i])
                {
                    int childCount = t.childCount;
                    for (int j = m_AllPointChildCount[i]; j < childCount; j++)
                    {
                        GameObject tempObj = t.GetChild(j).gameObject;
                        if (!ObjectManager.Instance.IsObjectManagerCreat(tempObj))
                        {
                            GameObject.Destroy(tempObj);
                        }
                    }
                }
            }
        }
    }
 
    /// <summary>
    /// 编辑器下保存初始数据
    /// </summary>
    public virtual void BindData()
    {
        m_Collider = gameObject.GetComponentInChildren<Collider>(true);
        m_Rigidbody = gameObject.GetComponentInChildren<Rigidbody>(true);

        m_AllPoint = gameObject.GetComponentsInChildren<Transform>(true);
        m_AllPointChildCount = new int[m_AllPoint.Length];
        m_AllPointActive = new bool[m_AllPoint.Length];
        m_Pos = new Vector3[m_AllPoint.Length];
        m_Scale = new Vector3[m_AllPoint.Length];
        m_Rot = new Quaternion[m_AllPoint.Length];
        for (int i = 0; i < m_AllPoint.Length; i++)
        {
            Transform t = m_AllPoint[i];
            m_AllPointChildCount[i] = t.childCount;
            m_AllPointActive[i] = t.gameObject.activeSelf;
            m_Pos[i] = t.position;
            m_Scale[i] = t.localScale;
            m_Rot[i] = t.rotation;
        }
    }
}
