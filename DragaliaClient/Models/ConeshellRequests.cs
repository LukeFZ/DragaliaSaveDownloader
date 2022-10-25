using MessagePack;

namespace DragaliaClient.Models;

// ReSharper disable InconsistentNaming
public class ConeshellRequests
{
    [MessagePackObject(true)]
    public class ToolAuthRequest : RequestCommon
    {
        public string uuid { get; set; }
        public string id_token { get; set; }

        public ToolAuthRequest(string uuid, string id_token)
        {
            this.uuid = uuid;
            this.id_token = id_token;
        }
    }

    [MessagePackObject(true)]
    public class RequestCommon
    {

    }

    [MessagePackObject(true)]
    public class ResponseCommon
    {
        public DataHeader data_headers { get; set; }

        public ResponseCommon(DataHeader data_headers)
        {
            this.data_headers = data_headers;
        }
    }

    [MessagePackObject(true)]
    public class DataHeader
    {
        public int result_code { get; set; }
    }

    [MessagePackObject(true)]
    public class ToolAuthResponse : ResponseCommon
    {
        public CommonResponse data { get; set; }

        [MessagePackObject(true)]
        public class CommonResponse
        {
            public ulong viewer_id { get; set; }
            public string session_id { get; set; }
            public string nonce { get; set; }

            public CommonResponse(ulong viewer_id, string session_id, string nonce)
            {
                this.viewer_id = viewer_id;
                this.session_id = session_id;
                this.nonce = nonce;
            }
        }

        public ToolAuthResponse(CommonResponse data, DataHeader data_headers) : base(data_headers)
        {
            this.data = data;
        }
    }

    [MessagePackObject(true)]
    public class TransitionByNAccountResponse : ResponseCommon
    {
        public CommonResponse data { get; set; }

        [MessagePackObject(true)]
        public class CommonResponse
        {
            public TransitionResultData transition_result_data { get; set; }

            public CommonResponse(TransitionResultData transition_result_data)
            {
                this.transition_result_data = transition_result_data;
            }
        }

        [MessagePackObject(true)]
        public class TransitionResultData
        {
            public ulong abolished_viewer_id { get; set; }
            public ulong linked_viewer_id { get; set; }

            public TransitionResultData(ulong abolished_viewer_id, ulong linked_viewer_id)
            {
                this.abolished_viewer_id = abolished_viewer_id;
                this.linked_viewer_id = linked_viewer_id;
            }
        }

        public TransitionByNAccountResponse(CommonResponse data, DataHeader data_headers) : base(data_headers)
        {
            this.data = data;
        }
    }

    [MessagePackObject(true)]
    public class PushNotificationUpdateSettingRequest : RequestCommon
    {
        public string region { get; set; }
        public string uuid { get; set; }
        public string lang { get; set; }

        public PushNotificationUpdateSettingRequest(string region, string uuid, string lang)
        {
            this.region = region;
            this.uuid = uuid;
            this.lang = lang;
        }
    }

    [MessagePackObject(true)]
    public class PushNotificationUpdateSettingResponse : ResponseCommon
    {
        public CommonResponse data { get; set; }

        [MessagePackObject(true)]
        public class CommonResponse
        {
            public int result { get; set; }

            public CommonResponse(int result)
            {
                this.result = result;
            }
        }

        public PushNotificationUpdateSettingResponse(CommonResponse data, DataHeader data_headers) : base(data_headers)
        {
            this.data = data;
        }
    }

    [MessagePackObject(true)]
    public class DeployGetDeployVersionResponse : ResponseCommon
    {
        public CommonResponse data { get; set; }

        [MessagePackObject(true)]
        public class CommonResponse
        {
            public string deploy_hash { get; set; }

            public CommonResponse(string deploy_hash)
            {
                this.deploy_hash = deploy_hash;
            }
        }

        public DeployGetDeployVersionResponse(CommonResponse data, DataHeader data_headers) : base(data_headers)
        {
            this.data = data;
        }
    }

    [MessagePackObject(true)]
    public class VersionGetResourceVersionResponse : ResponseCommon
    {
        public CommonResponse data { get; set; }

        [MessagePackObject(true)]
        public class CommonResponse
        {
            public string resource_version { get; set; }

            public CommonResponse(string resource_version)
            {
                this.resource_version = resource_version;
            }
        }

        public VersionGetResourceVersionResponse(CommonResponse data, DataHeader data_headers) : base(data_headers)
        {
            this.data = data;
        }
    }

    [MessagePackObject(true)]
    public class VersionGetResourceVersionRequest : RequestCommon
    {
        public int platform { get; set; }
        public string app_version { get; set; }

        public VersionGetResourceVersionRequest(int platform, string app_version)
        {
            this.platform = platform;
            this.app_version = app_version;
        }
    }
}