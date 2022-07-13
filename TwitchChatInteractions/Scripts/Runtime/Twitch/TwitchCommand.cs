using TwitchIntegration.Utils;

namespace TwitchIntegration
{
    [System.Serializable]
    public class TwitchCommand
    {
        [ReadOnly] public string name;
        public bool enabled;
        [ReadOnly] public float cooldown;
        
        public TwitchCommand()
        {
            name = "";
            enabled = true;
            cooldown = 0;
        }
        
        public TwitchCommand(string name, float cooldown = 0, bool enabled = true)
        {
            this.name = name;
            this.enabled = enabled;
            this.cooldown = cooldown;
        }
    }
}