using TMPro;

namespace TwitchIntegration.Demo
{
    public class TwitchMessageSender : TwitchMonoBehaviour
    {
        public void SendMessage(TMP_InputField inputField)
        {
            TwitchManager.SendChatMessage(inputField.text);
            inputField.text = "";
        }
    }
}