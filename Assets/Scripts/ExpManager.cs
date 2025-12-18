using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class ExperimentManager : MonoBehaviour
{
    [Header("1. 引用配置")]
    public ExperimentConfig config; 

    [Header("2. 核心模块")]
    public BaseInputHandler inputHandler; // 你的输入脚本
    public DataRecorder dataRecorder;     // 你的数据记录脚本

    [Header("3. UI 组件绑定")]
    public GameObject containerLeft;
    public GameObject containerRight;
    public Image imgLeft, imgRight;
    public RawImage rawVideoLeft, rawVideoRight;
    public VideoPlayer vpLeft, vpRight;
    public GameObject fixationCross; // 注视点
    public GameObject feedbackPanel; // 选择提示/遮罩

    // 私有状态变量
    private int currentTrialIndex = 0;
    private bool isWaitingForInput = false;
    private List<TrialData> currentSubjectTrials; // 缓存当前被试的列表

    void Start()
    {
        if (config == null || inputHandler == null) 
        { 
            Debug.LogError("Config 或 InputHandler 未绑定！"); 
            return; 
        }

        // 订阅输入事件
        inputHandler.OnSelectionMade += OnUserResponded;

        StartCoroutine(RunExperimentFlow());
    }

    void OnDestroy()
    {
        if(inputHandler != null)
            inputHandler.OnSelectionMade -= OnUserResponded;
    }

    IEnumerator RunExperimentFlow()
    {
        // 1. 获取当前被试的数据
        currentSubjectTrials = config.GetCurrentSubjectTrials();

        if (currentSubjectTrials == null || currentSubjectTrials.Count == 0)
        {
            Debug.LogError($"ID 为 {config.currentSubjectID} 的被试数据为空！请先在 Config 里生成数据。");
            yield break; // 停止运行
        }

        Debug.Log($"实验开始：被试 ID {config.currentSubjectID}，共 {currentSubjectTrials.Count} 个试次。");

        // 2. 循环执行每一个试次
        for (int i = 0; i < currentSubjectTrials.Count; i++)
        {
            currentTrialIndex = i;
            TrialData currentTrial = currentSubjectTrials[i];

            // A. 初始化 (黑屏/清空)
            ResetUI();

            // B. 注视点阶段
            fixationCross.SetActive(true);
            yield return new WaitForSeconds(0.5f); // 注视点固定显示0.5秒
            fixationCross.SetActive(false);

            // C. 刺激呈现阶段 (使用 Config 中的时间参数)
            if (config.mode == PresentationMode.Simultaneous)
            {
                yield return StartCoroutine(PlaySimultaneous(currentTrial));
            }
            else
            {
                yield return StartCoroutine(PlaySequential(currentTrial));
            }

            // D. 响应阶段
            isWaitingForInput = true;
            inputHandler.EnableInput(); // 允许输入
            feedbackPanel.SetActive(true); // 显示"请选择"

            // 开始记录高频数据 (如果Recorder存在)
            if (dataRecorder != null) dataRecorder.StartRecordingTrial(i);

            // 等待直到用户按键 (isWaitingForInput 变为 false)
            while (isWaitingForInput)
            {
                yield return null; 
            }

            // 停止记录高频数据
            if (dataRecorder != null) dataRecorder.StopContinuousRecording();

            // E. 试次间隔 (ITI) - 休息时间
            feedbackPanel.SetActive(false);
            ResetUI();
            
            // 使用 Config 中的 InterTrialInterval
            yield return new WaitForSeconds(config.interTrialInterval);
        }

        Debug.Log("所有试次结束！");
    }

    // --- 并行模式 ---
    IEnumerator PlaySimultaneous(TrialData data)
    {
        LoadContent(data, true, true); // 同时加载 A 和 B
        
        containerLeft.SetActive(true);
        containerRight.SetActive(true);

        // 如果是视频，开始播放
        if(config.mediaType == MediaType.Video)
        {
            vpLeft.Play();
            vpRight.Play();
        }

        // 等待设定的持续时间
        yield return new WaitForSeconds(config.stimulusDuration);

        containerLeft.SetActive(false);
        containerRight.SetActive(false);
    }

    // --- 串行模式 ---
    IEnumerator PlaySequential(TrialData data)
    {
        // 1. 播放 A
        LoadContent(data, true, false); 
        containerLeft.SetActive(true); 
        if (config.mediaType == MediaType.Video) vpLeft.Play();

        yield return new WaitForSeconds(config.stimulusDuration);
        
        containerLeft.SetActive(false);

        // 2. 间隔 (ISI)
        yield return new WaitForSeconds(config.interStimulusInterval);

        // 3. 播放 B
        LoadContent(data, false, true); 
        containerLeft.SetActive(true);
        if (config.mediaType == MediaType.Video) vpLeft.Play();

        yield return new WaitForSeconds(config.stimulusDuration);
        
        containerLeft.SetActive(false);
    }

    // 资源加载逻辑
    void LoadContent(TrialData data, bool loadA, bool loadB)
    {
        if (config.mediaType == MediaType.Image)
        {
            imgLeft.gameObject.SetActive(true);
            imgRight.gameObject.SetActive(true);
            rawVideoLeft.gameObject.SetActive(false);
            rawVideoRight.gameObject.SetActive(false);

            if(loadA && data.imageA != null) imgLeft.sprite = data.imageA;
            if(loadB && data.imageB != null) imgRight.sprite = data.imageB;
        }
        else // Video
        {
            imgLeft.gameObject.SetActive(false);
            imgRight.gameObject.SetActive(false);
            rawVideoLeft.gameObject.SetActive(true);
            rawVideoRight.gameObject.SetActive(true);

            if(loadA && data.videoA != null) 
            { 
                vpLeft.clip = data.videoA; 
                AdjustRenderTexture(vpLeft); // 自动调整分辨率
                vpLeft.Prepare(); 
            }
            if(loadB && data.videoB != null) 
            { 
                vpRight.clip = data.videoB; 
                AdjustRenderTexture(vpRight);
                vpRight.Prepare(); 
            }
        }
    }

    // 用户输入回调
    void OnUserResponded(int choice)
    {
        isWaitingForInput = false;
        
        // 安全检查
        if (currentSubjectTrials != null && currentTrialIndex < currentSubjectTrials.Count)
        {
            TrialData currentData = currentSubjectTrials[currentTrialIndex];
            bool isCorrect = (choice == currentData.correctOption);

            // 保存数据
            if (dataRecorder != null)
                dataRecorder.SaveTrialResult(currentData, choice, isCorrect);
            
            Debug.Log($"Trial {currentTrialIndex} 结果: 选了 {choice}");
        }
    }

    // 自动调整 RenderTexture 大小以匹配视频
    void AdjustRenderTexture(VideoPlayer vp)
    {
        if (vp.clip == null || vp.targetTexture == null) return;
        
        RenderTexture rt = vp.targetTexture;
        if (rt.width != (int)vp.clip.width || rt.height != (int)vp.clip.height)
        {
            rt.Release();
            rt.width = (int)vp.clip.width;
            rt.height = (int)vp.clip.height;
            rt.Create();
        }
    }

    void ResetUI()
    {
        containerLeft.SetActive(false);
        containerRight.SetActive(false);
        fixationCross.SetActive(false);
    }
}