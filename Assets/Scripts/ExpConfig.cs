using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Global_Experiment_Plan", menuName = "2AFC/Global Plan")]
public class ExperimentConfig : ScriptableObject
{
    [Header("--- 1. 基础模式设置 ---")]
    public PresentationMode mode = PresentationMode.Simultaneous; // 并行还是串行
    public MediaType mediaType = MediaType.Image; // 图片还是视频
    public int totalParticipants = 24; // 实验总人数（必须是偶数）

    [Header("--- 2. 时间参数 (秒) ---")]
    [Tooltip("刺激呈现的时间 (图片/视频播放多久)")]
    public float stimulusDuration = 2.0f; 
    
    [Tooltip("两次刺激之间的间隔 (仅串行模式有效)")]
    public float interStimulusInterval = 0.5f;

    [Tooltip("做完选择后的休息缓冲时间")]
    public float interTrialInterval = 1.0f;

    [Header("--- 3. 素材池 (请拖入所有素材) ---")]
    public List<Sprite> imagePool;
    public List<VideoClip> videoPool;

    [Header("--- 4. 运行时设置 ---")]
    [Tooltip("当前正在进行实验的是第几号被试？(1 ~ Total)")]
    public int currentSubjectID = 1;

    [Header("--- 5. 全局生成结果 (自动生成，勿动) ---")]
    // 存储所有人的数据，Index 0 代表 Subject 1
    public List<SubjectSession> allParticipantsData = new List<SubjectSession>();

    // =========================================================
    //  对外接口：获取当前被试的试次列表
    // =========================================================
    public List<TrialData> GetCurrentSubjectTrials()
    {
        // 校验 ID 是否合法
        if (allParticipantsData == null || currentSubjectID < 1 || currentSubjectID > allParticipantsData.Count)
        {
            Debug.LogError($"错误：请求的 Subject ID {currentSubjectID} 超出范围或数据未生成！");
            return new List<TrialData>();
        }
        
        // 列表索引是从0开始的，所以要减1
        return allParticipantsData[currentSubjectID - 1].trials;
    }

    // =========================================================
    //  核心算法：全局平衡生成 (One-Click Logic)
    // =========================================================
    [ContextMenu("生成全局实验计划 (Generate Global Plan)")]
    public void GenerateGlobalPlan()
    {
        if (totalParticipants % 2 != 0)
        {
            Debug.LogError("生成失败：实验人数必须是偶数 (如 12, 24)，才能实现完美平衡！");
            return;
        }

        // 1. 初始化容器
        allParticipantsData.Clear();
        for (int i = 0; i < totalParticipants; i++)
        {
            SubjectSession session = new SubjectSession();
            session.subjectLabel = $"Subject_{i + 1}";
            allParticipantsData.Add(session);
        }

        // 2. 根据类型分发
        if (mediaType == MediaType.Image) GenerateImageDistribution();
        else GenerateVideoDistribution();

        // 3. 打乱每个人内部的顺序 (Shuffle)
        foreach (var session in allParticipantsData)
        {
            ShuffleList(session.trials);
        }

        Debug.Log($"生成完毕！共生成 {totalParticipants} 人份数据，每人 {allParticipantsData[0].trials.Count} 个试次。");
    }

    void GenerateImageDistribution()
    {
        if (imagePool.Count < 2) return;
        // 遍历所有唯一的配对
        for (int i = 0; i < imagePool.Count; i++)
        {
            for (int j = i + 1; j < imagePool.Count; j++)
            {
                DistributeSinglePair(imagePool[i], imagePool[j], null, null);
            }
        }
    }

    void GenerateVideoDistribution()
    {
        if (videoPool.Count < 2) return;
        for (int i = 0; i < videoPool.Count; i++)
        {
            for (int j = i + 1; j < videoPool.Count; j++)
            {
                DistributeSinglePair(null, null, videoPool[i], videoPool[j]);
            }
        }
    }

    // 将一对素材 (A, B) 分发给所有被试
    void DistributeSinglePair(Sprite imgA, Sprite imgB, VideoClip vidA, VideoClip vidB)
    {
        // 制造“指令牌”：一半正序(1)，一半反序(2)
        List<int> orderTokens = new List<int>();
        int half = totalParticipants / 2;
        
        for (int k = 0; k < half; k++)
        {
            orderTokens.Add(1); // Normal: A -> B
            orderTokens.Add(2); // Reverse: B -> A
        }

        // 随机打乱指令牌分配给谁
        ShuffleIntList(orderTokens);

        // 分发
        for (int p = 0; p < totalParticipants; p++)
        {
            int token = orderTokens[p];
            TrialData t1 = new TrialData(); // 这一对里的第一次展示
            TrialData t2 = new TrialData(); // 这一对里的第二次展示
            
            string nameA = (imgA != null) ? imgA.name : vidA.name;
            string nameB = (imgB != null) ? imgB.name : vidB.name;

            if (token == 1) 
            {
                // 该被试先看 A-B，再看 B-A
                t1 = CreateTrialData($"{nameA} vs {nameB}", imgA, imgB, vidA, vidB);
                t2 = CreateTrialData($"{nameB} vs {nameA}", imgB, imgA, vidB, vidA);
            }
            else 
            {
                // 该被试先看 B-A，再看 A-B
                t1 = CreateTrialData($"{nameB} vs {nameA}", imgB, imgA, vidB, vidA);
                t2 = CreateTrialData($"{nameA} vs {nameB}", imgA, imgB, vidA, vidB);
            }

            allParticipantsData[p].trials.Add(t1);
            allParticipantsData[p].trials.Add(t2);
        }
    }

    TrialData CreateTrialData(string name, Sprite a, Sprite b, VideoClip va, VideoClip vb)
    {
        TrialData t = new TrialData();
        t.trialName = name;
        t.imageA = a; t.imageB = b;
        t.videoA = va; t.videoB = vb;
        return t;
    }

    // --- 洗牌算法 ---
    void ShuffleList(List<TrialData> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1) { n--; int k = rng.Next(n + 1); TrialData v = list[k]; list[k] = list[n]; list[n] = v; }
    }
    void ShuffleIntList(List<int> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1) { n--; int k = rng.Next(n + 1); int v = list[k]; list[k] = list[n]; list[n] = v; }
    }
}

// ==========================================
//  自定义数据结构 (必须放在这里)
// ==========================================

[System.Serializable]
public class SubjectSession
{
    public string subjectLabel;
    public List<TrialData> trials = new List<TrialData>();
}

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