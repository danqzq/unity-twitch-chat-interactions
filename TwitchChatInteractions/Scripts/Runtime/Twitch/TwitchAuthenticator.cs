using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using static TwitchIntegration.Utils.TwitchVariables;

namespace TwitchIntegration
{
    public class TwitchAuthenticator : MonoBehaviour
    {
        private TwitchSettings _settings;

        private static HttpListener _listener;
        private static Thread _listenerThread;

        private OAuth _oauth;

        public bool IsAuthenticated { get; private set; }

        private const float Timeout = 10.0f;

        private void Awake()
        {
            _settings = Resources.Load<TwitchSettings>(TwitchSettingsPath);
            IsAuthenticated = CheckAuthenticationStatus();
        }
        
        private bool CheckAuthenticationStatus()
        {
            if (!PlayerPrefs.HasKey(TwitchOAuthTokenKey))
            {
                Log("Twitch client unauthenticated", "yellow");
                return false;
            }
            
            try
            {
                _oauth = JsonUtility.FromJson<OAuth>(PlayerPrefs.GetString(TwitchOAuthTokenKey));
                
                if (string.IsNullOrEmpty(_oauth.accessToken))
                    throw new TwitchCommandException("Invalid Twitch client access token");
                
                IsAuthenticated = true;
                Log("Twitch client authenticated", "green");
            }
            catch (TwitchCommandException e)
            {
                Log(e.Message, "red");
            }

            return IsAuthenticated;
        }

        internal void TryAuthenticate(string username, string channelName, Action<bool> onComplete)
        {
            PlayerPrefs.SetString(TwitchUsernameKey, username);
            PlayerPrefs.SetString(TwitchChannelNameKey, channelName);
            StartCoroutine(TryAuthenticateCoroutine(onComplete));
        }

        private IEnumerator TryAuthenticateCoroutine(Action<bool> onComplete)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost/");
            _listener.Prefixes.Add("http://127.0.0.1/");
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Start();
            
            _listenerThread = new Thread(StartListener);
            _listenerThread.Start();
            
            IsAuthenticated = false;

            var url = "https://id.twitch.tv/oauth2/authorize?client_id=" + _settings.clientId +
                           "&redirect_uri=" + _settings.redirectUri + "&response_type=token&scope=chat:read";
            
#if UNITY_WEBGL
            var webglURL = string.Format("window.open(\"{0}\")", url);
            Application.ExternalEval(webglURL);
#else
            Application.OpenURL(url);
#endif

            var processStartTime = Time.realtimeSinceStartup;
            while (!IsAuthenticated)
            {
                var elapsedTime = Time.realtimeSinceStartup - processStartTime;
                if (elapsedTime >= Timeout)
                {
                    Log("Authentication timed out", "red");
                    onComplete(false);
                    yield break;
                }
                yield return null;
            }
            onComplete?.Invoke(IsAuthenticated);
            _listener.Stop();
            _listener.Close();
            PlayerPrefs.SetString(TwitchOAuthTokenKey, JsonUtility.ToJson(_oauth));
            if (!PlayerPrefs.HasKey(TwitchAuthenticatedKey)) PlayerPrefs.SetInt(TwitchAuthenticatedKey, 1);
        }
        
        private void StartListener()
        {
            while (true)
            {
                if (IsAuthenticated) return;
                var result = _listener.BeginGetContext(GetContextCallback, _listener);
                result.AsyncWaitHandle.WaitOne();
            }
        }
        
        private void GetContextCallback(IAsyncResult asyncResult)
        {
            var context = _listener.EndGetContext(asyncResult);
            
            if (context.Request.HttpMethod == "POST")
            {
                var dataText = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                _oauth = JsonUtility.FromJson<OAuth>(dataText);
                IsAuthenticated = true;
            }

            _listener.BeginGetContext(GetContextCallback, null);

            const string responseHtmlPage = @"<html><head>
<script src=""https://unpkg.com/axios/dist/axios.min.js""></script>
<script>if (window.location.hash) {
    let fragments = window.location.hash.substring(1).split('&').map(x => x.split('=')[1]);

    let data = {
        accessToken: fragments[0],
        scope: fragments[1],
        state: fragments[2]
    };

    axios.post('/', data).then(function(response) {console.log(response); window.close();}).catch(function(error) {console.log(error); window.close();});
}
</script></head>";

            var buffer = Encoding.UTF8.GetBytes(responseHtmlPage);
            var response = context.Response;
            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private void Log(string message, string color)
        {
            if (_settings.isDebugMode) print($"<color={color}>{message}</color>");
        }
    }
}