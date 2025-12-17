using UnityEngine;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class CSVWriter
{
    private string filePath;
    private StreamWriter writer;
    private bool isFileCreated = false;

    public CSVWriter(string directory, string filename)
    {
        // 确保文件夹存在
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // 构造完整路径 (例如: D:/Data/Subject_01_Trials.csv)
        filePath = Path.Combine(directory, filename);
    }

    // 1. 定义表头 (Columns)
    public void WriteHeader(params string[] headers)
    {
        if (File.Exists(filePath))
        {
            // 如果文件已存在，通常不重复写表头，或者你可以选择覆盖
            // 这里我们选择追加模式，所以不做操作，或者你可以删掉旧文件
            // File.Delete(filePath); 
        }

        using (writer = new StreamWriter(filePath, true, Encoding.UTF8))
        {
            string line = string.Join(",", headers);
            writer.WriteLine(line);
        }
        isFileCreated = true;
    }

    // 2. 写入一行数据 (可扩展性体现在这里：传入一个列表即可)
    public void WriteRow(List<string> dataValues)
    {
        // 简单的转义处理：如果内容里有逗号，需要用引号包起来
        for (int i = 0; i < dataValues.Count; i++)
        {
            if (dataValues[i].Contains(","))
                dataValues[i] = $"\"{dataValues[i]}\"";
        }

        string line = string.Join(",", dataValues);

        // 使用 Append 模式写入，写完立刻 Flush，防止崩溃丢数据
        using (writer = new StreamWriter(filePath, true, Encoding.UTF8))
        {
            writer.WriteLine(line);
        }
    }
}