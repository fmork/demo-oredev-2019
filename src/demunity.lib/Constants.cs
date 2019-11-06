using System;
using System.IO;
using System.Reflection;

namespace demunity.lib
{
    public static class Constants
    {
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DateTimeFormatWithMilliseconds = "yyyy-MM-ddTHH:mm:ss,fffZ";
        public static class Security
        {
            public const string UserIdClaim = "demunity:userid";
            public const string UserNameClaim = "demunity:name";
        }
        public static class EnvironmentVariables
        {
            public const string UploadBucket = "PHOTO_UPLOAD_BUCKET";

            public const string DynamoDbTableName = "DYNAMODB_TABLE_NAME";
            public const string LocalDynamoDbEndpoint = "LOCAL_DYNAMODB_ENDPOINT";
            public const string DataProtectionTableName = "DATA_PROTECTION_DYNAMODB_TABLE_NAME";
            public const string StaticAssetHost = "STATIC_ASSET_HOST";
            public const string SiteTitle = "SITE_TITLE";
            public const string GoogleTagManagerId = "GOOGLE_TAG_MANAGER_ID";
            public const string AwsRegion = "AWS_REGION";
        }

        public static readonly DateTime BuildTime = File.GetCreationTimeUtc(Assembly.GetExecutingAssembly().Location);
    }
}