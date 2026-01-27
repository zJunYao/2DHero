using System;
using System.IO;

/// <summary>
/// CRC32校验类，用于计算数据的CRC32值
/// </summary>
public static class CRC32
{
    // CRC32多项式表
    private static readonly uint[] _crc32Table;

    /// <summary>
    /// 静态构造函数，初始化CRC32多项式表
    /// </summary>
    static CRC32()
    {
        // 标准CRC32多项式
        uint polynomial = 0xEDB88320u;
        _crc32Table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                {
                    crc = (crc >> 1) ^ polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }
            _crc32Table[i] = crc;
        }
    }

    /// <summary>
    /// 计算字节数组的CRC32值
    /// </summary>
    /// <param name="bytes">要计算的字节数组</param>
    /// <returns>CRC32校验值</returns>
    public static uint Calculate(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return 0;
        }

        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < bytes.Length; i++)
        {
            byte index = (byte)((crc & 0xFF) ^ bytes[i]);
            crc = (_crc32Table[index] ^ (crc >> 8));
        }
        return crc ^ 0xFFFFFFFFu;
    }

    /// <summary>
    /// 计算字符串的CRC32值
    /// </summary>
    /// <param name="str">要计算的字符串</param>
    /// <returns>CRC32校验值</returns>
    public static uint Calculate(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return 0;
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        return Calculate(bytes);
    }

    /// <summary>
    /// 计算文件的CRC32值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>CRC32校验值</returns>
    public static uint CalculateFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("文件不存在", filePath);
        }

        uint crc = 0xFFFFFFFFu;
        byte[] buffer = new byte[4096];

        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    byte index = (byte)((crc & 0xFF) ^ buffer[i]);
                    crc = (_crc32Table[index] ^ (crc >> 8));
                }
            }
        }

        return crc ^ 0xFFFFFFFFu;
    }
}