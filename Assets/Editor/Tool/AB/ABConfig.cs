using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( fileName = "ABConfig", menuName = "Create ABConfig", order = 1)]
public class ABConfig : ScriptableObject
{
    /// <summary>
    /// 所有需要打包进AB包中的预设体路径
    /// </summary>
    public List<string> m_AllPrefabPath = new List<string>();
    public List<FileDirABName> m_FileDirAB = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName
    {
        public string ABName;
        public string Path;
    }
}
