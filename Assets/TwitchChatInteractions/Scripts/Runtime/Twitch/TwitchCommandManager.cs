using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

using static TwitchIntegration.Utils.TwitchVariables;

namespace TwitchIntegration
{
    public class TwitchCommandManager : MonoBehaviour
    {
        internal bool IsEnabled { get; set; } = true;

        internal static bool IsInitialized { get; private set; }

        internal static bool IsConnected => _twitchClient.Connected;

        internal TwitchCommand[] GetAllAvailableCommands => _settings.commandList.Where(x => x.enabled).ToArray();

        internal List<string> CommandsOnCooldown { get; private set; }

        private static TwitchSettings _settings;

        private static Dictionary<string, MethodInfo> _methodsDict;
        private static Dictionary<MethodInfo, ParameterInfo[]> _methodParameters;

        private static Dictionary<MethodInfo, List<TwitchMonoBehaviour>> _methodBehaviours;
        private static Dictionary<string, List<MethodInfo>> _typeMethods;

        private static Dictionary<string, string> _aliasToCommandName;

        private static TcpClient _twitchClient;
        private static StreamReader _streamReader;
        private static StreamWriter _streamWriter;

        private static bool _isConnecting;
        private static float _timeUntilTimeout;

        private static string _channelName;

        private const float Timeout = 10f;

        internal static void AddBehaviour(TwitchMonoBehaviour behaviour)
        {
            if (!IsInitialized) return;
            var key = behaviour.GetType().Name;
            if (!_typeMethods.ContainsKey(key)) return;
            _typeMethods[key].ForEach(method =>
            {
                if (_methodBehaviours.ContainsKey(method))
                    _methodBehaviours[method].Add(behaviour);
                else
                    _methodBehaviours.Add(method, new List<TwitchMonoBehaviour> { behaviour });
            });
        }

        internal static void RemoveBehaviour(TwitchMonoBehaviour behaviour)
        {
            if (!IsInitialized) return;
            var key = behaviour.GetType().Name;
            if (!_typeMethods.ContainsKey(key)) return;
            _typeMethods[key].ForEach(method =>
            {
                if (_methodBehaviours.ContainsKey(method))
                    _methodBehaviours[method].Remove(behaviour);
            });
        }

        [ContextMenu("Init")]
        public void Init() => Initialize();

        internal static void Initialize()
        {
            if (IsInitialized)
            {
                Log("Twitch commands are already initialized.", "yellow");
                return;
            }

            Log("Initializing Twitch client...", "yellow");

            _methodsDict = new Dictionary<string, MethodInfo>();
            _methodParameters = new Dictionary<MethodInfo, ParameterInfo[]>();
            _methodBehaviours = new Dictionary<MethodInfo, List<TwitchMonoBehaviour>>();
            _typeMethods = new Dictionary<string, List<MethodInfo>>();
            _aliasToCommandName = new Dictionary<string, string>();

            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && typeof(TwitchMonoBehaviour).IsAssignableFrom(x))
                .SelectMany(x => x.GetMethods())
                .Where(x => x.GetCustomAttributes(typeof(TwitchCommandAttribute), false).FirstOrDefault() != null);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<TwitchCommandAttribute>();
                _methodsDict.Add(attr.Name, method);
                if (_typeMethods.ContainsKey(method.DeclaringType!.Name))
                    _typeMethods[method.DeclaringType.Name].Add(method);
                else
                    _typeMethods.Add(method.DeclaringType.Name, new List<MethodInfo> { method });
                foreach (var alias in attr.Aliases)
                    _aliasToCommandName.Add(alias, attr.Name);
                _methodParameters[method] = method.GetParameters();
            }

            IsInitialized = true;
            Log("Initialized! Attempting to connect...", "yellow");

            Connect();
        }

        internal static void Connect()
        {
            _twitchClient = new TcpClient("irc.chat.twitch.tv", 6667);
            _streamReader = new StreamReader(_twitchClient.GetStream());
            _streamWriter = new StreamWriter(_twitchClient.GetStream());

            var oAuth = JsonUtility.FromJson<OAuth>(PlayerPrefs.GetString(TwitchOAuthTokenKey));
            var username = PlayerPrefs.GetString(TwitchUsernameKey);
            _channelName = PlayerPrefs.GetString(TwitchChannelNameKey);

            _streamWriter.WriteLine("PASS oauth:" + oAuth.accessToken);
            _streamWriter.WriteLine("NICK " + username.ToLower());
            _streamWriter.WriteLine("JOIN #" + _channelName.ToLower());
            _streamWriter.WriteLine("CAP REQ :twitch.tv/tags");
            _streamWriter.Flush();

            _timeUntilTimeout = Timeout;
            _isConnecting = true;
        }

        internal static void SendChatMessage(string message)
        {
            if (_streamWriter == null) return;

            _streamWriter.WriteLine("PRIVMSG #" + _channelName.ToLower() + " :" + message);
            _streamWriter.Flush();

            var user = new TwitchUser { displayname = _channelName };
            TwitchManager.OnMessageReceived(user, message);
        }

        private void FixedUpdate()
        {
            if (!_isConnecting) return;
            _timeUntilTimeout -= Time.fixedDeltaTime;
            if (_timeUntilTimeout > 0) return;
            TwitchManager.OnFailedToConnect();
            Log("Connection timed out, retrying... If several attempts fail, refresh your OAuth token", "red");
            Connect();
        }

        private void ReadChat()
        {
            if (_twitchClient.Available <= 0 || !IsEnabled) return;

            var message = _streamReader.ReadLine();
            if (message == null) return;

            //https://dev.twitch.tv/docs/irc/#keepalive-messages
            //The client needs to respond to PING messages, or it will disconnect
            if (message.StartsWith("PING "))
            {
                _streamWriter.WriteLine("PONG " + message.Substring(5));
                _streamWriter.Flush();
            }

            if (message.Contains(UserMessageCode))
                OnMessageReceived(message);
            else if (message.Contains(UserJoinCode))
                OnJoinedToChat();
        }

        private void OnMessageReceived(string message)
        {
            //EXAMPLE (no emotes)
            //@badge-info=;badges=broadcaster/1;client-nonce=0c3f702e8676d24ae22ac5f56706af37;color=#1E90FF;display-name=danqzq;emotes=;
            //flags=;id=9ee20670-8fcf-4a77-ab9c-d7aede171b85;mod=0;room-id=201429480;subscriber=0;tmi-sent-ts=1627719386492;turbo=0;
            //user-id=201429480;user-type= :danqzq!danqzq@danqzq.tmi.twitch.tv PRIVMSG #danqzq :!join

            //EXAMPLE (emotes + text)
            //@badgeinfo=;badges=broadcaster/1;client-nonce=af941b19ca65b5017898ae782d54fafa;color=;display-name-=POLYB1U5;
            //emotes=64138:21-29; <<5th position has emote data, rest is unchanged

            //EXAMPLE (emote only)
            //@badge-info=;badges=broadcaster/1;client-nonce=d97183ae4f5044fe05de4397fe25c27f;color=;display-name=POLYB1U5;
            //emote-only=1;emotes=25:0-4; <<5th position is now emote-only flag, emote data follows, rest is unchanged

            //Split the incoming data from tags and store the raw message
            //(in the case above the raw message would be => danqzq!danqzq@danqzq.tmi.twitch.tv PRIVMSG #danqzq :!join)

            //if the message has emotes, preform a colectomy
            string[] sanitizedMessage = message.Split(';', 8);
            string emoteData = sanitizedMessage[5]; 
            

            if(emoteData != "" && emoteData != "0")
            {
                if (emoteData == "emote-only=1") //check if message is emote only
                {
                    emoteData = sanitizedMessage[6];
                    sanitizedMessage[6] = "emotes=";
                }
                else
                {
                    sanitizedMessage[5] = "emotes=";
                }
                emoteData = emoteData.Split('=', 2)[1]; //<<this string contains emote data
                message = string.Join(";", sanitizedMessage);
            }

            var splitMessage = message.Split(new[] { ':' }, 2, StringSplitOptions.None);
            var rawMessage = splitMessage[1];
            var messageTags = splitMessage[0];

            //Get the typed command from the raw message
            rawMessage = rawMessage.Substring(rawMessage.IndexOf(':', 1) + 1);

            //Parse the tags into a list of strings
            var tags = messageTags.Replace("-", "").Substring(1).Split(';').ToList();
            tags[tags.Count - 1] = tags[tags.Count - 1].Replace(rawMessage, "");

            //Form a json out of the received tags
            var json = "{";
            for (var i = 0; i < tags.Count; i++)
            {
                var entry = tags[i].Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (entry.Length < 2) continue;
                var isDigit = entry[1].All(char.IsDigit);
                if (!isDigit) entry[1] = '"' + entry[1] + '"';
                json += '"' + entry[0] + '"' + ':' + entry[1] + (i == tags.Count - 1 ? ' ' : ',');
            }
            json += "}";

            //Parse the json
            var twitchUser = JsonUtility.FromJson<TwitchUser>(json);

            var playerName = twitchUser.displayname;
            if (!string.IsNullOrEmpty(twitchUser.color))
                playerName = "<color=" + twitchUser.color + ">" + playerName + "</color>";

            Log("Twitch chat - " + playerName + " : " + rawMessage, "white");

            TwitchManager.OnMessageReceived(twitchUser, rawMessage);

            if (!rawMessage.StartsWith(_settings.commandPrefix)) return;

            //Get the command and the arguments from the raw message
            var splitPoint = rawMessage.IndexOf(' ');
            var baseCommand = splitPoint < 0 ? rawMessage.Substring(1) : rawMessage.Substring(1, splitPoint - 1);
            var args = splitPoint < 0 ? new string[] { } : rawMessage.Substring(splitPoint + 1).Split(' ');

            if (_aliasToCommandName.ContainsKey(baseCommand))
                baseCommand = _aliasToCommandName[baseCommand];

            if (!_methodsDict.ContainsKey(baseCommand)) return;

            var commandInfo = _settings.commandList.Find(x => x.name == baseCommand);
            if (!commandInfo.enabled) return;

            if (_settings.commandsMode == TwitchCommandsMode.Cooldown)
            {
                if (CommandsOnCooldown.Contains(baseCommand)) return;
                CommandsOnCooldown.Add(baseCommand);
                StartCoroutine(CooldownCoroutine(baseCommand, commandInfo.cooldown));
            }

            TwitchManager.OnCommandReceived(twitchUser, commandInfo);
            CallCommand(baseCommand, twitchUser, args);
        }

        private static void OnJoinedToChat()
        {
            //EXAMPLE:
            //:danqzq!danqzq@danqzq.tmi.twitch.tv JOIN #danqzq

            Log("Twitch client successfully connected to the chat!", "green");

            TwitchManager.OnJoinedToChat();
            _isConnecting = false;
        }

        private void CallCommand(string commandName, TwitchUser user, IReadOnlyList<string> args)
        {
            var method = _methodsDict[commandName];
            var parameters = _methodParameters[method];

            var filteredArgs = new object[parameters.Length];
            var increment = 0;
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(TwitchUser))
            {
                filteredArgs[0] = user;
                increment = 1;
            }

            if (parameters.Length - increment != args.Count)
                throw new TwitchCommandException("Invalid number of arguments for command: " + commandName);

            for (var i = 0; i < args.Count; i++)
            {
                object value;
                if (parameters[i + increment].ParameterType == typeof(int))
                    value = int.Parse(args[i]);
                else if (parameters[i + increment].ParameterType == typeof(float))
                    value = float.Parse(args[i]);
                else if (parameters[i + increment].ParameterType == typeof(bool))
                    value = bool.Parse(args[i]);
                else if (parameters[i + increment].ParameterType == typeof(string))
                    value = args[i];
                else throw new TwitchCommandException(
                    "Twitch command arguments can only be int, float, bool, or string");
                filteredArgs[i + increment] = value;
            }

            Log("Calling command: " + commandName, "white");

            if (!_methodBehaviours.ContainsKey(method)) return;

            _methodBehaviours[method].ForEach(behaviour => method.Invoke(behaviour, filteredArgs));
        }

        private IEnumerator CooldownCoroutine(string command, float time)
        {
            yield return new WaitForSeconds(time);
            CommandsOnCooldown.Remove(command);
        }

        private static void Log(string message, string color)
        {
            if (_settings.isDebugMode) print($"<color={color}>{message}</color>");
        }

        #region Unity Callbacks

        private void Awake()
        {
            CommandsOnCooldown = new List<string>();
            _settings = Resources.Load<TwitchSettings>(TwitchSettingsPath);
            if (_settings.initializeOnAwake) StartCoroutine(WaitForAuthentication());
        }

        private static IEnumerator WaitForAuthentication()
        {
            yield return new WaitUntil(() => TwitchManager.IsAuthenticated);
            if (!IsInitialized) Initialize();
        }

        private void OnDestroy()
        {
            _streamReader?.Close();
            _streamWriter?.Close();
            _twitchClient?.Close();
        }

        private void Update()
        {
            if (!IsInitialized) return;

            if (!IsConnected)
                Connect();

            ReadChat();
        }

        #endregion
    }
}
