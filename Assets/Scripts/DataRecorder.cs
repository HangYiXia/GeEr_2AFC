using UnityEngine;
using System.Collections.Generic;
using System;

public class DataRecorder : MonoBehaviour
{
    [Header("引用配置 (必须拖入)")]
    public ExperimentConfig config; // 【新增】引用全局配置，获取被试ID

    [Header("设置")]
    public string saveFolder = "ExperimentData";

    [Header("眼动/VR数据源 (可选)")]
    public Transform vrCamera; // 拖入 Main Camera

    private CSVWriter trialWriter;
    private CSVWriter continuousWriter;
    
    private bool isRecordingContinuous = false;
    private int currentTrialID = -1;
    private float trialStartTime;

    // 缓存当前的被试ID字符串
    private string currentSubjectIDStr; 

    void Start()
    {
        if (config == null)
        {
            Debug.LogError("DataRecorder 缺少 ExperimentConfig 引用！无法获取被试ID。");
            return;
        }

        // 1. 获取当前 Config 设置的 ID (比如 "5")
        currentSubjectIDStr = config.currentSubjectID.ToString();
        
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string finalPath = System.IO.Path.Combine(Application.streamingAssetsPath, saveFolder);

        // --- 初始化试次记录器 (Trial Data) ---
        // 文件名包含 ID： "Subject_5_20231027_Trials.csv"
        trialWriter = new CSVWriter(finalPath, $"Subject_{currentSubjectIDStr}_{timestamp}_Trials.csv");
        
        trialWriter.WriteHeader(
            "Subject_ID",       // 【新增】第几号被试
            "Trial_ID",         // 第几个试次
            "Absolute_Time",    // 【新增】绝对时间戳
            "Presentation_Mode",// 【新增】串行还是并行
            "Stimulus_Left_A",  // 图片/视频A的名字
            "Stimulus_Right_B", // 图片/视频B的名字
            "User_Choice",      // 用户选了什么
            "Is_Correct",       // 正确与否
            "Reaction_Time"     // 反应耗时
        );

        // --- 初始化连续记录器 (Gaze/Head Data) ---
        continuousWriter = new CSVWriter(finalPath, $"Subject_{currentSubjectIDStr}_{timestamp}_Continuous.csv");
        
        continuousWriter.WriteHeader(
            "Subject_ID",       // 【新增】方便合并数据
            "System_Time",      // 绝对时间
            "Trial_ID",         // 当前属于哪个试次
            "Time_Since_Start", // 试次内的时间
            "Head_Pos_X", "Head_Pos_Y", "Head_Pos_Z",
            "Head_Rot_X", "Head_Rot_Y", "Head_Rot_Z"
        );
    }

    // --- 功能 A: 记录每一帧的数据 (高频) ---
    void Update()
    {
        if (isRecordingContinuous && vrCamera != null)
        {
            List<string> rowData = new List<string>();

            // 1. 基础信息
            rowData.Add(currentSubjectIDStr); // Subject_ID
            rowData.Add(System.DateTime.Now.ToString("HH:mm:ss.fff")); // System_Time
            rowData.Add(currentTrialID.ToString()); // Trial_ID
            rowData.Add((Time.time - trialStartTime).ToString("F3")); // Time_Since_Start

            // 2. 头部数据
            Vector3 pos = vrCamera.position;
            Vector3 rot = vrCamera.rotation.eulerAngles;
            
            rowData.Add(pos.x.ToString("F4"));
            rowData.Add(pos.y.ToString("F4"));
            rowData.Add(pos.z.ToString("F4"));
            rowData.Add(rot.x.ToString("F4"));
            rowData.Add(rot.y.ToString("F4"));
            rowData.Add(rot.z.ToString("F4"));

            continuousWriter.WriteRow(rowData);
        }
    }

    // --- 功能 B: 供 Manager 调用的接口 ---

    public void StartRecordingTrial(int trialID)
    {
        currentTrialID = trialID;
        trialStartTime = Time.time;
        isRecordingContinuous = true;
    }

    public void StopContinuousRecording()
    {
        isRecordingContinuous = false;
        currentTrialID = -1;
    }

    // 记录该试次的最终结果
    public void SaveTrialResult(TrialData trial, int userChoice, bool isCorrect)
    {
        float reactionTime = Time.time - trialStartTime;
        string absoluteTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        List<string> row = new List<string>();

        // 必须严格对应 WriteHeader 的顺序
        row.Add(currentSubjectIDStr);             // Subject_ID
        row.Add(currentTrialID.ToString());       // Trial_ID
        row.Add(absoluteTime);                    // Absolute_Time
        row.Add(config.mode.ToString());          // Presentation_Mode (Simultaneous/Sequential)

        // 记录素材名称
        string nameA = (trial.imageA != null) ? trial.imageA.name : (trial.videoA != null ? trial.videoA.name : "null");
        string nameB = (trial.imageB != null) ? trial.imageB.name : (trial.videoB != null ? trial.videoB.name : "null");
        
        row.Add(nameA);
        row.Add(nameB);
        
        row.Add(userChoice == 1 ? "Left/A" : "Right/B");
        row.Add(isCorrect ? "1" : "0");
        row.Add(reactionTime.ToString("F3"));

        trialWriter.WriteRow(row);
    }
}