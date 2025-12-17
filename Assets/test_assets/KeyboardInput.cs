using UnityEngine;

public class KeyboardInput : BaseInputHandler
{
    void Update()
    {
        // 只有当父类允许输入时，才检测按键
        if (!isInputActive) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            SubmitSelection(1); // 提交左
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            SubmitSelection(2); // 提交右
        }
    }
}