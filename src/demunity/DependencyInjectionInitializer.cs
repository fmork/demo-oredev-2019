using demunity.aws;
using demunity.aws.Data;
using demunity.aws.Data.DynamoDb;
using demunity.aws.Security;
using demunity.lib;
using demunity.lib.Data;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Security;
using demunity.lib.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace demunity
{
    internal static class DependencyInjectionInitializer
    {
        public static void SetupDependencyInjection(IServiceCollection services)
        {

            services.AddSingleton<IHttpHelper>(provider =>
                new HttpHelper());

            services.AddSingleton<ITextSplitter>(provider =>
                new TextSplitter(
                    provider.GetService<IHttpHelper>(),
                    provider.GetService<ILogWriterFactory>()));

            SetupServiceInterfaces(services);

            services.AddSingleton<ILogWriterFactory>(provider =>
                new AspnetCoreLogWriterFactory(provider.GetService<ILoggerFactory>()));

            services.AddSingleton<IRemoteFileRepository>(provider =>
                new S3FileRepository(provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<IScoreCalculator>(provider =>
                new ScoreCalculator(provider.GetService<ISystem>(),
                provider.GetService<ILogWriterFactory>()));

            SetupDataRepositories(services);


            services.AddSingleton<ISecretsProvider>(provider =>
                new AwsSecretsProvider(provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<IDynamoDbClientFactory>(provider =>
                new DynamoDbClientFactory(
                    provider.GetService<IEnvironment>().GetVariable(Constants.EnvironmentVariables.LocalDynamoDbEndpoint)));

            services.AddSingleton<IEnvironment>(provider => new SystemEnvironment());
            services.AddSingleton<ISystemTime>(provider => new SystemTime());
            services.AddSingleton<ISystem>(provider
                => new DefaultSystem(
                    provider.GetService<IEnvironment>(),
                    provider.GetService<ISystemTime>()));

        }

        private static void SetupDataRepositories(IServiceCollection services)
        {
            services.AddSingleton<IDynamoDbCore>(provider =>
                new DynamoDbCore(provider.GetService<IDynamoDbClientFactory>(),
                provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<IPhotoRepository>(provider =>
                new DynamoDbPhotoRepository(
                    provider.GetService<IScoreCalculator>(),
                    provider.GetService<IDynamoDbCore>(),
                    provider.GetService<ISystem>(),
                    provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<IUserRepository>(provider =>
                new UserRepository(
                    provider.GetService<IDynamoDbCore>(),
                    provider.GetService<ISystem>(),
                    provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<ILikeRepository>(provider =>
                new LikeRepository(
                    provider.GetService<ISystem>(),
                    provider.GetService<IPhotoRepository>(),
                    provider.GetService<IScoreCalculator>(),
                    provider.GetService<IDynamoDbCore>(),
                    provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<ICommentRepository>(provider =>
                new CommentRepository(
                    provider.GetService<IPhotoRepository>(),
                    provider.GetService<IDynamoDbCore>(),
                    provider.GetService<ISystem>(),
                    provider.GetService<IScoreCalculator>(),
                    provider.GetService<ILogWriterFactory>()));

            services.AddSingleton<ISettingsRepository>(provider =>
                new SettingsRepository(
                    provider.GetService<ISystem>(),
                    provider.GetService<IDynamoDbCore>()));
        }

        private static void SetupServiceInterfaces(IServiceCollection services)
        {
            services.AddSingleton<IFeedbackService>(provider =>
                new FeedbackService(
                    provider.GetService<ILikeRepository>(),
                    provider.GetService<ICommentRepository>(),
                    provider.GetService<ILogWriterFactory>()));


            services.AddSingleton<IPhotosService>(provider =>
                new PhotosService(
                    provider.GetService<IPhotoRepository>(),
                    provider.GetService<IRemoteFileRepository>(),
                    provider.GetService<ITextSplitter>(),
                    provider.GetService<ILogWriterFactory>()));


            services.AddSingleton<IUsersService>(provider =>
                new UsersService(
                    provider.GetService<IUserRepository>(),
                    provider.GetService<ILogWriterFactory>()));

        }
    }
}