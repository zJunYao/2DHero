using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using UnityEditor.U2D.Sprites;

public class TexturePackerBuild : Editor
{
    private const string BuildAll = "Tools/打包图集";
    private const string BuildOne = "Assets/打包该图集";
    /// <summary>
    /// 输出目录
    /// </summary>
    private const string OutPutDirRoot = "Assets/_Resource/UI/UIAtlas/";
    /// <summary>
    /// 小图目录
    /// </summary>
    private const string OriginalDir = "Assets/_Resource/UI/UISprite";
    /// <summary>
    /// TexturePacker的安装目录
    /// </summary>
    private const string TPInstallDir = "C:\\Program Files\\CodeAndWeb\\TexturePacker\\bin\\TexturePacker.exe";
    [MenuItem(BuildAll)]
    public static void BuildAllTP()
    {
        string inputPath = OriginalDir;
        string commandText = " --sheet {0}.png --data {1}.xml --format sparrow --trim-mode None --pack-mode Best  --algorithm MaxRects --max-size 2048 --size-constraints POT  --disable-rotation --scale 1 {2}";
        string[] imagePath = Directory.GetDirectories(inputPath);
        for (int i = 0; i < imagePath.Length; i++)
        {
            UnityEngine.Debug.Log(imagePath[i]);
            StringBuilder sb = new StringBuilder("");
            string[] fileName = Directory.GetFiles(imagePath[i]);
            GetImageName(fileName, ref sb);
            string name = Path.GetFileName(imagePath[i]);
            string sheetName = OutPutDirRoot + name;
            processCommand(TPInstallDir, string.Format(commandText, sheetName, sheetName, sb.ToString()));
        }
    }
    [MenuItem(BuildOne, false, 2)]
    public static void BuildOneTP()
    {
        var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(AssetDatabase.IsValidFolder).ToList();
        if (paths.Count > 1)
        {
            EditorUtility.DisplayDialog("", "不能同时选择多个目录进行该操作!", "确定");
            return;
        }
        string commandText = " --sheet {0}.png --data {1}.xml --format sparrow --trim-mode None --pack-mode Best  --algorithm MaxRects --max-size 2048 --size-constraints POT  --disable-rotation --scale 1 {2}";
        UnityEngine.Debug.Log(paths[0]);
        StringBuilder sb = new StringBuilder("");
        string[] fileName = Directory.GetFiles(paths[0]);
        GetImageName(fileName, ref sb);
        string name = Path.GetFileName(paths[0]);
        string sheetName = OutPutDirRoot + name;
        processCommand(TPInstallDir, string.Format(commandText, sheetName, sheetName, sb.ToString()));
    }
    private static StringBuilder GetImageName(string[] fileName, ref StringBuilder sb)
    {
        for (int j = 0; j < fileName.Length; j++)
        {
            string extenstion = Path.GetExtension(fileName[j]);
            if (extenstion == ".png")
            {
                sb.Append(fileName[j]);
                sb.Append("  ");
            }
        }
        return sb;
    }
    private static void processCommand(string command, string argument)
    {
        ProcessStartInfo start = new ProcessStartInfo(command);
        start.Arguments = argument;
        start.CreateNoWindow = false;
        start.ErrorDialog = true;
        start.UseShellExecute = false;

        if (start.UseShellExecute)
        {
            start.RedirectStandardOutput = false;
            start.RedirectStandardError = false;
            start.RedirectStandardInput = false;
        }
        else
        {
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.RedirectStandardInput = true;
            start.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
            start.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        }
        Process p = Process.Start(start);
        if (!start.UseShellExecute)
        {
            UnityEngine.Debug.Log(p.StandardOutput.ReadToEnd());
            UnityEngine.Debug.Log(p.StandardError.ReadToEnd());
        }
        p.WaitForExit();
        p.Close();
        AssetDatabase.Refresh();
        BuildTexturePacker();
        ClearOtherFiles();
        AssetDatabase.Refresh();
    }
    /// <summary>
    /// 清理原始图
    /// </summary>
    private static void ClearOtherFiles()
    {
        string[] fileName = Directory.GetFiles(OutPutDirRoot);
        if (fileName != null && fileName.Length > 0)
        {
            for (int i = 0; i < fileName.Length; i++)
            {
                string extenstion = Path.GetExtension(fileName[i]);
                if (extenstion == ".png" || extenstion == ".xml")
                {
                    File.Delete(fileName[i]);
                }
            }
        }

    }

    public static void BuildTexturePacker()
    {
        string[] imagePath = Directory.GetFiles(OutPutDirRoot);
        foreach (string path in imagePath)
        {
            if (Path.GetExtension(path) == ".png" || Path.GetExtension(path) == ".PNG")
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                UnityEngine.Debug.Log(texture.name);
                string rootPath = OutPutDirRoot + texture.name;
                string pngPath = rootPath + "/" + texture.name + ".png";
                TextureImporter asetImp = null;
                Dictionary<string, Vector4> tIpterMap = new Dictionary<string, Vector4>();
                if (Directory.Exists(rootPath))
                {
                    if (File.Exists(pngPath))
                    {
                        UnityEngine.Debug.Log("exite: " + pngPath);
                        asetImp = GetTextureIpter(pngPath);
                        SaveBoreder(tIpterMap, asetImp);
                        File.Delete(pngPath);
                    }
                    File.Copy(OutPutDirRoot + texture.name + ".png", pngPath);
                }
                else
                {
                    Directory.CreateDirectory(rootPath);
                    File.Copy(OutPutDirRoot + texture.name + ".png", pngPath);
                }
                AssetDatabase.Refresh();
                FileStream fs = new FileStream(OutPutDirRoot + texture.name + ".xml", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                string jText = sr.ReadToEnd();
                fs.Close();
                sr.Close();
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(jText);
                XmlNodeList elemList = xml.GetElementsByTagName("SubTexture");
                WriteMeta(elemList, texture.name, tIpterMap);
            }
        }
        AssetDatabase.Refresh();
    }
    //如果这张图集已经拉好了9宫格，需要先保存起来
    static void SaveBoreder(Dictionary<string, Vector4> tIpterMap, TextureImporter tIpter)
    {
        ISpriteEditorDataProvider dataProvider = GetSpriteDataProvider(tIpter);
        if (dataProvider == null)
        {
            UnityEngine.Debug.LogError("无法读取 Sprite 切片数据: " + tIpter?.assetPath);
            return;
        }

        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        for (int i = 0; i < spriteRects.Length; i++)
        {
            tIpterMap[spriteRects[i].name] = spriteRects[i].border;
        }
    }

    static ISpriteEditorDataProvider GetSpriteDataProvider(TextureImporter textureImporter)
    {
        if (textureImporter == null)
        {
            return null;
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(textureImporter);
        dataProvider?.InitSpriteEditorDataProvider();
        return dataProvider;
    }

    static TextureImporter GetTextureIpter(Texture2D texture)
    {
        TextureImporter textureIpter = null;
        string impPath = AssetDatabase.GetAssetPath(texture);
        textureIpter = TextureImporter.GetAtPath(impPath) as TextureImporter;
        return textureIpter;
    }

    static TextureImporter GetTextureIpter(string path)
    {
        TextureImporter textureIpter = null;
        Texture2D textureOrg = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        string impPath = AssetDatabase.GetAssetPath(textureOrg);
        textureIpter = TextureImporter.GetAtPath(impPath) as TextureImporter;
        return textureIpter;
    }
    //写信息到SpritesSheet里
    static void WriteMeta(XmlNodeList elemList, string sheetName, Dictionary<string, Vector4> borders)
    {
        string path = string.Format("{0}{1}/{2}.png",OutPutDirRoot, sheetName, sheetName);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            UnityEngine.Debug.LogError("图集加载失败: " + path);
            return;
        }

        string impPath = AssetDatabase.GetAssetPath(texture);
        TextureImporter asetImp = TextureImporter.GetAtPath(impPath) as TextureImporter;
        if (asetImp == null)
        {
            UnityEngine.Debug.LogError("无法获取图集导入器: " + path);
            return;
        }

        asetImp.textureType = TextureImporterType.Sprite;
        asetImp.spriteImportMode = SpriteImportMode.Multiple;
        asetImp.mipmapEnabled = false;

        ISpriteEditorDataProvider dataProvider = GetSpriteDataProvider(asetImp);
        if (dataProvider == null)
        {
            UnityEngine.Debug.LogError("无法写入 Sprite 切片数据: " + path);
            return;
        }

        Dictionary<string, GUID> oldSpriteIds = new Dictionary<string, GUID>();
        SpriteRect[] oldSpriteRects = dataProvider.GetSpriteRects();
        for (int i = 0; i < oldSpriteRects.Length; i++)
        {
            oldSpriteIds[oldSpriteRects[i].name] = oldSpriteRects[i].spriteID;
        }

        SpriteRect[] spriteRects = new SpriteRect[elemList.Count];
        for (int i = 0, size = elemList.Count; i < size; i++)
        {
            XmlElement node = (XmlElement)elemList.Item(i);
            string spriteName = node.GetAttribute("name");
            int width = int.Parse(node.GetAttribute("width"));
            int height = int.Parse(node.GetAttribute("height"));

            GUID spriteId = oldSpriteIds.TryGetValue(spriteName, out GUID oldSpriteId)
                ? oldSpriteId
                : GUID.Generate();
            Vector4 border = borders.TryGetValue(spriteName, out Vector4 oldBorder)
                ? oldBorder
                : Vector4.zero;

            spriteRects[i] = new SpriteRect
            {
                name = spriteName,
                rect = new Rect(
                    int.Parse(node.GetAttribute("x")),
                    texture.height - int.Parse(node.GetAttribute("y")) - height,
                    width,
                    height
                ),
                pivot = new Vector2(0.5f, 0.5f),
                alignment = SpriteAlignment.Center,
                border = border,
                spriteID = spriteId
            };
        }

        dataProvider.SetSpriteRects(spriteRects);
        ISpriteNameFileIdDataProvider nameFileIdProvider =
            dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIdProvider?.SetNameFileIdPairs(
            spriteRects.Select(spriteRect =>
                new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID))
        );
        dataProvider.Apply();
        asetImp.SaveAndReimport();
    }
}
internal class TextureIpter
{
    public string spriteName = "";
    public Vector4 border = new Vector4();
    public TextureIpter() { }
    public TextureIpter(string spriteName, Vector4 border)
    {
        this.spriteName = spriteName;
        this.border = border;
    }
}
