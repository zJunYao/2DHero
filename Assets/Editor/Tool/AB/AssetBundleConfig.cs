using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using Unity.VisualScripting;

[System.Serializable]
public class AssetBundleConfig
{
    /// <summary>
    /// AssetBundle配置列表，包含所有AssetBundle的信息
    /// </summary>
    [XmlElement("ABList")]
    public List<ABBase> ABList = new List<ABBase>();
}

[System.Serializable]
public class ABBase
{
    /// <summary>
    /// 资源在项目中的相对路径
    /// </summary>
    [XmlAttribute("Path")]
    public string Path { get; set; }

    /// <summary>
    /// 资源的CRC校验值，用于验证资源完整性
    /// </summary>
    [XmlAttribute("Crc")]
    public uint Crc { get; set; }

    /// <summary>
    /// AssetBundle的名称
    /// </summary>
    [XmlAttribute("ABName")]
    public string ABName { get; set; }

    /// <summary>
    /// 资源的名称
    /// </summary>
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }

    /// <summary>
    /// 当前AssetBundle依赖的其他AssetBundle列表
    /// </summary>
    [XmlArray("ABDependce")]
    public List<string> ABDependce { get; set; }
}