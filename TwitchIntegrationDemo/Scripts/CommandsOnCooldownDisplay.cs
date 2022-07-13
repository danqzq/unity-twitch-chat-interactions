using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwitchIntegration.Demo
{
    public class CommandsOnCooldownDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject _commandNameDisplayPrefab;
        [SerializeField] private Transform _commandNameDisplayParent;
        
        private void Start() => TwitchManager.OnTwitchCommandReceived += OnTwitchCommandReceived;

        private void OnTwitchCommandReceived(TwitchUser user, TwitchCommand command) => 
            StartCoroutine(UpdateCommandDisplay(command));

        private IEnumerator UpdateCommandDisplay(TwitchCommand command)
        {
            var commandNameDisplay = Instantiate(_commandNameDisplayPrefab, _commandNameDisplayParent);
            var commandNameText = commandNameDisplay.GetComponentInChildren<TextMeshProUGUI>();
            var commandBackground = commandNameDisplay.GetComponentInChildren<Image>();
            commandNameText.text = command.name;

            var time = 0f;
            
            while (time < command.cooldown)
            {
                time += Time.deltaTime;
                commandBackground.fillAmount = Mathf.Lerp(1f, 0f, time / command.cooldown);
                yield return null;
            }
            
            Destroy(commandNameDisplay.gameObject);
        }
    }
}