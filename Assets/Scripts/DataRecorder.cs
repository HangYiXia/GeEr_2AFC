using UnityEngine;
using System.Collections.Generic;
using System;

public class DataRecorder : MonoBehaviour
{
    [Header("设置")]
    public string subjectID = "Test_User";
    public string saveFolder = "ExperimentData";

    [Header("眼动/VR数据源 (可选)")]
    public Transform vrCamera; // 拖入 Main Camera
    // public EyeTrackerSDK eyeTracker; // 如果有眼动仪SDK，在这里引用

    private CSVWriter trialWriter;
    private CSVWriter continuousWriter;
    
    private bool isRecordingContinuous = false;
    private int currentTrialID = -1;
    private float trialStartTime;

    void Start()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string finalPath = System.IO.Path.Combine(Application.streamingAssetsPath, saveFolder);

        // --- 1. 初始化试次记录器 (Trial Data) ---
        trialWriter = new CSVWriter(finalPath, $"{subjectID}_{timestamp}_Trials.csv");
        // 定义表头：你想加什么列就在这里加
        trialWriter.WriteHeader(
            "Trial_ID", 
            "Trial_Name", 
            "Condition",
            "Stimulus_Left", 
            "Stimulus_Right", 
            "User_Choice", 
            "Is_Correct", 
            "Reaction_Time"
        );

        // --- 2. 初始化连续记录器 (Gaze/Head Data) ---
        continuousWriter = new CSVWriter(finalPath, $"{subjectID}_{timestamp}_Continuous.csv");
        // 定义高频数据表头
        continuousWriter.WriteHeader(
            "System_Time",      // 绝对时间
            "Trial_ID",         // 当前属于哪个试次
            "Time_Since_Start", // 试次内的时间
            "Head_Pos_X", "Head_Pos_Y", "Head_Pos_Z",
            "Head_Rot_X", "Head_Rot_Y", "Head_Rot_Z"
            // "Gaze_X", "Gaze_Y" // 如果有眼动数据加在这里
        );
    }

    // --- 功能 A: 记录每一帧的数据 (高频) ---
    void Update()
    {
        if (isRecordingContinuous && vrCamera != null)
        {
            List<string> rowData = new List<string>();

            // 1. 时间戳
            rowData.Add(System.DateTime.Now.ToString("HH:mm:ss.fff"));
            rowData.Add(currentTrialID.ToString());
            rowData.Add((Time.time - trialStartTime).ToString("F3"));

            // 2. 头部数据 (Head Tracking)
            Vector3 pos = vrCamera.position;
            Vector3 rot = vrCamera.rotation.eulerAngles;
            
            rowData.Add(pos.x.ToString("F4"));
            rowData.Add(pos.y.ToString("F4"));
            rowData.Add(pos.z.ToString("F4"));
            rowData.Add(rot.x.ToString("F4"));
            rowData.Add(rot.y.ToString("F4"));
            rowData.Add(rot.z.ToString("F4"));

            // 3. (扩展) 眼动数据 - 如果还没SDK，先填空
            // rowData.Add(eyeTracker.GetGazePoint().x.ToString());
            
            continuousWriter.WriteRow(rowData);
        }
    }

    // --- 功能 B: 供 Manager 调用的接口 ---

    // 开始一个新的试次记录
    public void StartRecordingTrial(int trialID)
    {
        currentTrialID = trialID;
        trialStartTime = Time.time;
        isRecordingContinuous = true; // 开始记录高频数据
    }

    // 停止高频记录 (比如在休息阶段)
    public void StopContinuousRecording()
    {
        isRecordingContinuous = false;
        currentTrialID = -1;
    }

    // 记录该试次的最终结果
    public void SaveTrialResult(TrialData trial, int userChoice, bool isCorrect)
    {
        float reactionTime = Time.time - trialStartTime;

        List<string> row = new List<string>();

        // 这里的顺序必须和 Start() 里的 WriteHeader 严格对应
        row.Add(currentTrialID.ToString());
        row.Add(trial.trialName);
        row.Add("Normal"); // Condition
        
        // 记录文件名
        string leftName = (trial.imageA != null) ? trial.imageA.name : trial.videoA.name;
        string rightName = (trial.imageB != null) ? trial.imageB.name : trial.videoB.name;
        
        row.Add(leftName);
        row.Add(rightName);
        
        row.Add(userChoice == 1 ? "Left/A" : "Right/B");
        row.Add(isCorrect ? "1" : "0");
        row.Add(reactionTime.ToString("F3"));

        trialWriter.WriteRow(row);
    }
}