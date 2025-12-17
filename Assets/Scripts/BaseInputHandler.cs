using UnityEngine;
using System;

public abstract class BaseInputHandler : MonoBehaviour
{
    // 定义一个事件：当做出选择时触发。参数 int 代表选择的索引 (1=左/A, 2=右/B)
    public event Action<int> OnSelectionMade;

    // 允许输入（例如：视频播放完了，现在允许用户按键）
    public virtual void EnableInput() { isInputActive = true; }

    // 禁止输入（例如：视频正在播放中，按键无效）
    public virtual void DisableInput() { isInputActive = false; }

    protected bool isInputActive = false;

    // 子类调用这个方法来通知管理器
    protected void SubmitSelection(int selectionIndex)
    {
        if (isInputActive)
        {
            OnSelectionMade?.Invoke(selectionIndex);
            DisableInput(); // 防止连点
        }
    }
    public void PublicSubmit(int id) => SubmitSelection(id);
}