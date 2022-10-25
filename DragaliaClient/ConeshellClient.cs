using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using DragaliaClient.Models;
using LibConeshell;
using MessagePack;
using Org.BouncyCastle.Crypto.Parameters;

namespace DragaliaClient;

public class ConeshellClient
{
    private readonly HttpClient _client;
    private long _requestId;
    private readonly X25519PublicKeyParameters _serverPublicKey;
    private readonly string _idToken;
    private readonly byte[] _clientId;
    private string _sessionId;
    private string _deployHash;
    private string _resourceVersion;

    public ConeshellClient(X25519PublicKeyParameters serverPublicKey, string idToken, string userId)
    {
        _client = new HttpClient();
        _serverPublicKey = serverPublicKey;
        _idToken = idToken;
        _sessionId = "";
        _deployHash = "";
        _resourceVersion = "";
        _clientId = MD5.HashData(Encoding.UTF8.GetBytes(userId));
        _requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        _client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("deflate, gzip");
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.UnityUserAgent);
        _client.DefaultRequestHeaders.Add("Device", Constants.Device);
        _client.DefaultRequestHeaders.Add("Platform", Constants.Platform);
        _client.DefaultRequestHeaders.Add("App-Ver", Constants.AppVer);
        _client.DefaultRequestHeaders.Add("OS-Version", Constants.OsVersion);
        _client.DefaultRequestHeaders.Add("GraphicsDeviceName", Constants.Graphics);
        _client.DefaultRequestHeaders.Add("X-Unity-Version", Constants.UnityVersion);
        _client.DefaultRequestHeaders.Add("DeviceId", "");
        _client.DefaultRequestHeaders.Add("DeviceName", "iPhone");
        _client.DefaultRequestHeaders.Add("Carrier", "Apple");
    }

    public void SetSessionId(string sessionId)
        => _sessionId = sessionId;

    public void SetDeployHash(string deployHash)
        => _deployHash = deployHash;

    public void SetResourceVersion(string resourceVersion)
        => _resourceVersion = resourceVersion;

    public bool Login(string deviceUuid)
    {
        try
        {
            var pushNotificationUpdateData =
                new ConeshellRequests.PushNotificationUpdateSettingRequest("US", deviceUuid, "en_us");

            var pushNotificationUpdateResponseData =
                Send<ConeshellRequests.PushNotificationUpdateSettingRequest,
                    ConeshellRequests.PushNotificationUpdateSettingResponse>(pushNotificationUpdateData,
                    Constants.PushNotificationUpdate);

            var toolAuthRequestData = new ConeshellRequests.ToolAuthRequest(deviceUuid, _idToken);
            /*var transitionResponseData =
                coneshellClient.Send<ConeshellRequests.ToolAuthRequest, ConeshellRequests.TransitionByNAccountResponse>(
                    toolAuthRequestData, Constants.TransitionByNAccount);*/

            var toolAuthResponseData =
                Send<ConeshellRequests.ToolAuthRequest, ConeshellRequests.ToolAuthResponse>(
                    toolAuthRequestData, Constants.ToolAuth);

            _sessionId = toolAuthResponseData.data.session_id;

            var deployHashResponse =
                Send<ConeshellRequests.RequestCommon, ConeshellRequests.DeployGetDeployVersionResponse>(
                    new ConeshellRequests.RequestCommon(), Constants.GetDeployVersion);

            _deployHash = deployHashResponse.data.deploy_hash;

            var resourceVersionRequest = new ConeshellRequests.VersionGetResourceVersionRequest(1, Constants.AppVer);
            var resourceVersionResponse =
                Send<ConeshellRequests.VersionGetResourceVersionRequest, ConeshellRequests.VersionGetResourceVersionResponse>(
                    resourceVersionRequest, Constants.GetResourceVersion);

            _resourceVersion = resourceVersionResponse.data.resource_version;

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    private long GetRequestId()
    {
        _requestId++;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return (_requestId & 0xffffff) | currentTime << 24;
    }

    public TResponse Send<TRequest, TResponse>(TRequest req, string url, bool shouldEncrypt = true) 
        where TRequest : ConeshellRequests.RequestCommon
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Request-Token", GetRequestId().ToString());
        httpRequest.Headers.Add("Request-Time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        if (_sessionId == "")
            httpRequest.Headers.Add("ID-TOKEN", _idToken);
        else
            httpRequest.Headers.Add("SID", _sessionId);

        if (_deployHash != "")
            httpRequest.Headers.Add("Deploy-Hash", _deployHash);

        if (_resourceVersion != "")
            httpRequest.Headers.Add("Res-Ver", _resourceVersion);

        var serializedData = MessagePackSerializer.Serialize(req);

        var secret = Array.Empty<byte>();

        if (shouldEncrypt)
        {
            (var encryptedData, secret) = Coneshell.EncryptRequestMessage(serializedData, _serverPublicKey, _clientId);
            var base64Data = Convert.ToBase64String(encryptedData);
            httpRequest.Content = new StringContent(base64Data, Encoding.UTF8, "application/octet-stream");
        }
        else
        {

            httpRequest.Content = new ByteArrayContent(serializedData);
        }

        var httpResponse = _client.Send(httpRequest);
        httpResponse.EnsureSuccessStatusCode();

        byte[] serializedResponseData;

        if (shouldEncrypt)
        {
            var base64ResponseData = httpResponse.Content.ReadAsStringAsync().Result;
            var encryptedResponseData = Convert.FromBase64String(base64ResponseData);
            serializedResponseData = Coneshell.DecryptResponseMessage(encryptedResponseData, secret, _clientId);
        }
        else
        {
            serializedResponseData = httpResponse.Content.ReadAsByteArrayAsync().Result;
        }

        if (typeof(TResponse) == typeof(byte[]))
            return (TResponse)(object)serializedResponseData;

        var responseData = MessagePackSerializer.Deserialize<TResponse>(serializedResponseData);
        return responseData;
    }
}