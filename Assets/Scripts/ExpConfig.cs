using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
// 引入 System.Linq 用于列表操作 (如乱序)
using System.Linq; 

[CreateAssetMenu(fileName = "New 2AFC Config", menuName = "2AFC/Configuration")]
public class ExperimentConfig : ScriptableObject
{
    [Header("--- 实验设置 ---")]
    public PresentationMode mode = PresentationMode.Simultaneous;
    public MediaType mediaType = MediaType.Image;

    [Header("--- 自动化生成池 (在此拖入所有素材) ---")]
    public List<Sprite> imagePool;     // 图片池
    public List<VideoClip> videoPool;  // 视频池

    [Header("--- 时间参数 (秒) ---")]
    public float stimulusDuration = 2.0f;
    public float interStimulusInterval = 0.5f;
    public float interTrialInterval = 1.0f;

    [Header("--- 生成结果 (不要手动修改，点击下方按钮生成) ---")]
    public List<TrialData> trials = new List<TrialData>();

    // =========================================================
    //  核心功能：自动生成排列组合
    // =========================================================
    
    // 这个属性会在 Inspector 的组件右上角三点菜单中增加一个选项
    [ContextMenu("一键生成全排列试次 (Generate Trials)")]
    public void GenerateTrials()
    {
        trials.Clear(); // 清空旧数据

        if (mediaType == MediaType.Image)
        {
            GenerateImagePairs();
        }
        else
        {
            GenerateVideoPairs();
        }

        // 打乱顺序 (洗牌)，防止用户猜到规律
        ShuffleTrials();

        Debug.Log($"生成完毕！共生成 {trials.Count} 个试次。");
    }

    void GenerateImagePairs()
    {
        if (imagePool == null || imagePool.Count < 2)
        {
            Debug.LogError("图片池数量不足，至少需要2张图片！");
            return;
        }

        int count = imagePool.Count;
        // 双重循环：i 和 j
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                if (i == j) continue; // 自己不和自己比

                TrialData newTrial = new TrialData();
                newTrial.trialName = $"{imagePool[i].name} vs {imagePool[j].name}";
                newTrial.imageA = imagePool[i];
                newTrial.imageB = imagePool[j];
                
                // 这里默认 correctOption 为 0，因为偏好测试通常没有正确答案
                // 如果是辨别测试，你需要在这里写逻辑判断谁是 Correct
                
                trials.Add(newTrial);
            }
        }
    }

    void GenerateVideoPairs()
    {
        if (videoPool == null || videoPool.Count < 2)
        {
            Debug.LogError("视频池数量不足，至少需要2个视频！");
            return;
        }

        int count = videoPool.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                TrialData newTrial = new TrialData();
                newTrial.trialName = $"{videoPool[i].name} vs {videoPool[j].name}";
                newTrial.videoA = videoPool[i];
                newTrial.videoB = videoPool[j];

                trials.Add(newTrial);
            }
        }
    }

    // Fisher-Yates 洗牌算法
    void ShuffleTrials()
    {
        System.Random rng = new System.Random();
        int n = trials.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            TrialData value = trials[k];
            trials[k] = trials[n];
            trials[n] = value;
        }
    }
}

// 下面这部分保持不变
public enum PresentationMode { Simultaneous, Sequential }
public enum MediaType { Image, Video }

[System.Serializable]
public class TrialData
{
    public string trialName;
    public Sprite imageA;
    public VideoClip videoA;
    public Sprite imageB;
    public VideoClip videoB;
    public int correctOption = 0; 
}