using TMPro;
using UnityEngine;

namespace TwitchIntegration.Demo
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _usernameField, _channelNameField;
        [SerializeField] private TextMeshProUGUI _statusText;

        [SerializeField] 
        private string _authSuccess = "Successfully authenticated Twitch account!";
        [SerializeField, Multiline] 
        private string _authRequired = "Not authenticated!";

        public void OnAuthenticateButtonClicked()
        {
            TwitchManager.Authenticate(_usernameField.text, _channelNameField.text, isAuthenticated =>
            {
                _statusText.text = isAuthenticated ? _authSuccess : _authRequired;
            });
        }
    }
}
