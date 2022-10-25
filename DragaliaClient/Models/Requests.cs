using System.Text.Json.Serialization;

namespace DragaliaClient.Models;

// ReSharper disable InconsistentNaming

public record SessionTokenResponse(
    string code,
    string session_token
);

public record SdkTokenRequest(
    string client_id, 
    string session_token
);
public record SdkTokenResponse(
    string AccessToken,
    object? Error,
    uint ExpiresIn,
    string IdToken,
    string SessionToken,
    object? TermsAgreement,
    BaaSUser User
);

public record BaaSRequest(
    string AppVersion,
    string Assertion,
    string Carrier,
    DeviceAccount? DeviceAccount,
    string DeviceAnalyticsId,
    string DeviceName,
    IdpAccount? IdpAccount,
    string Locale,
    string Manufacturer,
    string NetworkType,
    string OsType,
    string OsVersion,
    string? PreviousUserId,
    string SdkVersion,
    string? SessionId,
    string TimeZone,
    uint TimeZoneOffset
);

public record BaaSLoginResponse(
    string AccessToken,
    string IdToken,
    DeviceAccount CreatedDeviceAccount,
    string SessionId,
    BaaSTrimmedUser? User
);

// Only has the necessary properties.
public record BaaSTrimmedUser(
    string Id
);

public record DeviceAccount(
    string Id,
    string Password
);

public record IdpAccount(
    string IdToken,
    string Idp = "nintendoAccount"
);

public record BaaSUser(
    bool AnalyticsOptedIn,
    ulong AnalyticsOptedInUpdatedAt,
    object AnalyticsPermissions,
    string Birthday,
    bool ClientFriendsOptedIn,
    ulong ClientFriendsOptedInUpdatedAt,
    string Country,
    ulong CreatedAt,
    object EachEmailOptedIn,
    bool EmailOptedIn,
    ulong EmailOptedInUpdatedAt,
    bool EmailVerified,
    string Gender,
    string Id,
    bool IsChild,
    string Language,
    string Nichname,
    object? Region,
    object Timezone,
    ulong UpdatedAt
);