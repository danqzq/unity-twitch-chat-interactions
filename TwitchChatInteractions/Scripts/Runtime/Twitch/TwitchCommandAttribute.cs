using System;

namespace TwitchIntegration
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TwitchCommandAttribute : Attribute
    {
        public string Name { get; }
        
        public string[] Aliases { get; }
        
        public float DefaultCooldown { get; }
        
        public TwitchCommandAttribute(string name, params string[] aliases)
        {
            Name = name;
            Aliases = aliases;
            DefaultCooldown = 0f;
        }
        
        public TwitchCommandAttribute(string name, float defaultCooldown, params string[] aliases)
        {
            Name = name;
            Aliases = aliases;
            DefaultCooldown = defaultCooldown;
        }
    }
}