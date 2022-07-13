using TMPro;
using UnityEngine;

namespace TwitchIntegration.Demo
{
    public class TwitchChatBox : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _chatText;
        [SerializeField] private bool _enableMessages, _enableCommandMessages, _enableJoinEvents;

        private void Start()
        {
            if (_enableMessages)        TwitchManager.OnTwitchMessageReceived  += OnTwitchMessageReceived;
            if (_enableCommandMessages) TwitchManager.OnTwitchCommandReceived  += OnTwitchCommandReceived;
            if (_enableJoinEvents)      TwitchManager.OnTwitchClientJoinedChat += OnTwitchJoinReceived;
        }
        
        private void OnTwitchMessageReceived(TwitchUser user, string message)
        {
            var playerName = user.displayname;
            if (!string.IsNullOrEmpty(user.color))
                playerName = "<color=" + user.color + ">" + playerName + "</color>";
            _chatText.text += $"{playerName}: {message}\n";
        }
        
        private void OnTwitchCommandReceived(TwitchUser user, TwitchCommand command)
        {
            var playerName = user.displayname;
            if (!string.IsNullOrEmpty(user.color))
                playerName = "<color=" + user.color + ">" + playerName + "</color>";
            _chatText.text += $"{playerName}: Calls the {command} command!\n";
        }
        
        private void OnTwitchJoinReceived()
        {
            _chatText.text += "Connected to the chat!\n";
        }
    }
}
