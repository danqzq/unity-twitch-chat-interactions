using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TwitchIntegration
{
    public class TwitchSettings : ScriptableObject
    {
        public bool initializeOnAwake = true;
        public bool isDebugMode = true;
        
        public string clientId;
        public string commandPrefix = "!";

        public string redirectUri = "http://localhost";
        
        public TwitchCommandsMode commandsMode;

        public List<TwitchCommand> commandList;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var methodCollection = UnityEditor.TypeCache.GetMethodsWithAttribute(typeof(TwitchCommandAttribute));
            var listOfAllCommands = methodCollection.Select(x => 
                x.GetCustomAttribute<TwitchCommandAttribute>()).ToList();

            commandList ??= new List<TwitchCommand>();
            
            var newCommandList = listOfAllCommands.Select(x => 
                commandList.Count(y => y.name == x.Name) == 0 ? 
                    new TwitchCommand(x.Name, x.DefaultCooldown) : 
                    commandList.First(y => y.name == x.Name)).ToList();
            commandList = newCommandList;
        }
#endif
    }
}