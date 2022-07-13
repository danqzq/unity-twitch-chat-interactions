namespace TwitchIntegration.Utils
{
    public static class TwitchVariables
    {
        #region Versioning
        
        public const string Version = "1.0";
        
        #endregion
        
        #region Paths
        
        internal const string TwitchSettingsPath = "TwitchSettings";
        
        #endregion
        
        #region PlayerPrefs
        
        internal const string TwitchUsernameKey = "TwitchAuth__Username";
        internal const string TwitchChannelNameKey = "TwitchAuth__ChannelName";
        internal const string TwitchOAuthTokenKey = "TwitchAuth__OAuthToken";
        internal const string TwitchAuthenticatedKey = "TwitchAuth__Authenticated";
        
        #endregion

        #region Twitch API
        
        internal const string UserMessageCode = "PRIVMSG";
        internal const string UserJoinCode = "JOIN";
        
        #endregion
    }
}