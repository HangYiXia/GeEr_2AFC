using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class ExperimentManager : MonoBehaviour
{
    [Header("引用配置")]
    public ExperimentConfig config; // 拖入刚才创建的配置文件

    [Header("输入模块")]
    // 拖入任何继承自 BaseInputHandler 的脚本
    public BaseInputHandler inputHandler;

    [Header("数据记录")]
    public DataRecorder dataRecorder;

    [Header("UI 绑定")]
    public GameObject containerLeft;
    public GameObject containerRight;
    public Image imgLeft, imgRight;
    public RawImage rawVideoLeft, rawVideoRight;
    public VideoPlayer vpLeft, vpRight;
    public GameObject fixationCross;
    public GameObject feedbackPanel; // 比如显示 "请按键选择"

    private int currentTrialIndex = 0;
    private bool isWaitingForInput = false;

    void Start()
    {
        if (config == null) { Debug.LogError("请配置 ExperimentConfig!"); return; }

        inputHandler.OnSelectionMade += OnUserResponded;

        StartCoroutine(RunExperimentFlow());
    }

    void OnDestroy()
    {
        // 养成好习惯，记得取消订阅
        if(inputHandler != null)
            inputHandler.OnSelectionMade -= OnUserResponded;
    }

    IEnumerator RunExperimentFlow()
    {
        // 遍历所有 Trial
        for (int i = 0; i < config.trials.Count; i++)
        {
            currentTrialIndex = i;
            TrialData currentTrial = config.trials[i];

            

            // 1. 初始化状态 (隐藏所有)
            ResetUI();
            
            // 2. 显示注视点 (可选缓冲)
            fixationCross.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            // 3. 根据模式播放刺激
            if (config.mode == PresentationMode.Simultaneous)
            {
                yield return StartCoroutine(PlaySimultaneous(currentTrial));
            }
            else
            {
                yield return StartCoroutine(PlaySequential(currentTrial));
            }

            fixationCross.SetActive(false);
            // 4. 等待用户输入
            isWaitingForInput = true;

            inputHandler.EnableInput();

            feedbackPanel.SetActive(true); // 提示用户可以选择了
            
            dataRecorder.StartRecordingTrial(i);

            // 挂起协程，直到 Update 中接收到输入
            while (isWaitingForInput)
            {
                yield return null; 
            }

            dataRecorder.StopContinuousRecording();

            // 5. 试次间隔 (ITI)
            feedbackPanel.SetActive(false);
            ResetUI();
            yield return new WaitForSeconds(config.interTrialInterval);
        }

        Debug.Log("实验结束！");
        // TODO: 导出数据到 CSV
    }

    // --- 并行模式逻辑 ---
    IEnumerator PlaySimultaneous(TrialData data)
    {
        LoadContent(data, true, true); // 加载 A 和 B
        
        containerLeft.SetActive(true);
        containerRight.SetActive(true);

        if(config.mediaType == MediaType.Video)
        {
            vpLeft.Play();
            vpRight.Play();
        }

        yield return new WaitForSeconds(config.stimulusDuration);

        // 时间到，隐藏
        containerLeft.SetActive(false);
        containerRight.SetActive(false);
    }

    // --- 串行模式逻辑 ---
    IEnumerator PlaySequential(TrialData data)
    {
        // 播放 A (First)
        LoadContent(data, true, false); // 只加载 A
        containerLeft.SetActive(true); // 假设这里用左边容器代表“第一个”
        if (config.mediaType == MediaType.Video) vpLeft.Play();

        yield return new WaitForSeconds(config.stimulusDuration);
        
        containerLeft.SetActive(false);

        // 间隔 (ISI)
        yield return new WaitForSeconds(config.interStimulusInterval);

        // 播放 B (Second)
        LoadContent(data, false, true); // 只加载 B
        containerLeft.SetActive(true); // 或者是 containerRight，取决于你想显示在同一个位置还是不同位置
        // 如果是 Sequential，通常是在屏幕正中央先后显示，这里为了简单先复用逻辑
        if (config.mediaType == MediaType.Video) vpLeft.Play(); // 复用 VideoPlayer

        yield return new WaitForSeconds(config.stimulusDuration);
        
        containerLeft.SetActive(false);
    }

    // 辅助：加载图片或准备视频
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

            if(loadA && data.videoA != null) { 
                vpLeft.clip = data.videoA; 
                // 【新增】让纹理大小自动匹配视频大小
                AdjustRenderTexture(vpLeft);
                vpLeft.Prepare(); 
            }
            if(loadB && data.videoB != null) { 
                vpRight.clip = data.videoB; 
                AdjustRenderTexture(vpRight);
                vpRight.Prepare(); 
            }
        }
    }

    void OnUserResponded(int choice)
    {
        isWaitingForInput = false;
        
        // 记录数据
        TrialData currentData = config.trials[currentTrialIndex];
        bool isCorrect = (choice == currentData.correctOption);
        dataRecorder.SaveTrialResult(currentData, choice, isCorrect);
        
        Debug.Log($"Trial {currentTrialIndex}: 用户选择了 {choice}, 正确与否: {isCorrect}");
        
        // 这里可以调用一个 DataRecorder 脚本把结果存下来
    }

    void ResetUI()
    {
        containerLeft.SetActive(false);
        containerRight.SetActive(false);
        fixationCross.SetActive(false);
    }

    void AdjustRenderTexture(VideoPlayer vp)
    {
        // 获取 VideoPlayer 绑定的 Render Texture
        RenderTexture rt = vp.targetTexture;
        
        // 如果视频资源存在
        if (vp.clip != null && rt != null)
        {
            // 如果纹理大小 和 视频原片大小 不一致，就改一下
            if (rt.width != (int)vp.clip.width || rt.height != (int)vp.clip.height)
            {
                // 释放旧的内存
                rt.Release();
                // 设置新大小
                rt.width = (int)vp.clip.width;
                rt.height = (int)vp.clip.height;
                // 重新创建
                rt.Create();
                Debug.Log($"已自动调整 RT 大小为: {rt.width}x{rt.height}");
            }
        }
    }
}