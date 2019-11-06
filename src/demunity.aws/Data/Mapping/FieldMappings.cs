namespace demunity.aws.Data.Mapping
{
    public static class FieldMappings
    {
        public const string PartitionKey = "PartitionKey";
        public const string SortKey = "SortKey";
        public const string Gsi1PartitionKey = "GSI1PartitionKey";
        public const string CreatedTime = "CreatedTime";
        public const string RecordType = "RecordType";

        public static class User
        {
            public const string Email = PartitionKey;
            public const string RecordType = SortKey;
            public const string Id = Gsi1PartitionKey;
            public const string CreatedTime = FieldMappings.CreatedTime;
            public const string Name = "Name";
            public const string OnlineProfiles = "OnlineProfiles";
        }

        public static class Photo
        {
            public const string PhotoId = FieldMappings.SortKey;
            public const string ObjectKey = "ObjectKey";
            public const string CreatedTime = FieldMappings.CreatedTime;
            public const string Filename = "Filename";
            public const string UserId = FieldMappings.Gsi1PartitionKey;
            public const string State = "PhotoState";
            public const string Sizes = "Sizes";
            public const string LikeCount = "LikeCount";
            public const string CommentCount = "CommentCount";
            public const string RawText = "RawText";
            public const string UserName = "Name";
            public const string Score = "Score";
            public const string Hashtags = "Hashtags";
        }


        public static class PhotoLike
        {
            public const string UserId = FieldMappings.Gsi1PartitionKey;
            public const string PhotoId = FieldMappings.PartitionKey;
            public const string CreatedTime = FieldMappings.CreatedTime;

        }

        public static class PhotoComment
        {
            public const string UserId = FieldMappings.Gsi1PartitionKey;
            public const string PhotoId = FieldMappings.PartitionKey;
            public const string CreatedTime = FieldMappings.CreatedTime;
            public const string Text = "RawText";
            public const string UserName = "Name";
        }

        public static class Hashtag
        {
            public const string HastagId = FieldMappings.PartitionKey;
            public const string PhotoId = FieldMappings.SortKey;
            public const string CreatedTime = FieldMappings.CreatedTime;
        }

        public static class Settings
        {
            public const string Version = "Version";
            public const string SettingObjectJson = "SettingObjectJson";
            public const string Domain = FieldMappings.SortKey;
        }
    }

}