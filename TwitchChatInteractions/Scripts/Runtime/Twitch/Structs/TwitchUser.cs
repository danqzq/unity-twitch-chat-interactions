namespace TwitchIntegration
{
    /// <summary>
    /// Information about a Twitch User.
    /// https://dev.twitch.tv/docs/irc/tags
    /// </summary>
    [System.Serializable]
    public struct TwitchUser
    {
        /// <summary>
        /// <para>Metadata related to the chat badges in the <see cref="badges">badges</see> tag.</para>
        ///
        /// <para>Currently this is used only for <see cref="subscriber">subscriber</see>, to indicate the exact number of months the user has been a subscriber.
        /// This number is finer grained than the version number in badges.
        /// For example, a user who has been a subscriber for 45 months would have a <see cref="badgeInfo">badge-info</see> value of 45
        /// but might have a <see cref="badges">badges</see> version number for only 3 years.</para>
        /// </summary>
        public string badgeInfo;
        
        /// <summary>
        /// Comma-separated list of chat badges and the version of each badge (each in the format `badge/version`, such as `admin/1`).
        /// There are many valid badge values; e.g. admin, bits, broadcaster, global_mod, moderator, subscriber, staff, turbo.
        /// Many badges have only 1 version, but some badges have different versions (images),
        /// depending on how long you hold the badge status; e.g., subscriber.
        /// </summary>
        public string badges;
        
        /// <summary>
        /// Sent only for Bits messages. The amount of cheer/Bits employed by the user. All instances follow the regular expression: /(^\|\s)emote-name\d+(\s\|$)/
        /// (where `emote-name` is an emote name returned by the Get Cheermotes endpoint), should be replaced with the appropriate emote:
        /// <para>static-cdn.jtvnw.net/bits/`theme`/`type`/`color`/`size`</para>
        /// 
        /// <para>`theme` - light or dark:</para>
        /// 
        /// <para>`type`  - animated or static</para>
        ///
        /// <para>`color` - red for 10000+ Bits, blue for 5000-9999, green for 1000-4999, purple for 100-999, gray for 1-99</para>
        ///
        /// <para>`size`  - A digit between 1 and 4</para>
        /// </summary>
        public string bits;
        
        /// <summary>
        /// Hexadecimal RGB color code; the empty string if it is never set.
        /// </summary>
        public string color;
        
        /// <summary>
        /// The user’s display name, escaped as described in the IRCv3 spec. This is empty if it is never set.
        /// </summary>
        public string displayname;
        
        /// <summary>
        /// <para>Information to replace text in the message with emote images. This can be empty. Syntax:
        /// `emote ID`:`first index`-`last index`,`another first index`-`another last index`/`another emote ID`:`first index`-`last index`...</para>
        ///
        /// <para>emote ID - The number to use in this URL:
        /// http://static-cdn.jtvnw.net/emoticons/v1/:`emote ID`/:`size`
        /// (size is 1.0, 2.0 or 3.0.)</para>
        ///
        /// <para>`first index`, `last index` - Character indexes. \001ACTION does not count. Indexing starts from the first character that is part of the user’s actual message.</para>
        /// </summary>
        public string emotes;
        
        /// <summary>
        /// A unique ID for the message.
        /// </summary>
        public string id;

        /// <summary>
        /// The channel ID.
        /// </summary>
        public int roomId;
        
        /// <summary>
        /// 1 if the user has a moderator badge; otherwise, 0.
        /// </summary>
        public int mod;

        /// <summary>
        /// 1 if the user is a subscriber; otherwise, 0.
        /// </summary>
        public int subscriber;
        
        /// <summary>
        /// 1 if the user is turbo; otherwise, 0.
        /// </summary>
        public int turbo;

        /// <summary>
        /// Timestamp when the server received the message.
        /// </summary>
        public ulong tmiSentTs;
        
        /// <summary>
        /// The user’s ID.
        /// </summary>
        public int userId;
        
        public string userType;

        public string clientnonce;

        public string flags;
    }
}