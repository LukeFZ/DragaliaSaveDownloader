using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DragaliaClient.Models;
using Org.BouncyCastle.Crypto.Parameters;

namespace DragaliaClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Creating device account...");
            var (nintendoSessionId, deviceUserId, deviceAccount) = GetDeviceAccount();
            Console.WriteLine("Created device account!");

            Console.WriteLine("Waiting for user login...");
            var nintendoUserIdToken = DoNintendoLogin();
            Console.WriteLine("Got user login!");

            Console.WriteLine("Trying to login with device and user token...");
            var (transferredIdToken, transferredUserId) = AssociateWithDevice(nintendoSessionId, nintendoUserIdToken,
                deviceUserId, deviceAccount);
            Console.WriteLine("Successfully authenticated!");

            var officialServerPublicKey =
                new X25519PublicKeyParameters(Convert.FromHexString(Constants.OfficialServerEncPublicKey));

            var coneshellClient = new ConeshellClient(officialServerPublicKey, transferredIdToken, transferredUserId);

            var uuid = Guid.NewGuid().ToString();

            Console.WriteLine("Trying to login to the game server...");
            if (coneshellClient.Login(uuid))
            {
                Console.WriteLine("Successfully logged in!");
                Console.WriteLine("Downloading player data...");

                SaveResponse(coneshellClient, "savedata.txt", Constants.LoadIndex);
                SaveResponse(coneshellClient, "missionlist.txt", Constants.GetMissionList);
                SaveResponse(coneshellClient, "endeavour.txt", Constants.DmodeGetData);

                Console.WriteLine("Finished! Press any key to exit.");
            }
            else
            {
                Console.WriteLine("Failed to login.");
            }

            Console.ReadKey();
        }

        private static void SaveResponse(ConeshellClient client, string filename, string url)
        {
            var loadIndexResponseData = client.Send<ConeshellRequests.RequestCommon, dynamic>(new ConeshellRequests.RequestCommon(), url);
            var jsonLoadIndex = JsonSerializer.Serialize(loadIndexResponseData,
                new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true });

            File.WriteAllText(filename, jsonLoadIndex);
            Console.WriteLine($"Saved {filename}.");
        }

        private static (string sessionId, string deviceUserId, DeviceAccount deviceAccount) GetDeviceAccount()
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.NintendoUserAgent);
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Constants.AcceptLanguage);

            var loginRequestData = new BaaSRequest(
                "2.19.0",
                Utils.GenerateAssertion(),
                "unknown",
                null,
                "",
                "unknown",
                null,
                "en-US",
                "unknown",
                "wifi",
                "iOS",
                "14.3",
                null,
                "Unity-2.33.0-0a4be7c8",
                null,
                "Europe/Berlin",
                7200000
            );

            var loginRequest = new HttpRequestMessage(HttpMethod.Post, Constants.BaaSLogin);
            loginRequest.Content = JsonContent.Create(loginRequestData, options: new JsonSerializerOptions(JsonSerializerDefaults.Web) {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});

            var loginResponse = httpClient.Send(loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            var loginResponseData = loginResponse.Content.ReadFromJsonAsync<BaaSLoginResponse>().Result;
            if (loginResponseData?.User == null)
            {
                Console.WriteLine("Failed to create new device account.");
                Console.ReadKey(); Environment.Exit(-1);
            }

            return (loginResponseData.SessionId, loginResponseData.User!.Id, loginResponseData.CreatedDeviceAccount);
        }

        private static (string transferredIdToken, string transferredUserId) AssociateWithDevice(string sessionId, string idToken, string deviceUserId,
            DeviceAccount device)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.NintendoUserAgent);
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Constants.AcceptLanguage);

            var loginRequestData = new BaaSRequest(
                "2.19.0",
                Utils.GenerateAssertion(),
                "unknown",
                device,
                "",
                "unknown",
                new IdpAccount(idToken),
                "en-US",
                "unknown",
                "wifi",
                "iOS",
                "14.3",
                deviceUserId,
                "Unity-2.33.0-0a4be7c8",
                sessionId,
                "Europe/Berlin",
                7200000
            );

            var loginRequest = new HttpRequestMessage(HttpMethod.Post, Constants.BaaSFederation);
            loginRequest.Content = JsonContent.Create(loginRequestData);

            var loginResponse = httpClient.Send(loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            var loginResponseData = loginResponse.Content.ReadFromJsonAsync<BaaSLoginResponse>().Result;
            if (loginResponseData?.User == null)
            {
                Console.WriteLine("Failed to associate device with Nintendo account.");
                Console.ReadKey(); Environment.Exit(-1);
            }

            return (loginResponseData.IdToken, loginResponseData.User.Id);
        }

        private static string DoNintendoLogin()
        {
            var challenge = Utils.GetRandomString();

            var oauthParameters = new Dictionary<string, string>
            {
                {"client_id", Constants.ClientId},
                {"redirect_uri", Constants.RedirectUri},
                {"response_type", Constants.ResponseType},
                {"scope", Constants.Scope},
                {"session_token_code_challenge", Utils.HashString(challenge)},
                {"session_token_code_challenge_method", Constants.SessionCodeChallengeMethod},
                {"state", Utils.GetRandomString()}
            };

            var oauthLoginUrl = Constants.Authorize + Utils.UrlEncode(oauthParameters);

            var oauthToken = DoOAuthFlow(oauthLoginUrl);
            if (oauthToken == "")
            {
                Console.WriteLine("Failed to acquire OAuth token.");
                Console.ReadKey(); Environment.Exit(-1);
            }

            var sessionTokenCode = oauthToken.Split("&").First(entry => entry.StartsWith("session_token_code"))[19..];

            var sessionTokenParameters = new Dictionary<string, string>
            {
                {"client_id", Constants.ClientId},
                {"session_token_code", sessionTokenCode},
                {"session_token_code_verifier", challenge}
            };

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Constants.NintendoUserAgent);
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(Constants.AcceptLanguage);

            var sessionTokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.SessionToken);
            sessionTokenRequest.Content = new FormUrlEncodedContent(sessionTokenParameters);

            var sessionTokenResponse = httpClient.Send(sessionTokenRequest);
            sessionTokenResponse.EnsureSuccessStatusCode();
            var sessionTokenInfo = sessionTokenResponse.Content.ReadFromJsonAsync<SessionTokenResponse>().Result;
            if (sessionTokenInfo == null)
            {
                Console.WriteLine("Failed to acquire session token.");
                Console.ReadKey(); Environment.Exit(-1);
            }

            var sessionToken = sessionTokenInfo.session_token;

            var sdkTokenRequestData = new SdkTokenRequest(Constants.ClientId, sessionToken);

            var sdkTokenRequest = new HttpRequestMessage(HttpMethod.Post, Constants.SdkToken);
            sdkTokenRequest.Content = JsonContent.Create(sdkTokenRequestData,
                MediaTypeHeaderValue.Parse("application/json; charset=utf-8"));

            var sdkTokenResponse = httpClient.Send(sdkTokenRequest);
            sdkTokenResponse.EnsureSuccessStatusCode();
            var sdkTokenInfo = sdkTokenResponse.Content.ReadFromJsonAsync<SdkTokenResponse>().Result;
            if (sdkTokenInfo == null)
            {
                Console.WriteLine("Failed to acquire sdk token.");
                Console.ReadKey(); Environment.Exit(-1);
            }

            var userId = sdkTokenInfo.User.Id;
            var idToken = sdkTokenInfo.IdToken;

            #if DEBUG
            Console.WriteLine($"BaaS User Id: {sdkTokenInfo.User.Id} | ID Token: {sdkTokenInfo.IdToken}");
            #endif

            return idToken;
        }

        private static string DoOAuthFlow(string oauthUrl)
        {
            Console.WriteLine("Please follow the following steps:");
            Console.WriteLine($"1. Open this URL in a new browser window: {oauthUrl}");
            Console.WriteLine(
                "2. Once you arrive at the 'Select the account' page, right-click the red selection button and choose 'Copy Url'.");
            Console.WriteLine("3. Paste the copied URL into this console window and press enter.");

            string submittedUrl;

            while (true)
            {
                submittedUrl = Console.ReadLine();
                if (submittedUrl == null || !submittedUrl.StartsWith("npf"))
                {
                    Console.WriteLine("This does not look like a valid authentication URL.");
                    Console.WriteLine("Please try again.");
                }
                else
                {
                    break;
                }
            }

            return submittedUrl;
        }
    }
}