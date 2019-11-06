using demunity.aws.Data.Mapping;

namespace demunity.aws.Data
{
    public static class Mappers
    {
        public static PhotoModelMapper PhotoModel { get; } = new PhotoModelMapper();
        public static UserModelMapper UserModel { get; } = new UserModelMapper();
        public static PhotoLikeMapper PhotoLike { get; } = new PhotoLikeMapper();
        public static PhotoCommentMapper PhotoComment { get; } = new PhotoCommentMapper();
        public static HashtagModelMapper Hashtag { get; } = new HashtagModelMapper();
        public static HashtagPhotoMapper HashtagPhoto { get; } = new HashtagPhotoMapper();
        public static SettingsModelMapper Settings { get; } = new SettingsModelMapper();
        public static NoopMapper Noop { get; } = new NoopMapper();
    }
}