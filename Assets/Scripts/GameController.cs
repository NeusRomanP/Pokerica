using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TwitchChat;
using UnityEngine;
using UnityEngine.Networking;

public class GameController : MonoBehaviour
{

    private readonly string twitchValidateUrl = "https://id.twitch.tv/oauth2/validate";
    private readonly string twitchAuthUrl = "https://id.twitch.tv/oauth2/authorize";
    private readonly string twitchSettingsUrl = "https://api.twitch.tv/helix/chat/settings";

    private readonly string twitchRedirectUrl = "http://localhost:";
    private readonly string loginSuccessUrl = "https://neusroman.com/pokerica-success";
    private readonly string loginFailUrl = "https://neusroman.com/pokerica-fail";

    private string twitchAuthStateVerify;
    private string authToken = "";

    private string userId;
    private string channelName;

    private bool oauthTokenRetrieved;

    private HttpClient httpClient = new HttpClient();
    private HttpListener httpListener;

    private int freePort;
    private int[] portList = new[]
    {
        12345,
        1234,
        12346
    };

    public static GameController Instance { get; private set; }
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        } 
    }

    void Start()
    {
        TwitchController.onTwitchMessageReceived += OnTwitchMessageReceived;

        TwitchAuth();
    }

    private void Update()
    {
        if (!oauthTokenRetrieved) return;
        
        TwitchController.Login(channelName, new TwitchLoginInfo(channelName, authToken));
        
        UpdateTwitchSettings();
        oauthTokenRetrieved = false;
        InvokeRepeating(nameof(ValidateToken), 3600, 3600);
    }

    public void TwitchAuth()
    {
        List<string> scopes = new List<string>{"chat:read"};

        if (!CheckAvailablePort())
        {
            Application.OpenURL(loginFailUrl);
            return;
        }

        twitchAuthStateVerify = ((Int64)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString();

        string s = "client_id=" + Secrets.CLIENT_ID + "&" +
                "redirect_uri=" + UnityWebRequest.EscapeURL(twitchRedirectUrl+freePort+"/") + "&" +
                "state=" + twitchAuthStateVerify + "&" +
                "response_type=token" + "&" +
                "scope=" + String.Join("+", scopes);

        StartLocalWebserver();

        Application.OpenURL(twitchAuthUrl + "?" + s);
    }

    private void StartLocalWebserver()
    {
        httpListener = new HttpListener();
        
        httpListener.Prefixes.Add(twitchRedirectUrl+freePort+"/");
        httpListener.Start();
        httpListener.BeginGetContext(IncomingHttpRequest, httpListener);
    }

    private void IncomingHttpRequest(IAsyncResult result)
    {
        // fetch the context object
        HttpListenerContext httpContext = httpListener.EndGetContext(result);

        // the context object has the request object for us, that holds details about the incoming request
        HttpListenerRequest httpRequest = httpContext.Request;

        string[] tokens = httpRequest.QueryString.AllKeys;

        if (tokens.Contains("access_token"))
        {
            authToken = httpRequest.QueryString.Get("access_token");
            string state = httpRequest.QueryString.Get("state");

            if (state == twitchAuthStateVerify)
            {
                string responseString = $"<html><body><script>window.location.replace(\"{loginSuccessUrl}\");</script></body></html>";
                ValidateToken(true);
                SendResponse(httpContext, responseString);

                httpListener.Stop();
            }
            else
            {
                string responseString = $"<html><body><script>window.location.replace(\"{loginFailUrl}\");</script></body></html>";
                SendResponse(httpContext, responseString);
            
                httpListener.Stop();
            }
        }else if (tokens.Contains("error"))
        {
            string responseString = $"<html><body><script>window.location.replace(\"{loginFailUrl}\");</script></body></html>";
            SendResponse(httpContext, responseString);
            
            httpListener.Stop();
        }
        else
        {
            string responseString = "<html><head><meta http-equiv='cache-control' content='no-cache'><meta http-equiv='expires' content='0'> <meta http-equiv='pragma' content='no-cache'></head><body><script>var link = window.location.toString(); link = link.replace('#','?'); window.location.replace(link);</script></body></html>";
            SendResponse(httpContext, responseString);
            httpListener.BeginGetContext(IncomingHttpRequest, httpListener);
        }
    }

    private void SendResponse(HttpListenerContext httpContext, string responseString)
    {
        HttpListenerResponse httpResponse = httpContext.Response;

        // build a response to send an "ok" back to the browser for the user to see
        httpResponse = httpContext.Response;
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);

        // send the output to the client browser
        httpResponse.ContentLength64 = buffer.Length;
        System.IO.Stream output = httpResponse.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    private async Task ValidateToken(bool shouldConnectChat = false)
    {
        string apiResponseJson = await CallApi(twitchValidateUrl);
        ApiValidateResponse apiResponseData = JsonUtility.FromJson<ApiValidateResponse>(apiResponseJson);

        userId = apiResponseData.user_id;
        channelName = apiResponseData.login;
        
        if(shouldConnectChat) oauthTokenRetrieved = true;
    }

    private async Task UpdateTwitchSettings()
    {
        string apiUrl = twitchSettingsUrl +
                        "?broadcaster_id" + userId +
                        "&moderator_id" + userId;
        string body = $"{{\"data\": {{\"non_moderator_chat_delay\":false,\"unique_chat_mode\":false}}}}";
        await CallApi(apiUrl, "PATCH", body);
    }

    private async Task<string> CallApi(string endpoint, string method = "GET", string body = "", string[] headers = null)
    {

        int retries = 0;
        
        httpClient.BaseAddress = null;
        httpClient.DefaultRequestHeaders.Clear();

        

        HttpMethod httpMethod = new HttpMethod(method.ToUpperInvariant());
        HttpRequestMessage httpRequest = new HttpRequestMessage(httpMethod, endpoint);

        if (!string.IsNullOrEmpty(body))
        {
            httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrEmpty(authToken))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
        }

        if (!string.IsNullOrEmpty(Secrets.CLIENT_ID))
        {
            httpRequest.Headers.TryAddWithoutValidation("Client-Id", Secrets.CLIENT_ID);
        }

        httpRequest.Headers.TryAddWithoutValidation("Content-Type", "application/json");

        if (headers != null)
        {
            foreach (string header in headers)
            {
                string[] headerParts = header.Split(':');
                if (headerParts.Length >= 2 && !string.IsNullOrWhiteSpace(headerParts[0]) &&
                    !string.IsNullOrWhiteSpace(headerParts[1]))
                {
                    httpRequest.Headers.TryAddWithoutValidation(headerParts[0].Trim(), headerParts[1].Trim());
                }
            }
        }

        while (retries < 3)
        {
            Tuple<HttpStatusCode, string> response = await HttpCall(httpRequest);

            switch (response.Item1)
            {
                case HttpStatusCode.OK: 
                    return response.Item2;
                case HttpStatusCode.Unauthorized:
                    Debug.Log("Unauthorized");
                    break;
                default:
                    break;
            }

            retries++;
        }

        return string.Empty;
    }

    private async Task<Tuple<HttpStatusCode, string>> HttpCall(HttpRequestMessage httpRequest)
    {
        HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();

        string httpResponseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
        return new Tuple<HttpStatusCode, string>(httpResponse.StatusCode, httpResponseContent);
    }

    private bool CheckAvailablePort()
    {

        // Evaluate current system tcp connections. This is the same information provided
        // by the netstat command line application, just in .Net strongly-typed object
        // form.  We will look through the list, and if our port we would like to use
        // in our TcpClient is occupied, we will set isAvailable to false.

        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
        
        foreach (int port in portList)
        {
            if (tcpConnInfoArray.All(x => x.LocalEndPoint.Port != port))
            {
                freePort = port;
                return true;
            }
        }
        return false;
    }
    
    private void OnApplicationQuit()
    {
        httpListener?.Stop();
        httpListener?.Abort();
    }

    void OnDestroy()
    {
        Debug.Log("Destroy");
        TwitchController.onTwitchMessageReceived -= OnTwitchMessageReceived;
    }

    private void OnTwitchMessageReceived(Chatter chatter)
    {
        Debug.Log("Entra");
        Debug.Log(chatter.message);
    }
}
