using System;
using demunity.aws;
using demunity.aws.Data;
using demunity.aws.Data.DynamoDb;
using demunity.lib;
using demunity.lib.Data;
using demunity.lib.Logging;
using demunity.lib.Net;
using demunity.lib.Settings;
using demunity.lib.Text;

namespace demunity.popscore
{
    public class ScoringDependencies : IScoringWorkerDependencies
    {
        private readonly Func<ILogWriterFactory, S3FileRepository> remoteFileRepositoryFactory;
        private readonly Func<ILogWriterFactory, IPhotoRepository> dataRepositoryFactory;
        private readonly Lazy<IScoreCalculator> lazyScoreCalculator;
        private readonly Lazy<IPhotosService> lazyPhotosService;
        private readonly Lazy<ISettingsService> lazySettingsService;
        private readonly DefaultSystem system;

        public ScoringDependencies()
        {
            var dynamoDbClientFactory = new DynamoDbClientFactory(string.Empty);
            Func<ISystem> systemFactory = () => new DefaultSystem(new SystemEnvironment(), new SystemTime());

            lazyScoreCalculator = new Lazy<IScoreCalculator>(() =>
                new ScoreCalculator(systemFactory(), this.LogWriterFactory));

            Func<ILogWriterFactory, IDynamoDbCore> dynamoDbCoreFactory = logWriterFactory => new DynamoDbCore(dynamoDbClientFactory, logWriterFactory);
            remoteFileRepositoryFactory = logWriterFactory => new S3FileRepository(logWriterFactory);
            dataRepositoryFactory = logWriterFactory => new DynamoDbPhotoRepository(lazyScoreCalculator.Value, dynamoDbCoreFactory(logWriterFactory), new DefaultSystem(new SystemEnvironment(), new SystemTime()), logWriterFactory);


            lazyPhotosService = new Lazy<IPhotosService>(() =>
                new PhotosService(
                    dataRepositoryFactory(this.LogWriterFactory),
                    remoteFileRepositoryFactory(this.LogWriterFactory),
                    new TextSplitter(new HttpHelper(), this.LogWriterFactory),
                    this.LogWriterFactory));

            lazySettingsService = new Lazy<ISettingsService>(()
                => new SettingsService(
                    new SettingsRepository(systemFactory(), dynamoDbCoreFactory(this.LogWriterFactory)),
                    this.LogWriterFactory));

            system = new DefaultSystem(new SystemEnvironment(), new SystemTime());
        }
        public ILogWriterFactory LogWriterFactory { get; private set; }

        public IScoreCalculator ScoreCalculator => lazyScoreCalculator.Value;

        public IPhotosService PhotosService => lazyPhotosService.Value;

        public ISettingsService SettingsService => lazySettingsService.Value;

        public ISystem System => system;

        public void SetLogWriterFactory(ILogWriterFactory logWriterFactory)
        {
            LogWriterFactory = logWriterFactory;
        }
    }
}
