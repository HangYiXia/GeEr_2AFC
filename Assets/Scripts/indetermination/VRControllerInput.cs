public class VRControllerInput : BaseInputHandler
{
    // // 可以在Inspector里配置到底是按扳机键选，还是按X/Y键选
    // public OVRInput.Button leftConfirmBtn = OVRInput.Button.PrimaryIndexTrigger;
    // public OVRInput.Button rightConfirmBtn = OVRInput.Button.SecondaryIndexTrigger;

    // void Update()
    // {
    //     if (!isInputActive) return;

    //     // 检测左手确认键
    //     if (OVRInput.GetDown(leftConfirmBtn, OVRInput.Controller.LTouch))
    //     {
    //         SubmitSelection(1);
    //     }
    //     // 检测右手确认键
    //     else if (OVRInput.GetDown(rightConfirmBtn, OVRInput.Controller.RTouch))
    //     {
    //         SubmitSelection(2);
    //     }
    // }
}