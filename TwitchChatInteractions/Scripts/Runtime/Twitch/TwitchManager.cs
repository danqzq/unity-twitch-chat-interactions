using System.Collections.Generic;
using UnityEngine;

namespace TwitchIntegration
{
    public static class TwitchManager
    {
        public static bool IsAuthenticated => _authenticator.IsAuthenticated;
        
        public static bool IsInitialized => TwitchCommandManager.IsInitialized;
        
        public static bool IsConnected => TwitchCommandManager.IsConnected;

        /// <summary>
        /// Attempts to authenticate the user's channel with your game's Twitch client
        /// by opening a special authorization web page in the user's browser.
        /// </summary>
        /// <param name="username">The user's Twitch username.</param>
        /// <param name="channelName">The user's Twitch channel name (usually the same as the username).</param>
        /// <param name="onComplete">A callback that gets triggered when authentication is finished
        /// (returns true if the user was authenticated, false if not).</param>
        public static void Authenticate(string username, string channelName, System.Action<bool> onComplete = null) =>
            _authenticator.TryAuthenticate(username, channelName, onComplete);
        
        public static void Initialize() => TwitchCommandManager.Initialize();
        
        public static void Connect() => TwitchCommandManager.Connect();
        
        public static void EnableCommands(bool on) => _commandManager.IsEnabled = on;

        /// <summary>
        /// Returns all commands that are enabled and can be used.
        /// </summary>
        public static TwitchCommand[] GetAllAvailableCommands => _commandManager.GetAllAvailableCommands;
        
        /// <summary>
        /// Returns a list of commands that are currently in cooldown.
        /// </summary>
        public static List<string> CommandsOnCooldown => _commandManager.CommandsOnCooldown;

        /// <summary>
        /// Gets triggered when the Twitch client connects to the chat.
        /// </summary>
        public static event System.Action OnTwitchClientJoinedChat;
        
        /// <summary>
        /// Gets triggered when a message is received from the Twitch chat.
        /// </summary>
        public static event System.Action<TwitchUser, string> OnTwitchMessageReceived;
        
        /// <summary>
        /// Gets triggered when a command is received from the Twitch chat.
        /// </summary>
        public static event System.Action<TwitchUser, TwitchCommand> OnTwitchCommandReceived;
        
        /// <summary>
        /// Gets triggered when the Twitch client fails to connect to the chat.
        /// </summary>
        public static event System.Action OnTwitchClientFailedToConnect;

        private static TwitchAuthenticator _authenticator;
        private static TwitchCommandManager _commandManager;
        
        [RuntimeInitializeOnLoadMethod]
        private static void CreateInstance()
        {
            var gameObject = new GameObject("TwitchManager");
            _authenticator = gameObject.AddComponent<TwitchAuthenticator>();
            _commandManager = gameObject.AddComponent<TwitchCommandManager>();
            Object.DontDestroyOnLoad(_authenticator);
        }
        
        internal static void OnJoinedToChat() => OnTwitchClientJoinedChat?.Invoke();
        internal static void OnMessageReceived(TwitchUser user, string message) => OnTwitchMessageReceived?.Invoke(user, message);
        internal static void OnCommandReceived(TwitchUser user, TwitchCommand command) => OnTwitchCommandReceived?.Invoke(user, command);
        internal static void OnFailedToConnect() => OnTwitchClientFailedToConnect?.Invoke();
    }
}