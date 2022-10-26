namespace DragaliaClient;

public class Constants
{
    public const string NintendoUserAgent = "com.nintendo.zaga/2.19.0 ONEPLUS A6003/10 NPFSDK/Unity-2.33.0-0a4be7c8";
    public const string UnityUserAgent = "UnityPlayer/2019.4.31f1 (UnityWebRequest/1.0, libcurl/7.75.0-DEV)";
    public const string AcceptLanguage = "en-US; q=1, en; q=0.5, *; q=0.001";
    public const string UnityVersion = "2019.4.31f1";
    public const string Graphics = "Apple A9 GPU";
    public const string OsVersion = "iOS 14.3";
    public const string AppVer = "2.19.0";
    public const string Platform = "1";
    public const string Device = "1";

    // Nintendo user auth constants
    public const string AccountsHost = "https://accounts.nintendo.com/";
    public const string Authorize = $"{AccountsHost}connect/1.0.0/authorize";
    public const string SessionToken = $"{AccountsHost}connect/1.0.0/api/session_token";
    public const string SdkToken = "https://api.accounts.nintendo.com/1.0.0/gateway/sdk/token";

    // Authorize constants
    public const string ClientId = "5192a0623a51561a";
    public const string RedirectUri = $"npf{ClientId}://auth";
    public const string ResponseType = "session_token_code";
    public const string Scope = "user user.birthday openid";
    public const string Language = "en-US";
    public const string SessionCodeChallengeMethod = "S256";

    // Nintendo device auth constants
    public const string BaaSSdkPrefix = "/core/v1/gateway/sdk/";
    public const string BaaSHost = "https://48cc81cdb8de30e061928f56e9bd4b4d.baas.nintendo.com";
    public const string BaaSFederation = $"{BaaSHost}{BaaSSdkPrefix}federation";
    public const string BaaSLogin = $"{BaaSHost}{BaaSSdkPrefix}login";

    // Coneshell constants
    public const string OfficialServerEncPublicKey = "d733a12a53e53153b1ffd8908d28e0e1be2f03b17d9d47deca8285070094d849";
    public const string ProductionEndpoint = "https://production-api.dragalialost.com/2.19.0_20220719103923";

    // Coneshell login constants
    public const string TransitionByNAccount = $"{ProductionEndpoint}/transition/transition_by_n_account"; // Unused/Not needed
    public const string PushNotificationUpdate = $"{ProductionEndpoint}/push_notification/update_setting";
    public const string ToolAuth = $"{ProductionEndpoint}/tool/auth";
    public const string GetDeployVersion = $"{ProductionEndpoint}/deploy/get_deploy_version";
    public const string GetResourceVersion = $"{ProductionEndpoint}/version/get_resource_version";

    // Coneshell player data constants
    public const string LoadIndex = $"{ProductionEndpoint}/load/index";
    public const string GetMissionList = $"{ProductionEndpoint}/mission/get_mission_list";
    public const string DmodeGetData = $"{ProductionEndpoint}/dmode/get_data";
}