using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using demunity.aws.Logging;
using demunity.lib;
using demunity.lib.Data.Models;
using demunity.lib.Extensions;
using demunity.lib.Logging;
using demunity.popscore.Settings.v1;
using Newtonsoft.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace demunity.popscore
{

    public class ScoringWorker
    {
        private readonly IScoringWorkerDependencies dependencies;
        private readonly Func<ILambdaLogger, ILogWriterFactory> logWriterFactoryFactory;

        public ScoringWorker() : this(lambdaLogger => new LambdaLogWriterFactory(lambdaLogger), new ScoringDependencies())
        {
            AWSSDKHandler.RegisterXRayForAllServices();
        }

        public ScoringWorker(Func<ILambdaLogger, ILogWriterFactory> logWriterFactoryFactory, IScoringWorkerDependencies dependencies)
        {
            this.logWriterFactoryFactory = logWriterFactoryFactory ?? throw new ArgumentNullException(nameof(logWriterFactoryFactory));
            this.dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }

        public async Task CalculatePhotoScores(ILambdaContext context)
        {

            dependencies.SetLogWriterFactory(logWriterFactoryFactory(context.Logger));

            var logger = dependencies.LogWriterFactory.CreateLogger<ScoringWorker>();

            logger.LogInformation($"{nameof(CalculatePhotoScores)}");
            try
            {
                SettingsModel settingsModel = await dependencies.SettingsService.GetSettings("scoring-worker");
                var scoringWorkerSettings = GetScoringWorkerSettings(settingsModel);
                var thresholdConfiguration = GetThresholdConfiguration(scoringWorkerSettings);
                DateTimeOffset utcNow = dependencies.System.Time.UtcNow;
                var referenceTime = utcNow - thresholdConfiguration.LookbackSpan;
                logger.LogInformation(text: $"Calculating score changes up to {thresholdConfiguration.LookbackSpan.GetPastTimeString()}. Reference time is '{referenceTime.ToString(Constants.DateTimeFormatWithMilliseconds)}'");
                var calculationData = await dependencies.PhotosService.GetPhotosWithCommentsAndLikes(referenceTime);
                var scores = dependencies.ScoreCalculator.CalculateScores(calculationData);

                logger.LogInformation($"Calculated scores:\n{JsonConvert.SerializeObject(scores.ToArray())}");
                if (scores.Any())
                {
                    await dependencies.PhotosService.UpdateScores(scores);
                }

                thresholdConfiguration.LatestRunTime = utcNow;

                settingsModel.Version = "v1";
                settingsModel.SettingObjectJson = JsonConvert.SerializeObject(scoringWorkerSettings);
                await dependencies.SettingsService.SetSettings("scoring-worker", settingsModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in {nameof(CalculatePhotoScores)}:\n{ex.ToString()}");
                throw;
            }
        }

        private ScoringWorkerSettings GetScoringWorkerSettings(SettingsModel settingsModel)
        {
            if (string.IsNullOrEmpty(settingsModel.SettingObjectJson))
            {
                // return default settings object
                return new ScoringWorkerSettings
                {
                    ThresholdConfigurations = new[]{
                        // once an hour, look 19 hours back
                        new ThresholdConfiguration
                        {
                            LookbackSpan = TimeSpan.FromHours(19),
                            Interval = TimeSpan.FromHours(1),
                            LatestRunTime = DateTimeOffset.MinValue
                        },

                        // once every 30 minutes, look 8 hours back
                        new ThresholdConfiguration
                        {
                            LookbackSpan = TimeSpan.FromHours(8),
                            Interval = TimeSpan.FromMinutes(30),
                            LatestRunTime = DateTimeOffset.MinValue
                        },

                        // once every 10 minutes, look one hour back
                        new ThresholdConfiguration
                        {
                            LookbackSpan = TimeSpan.FromHours(1),
                            Interval = TimeSpan.FromMinutes(10),
                            LatestRunTime = DateTimeOffset.MinValue
                        },

                        // once every 5 minutes, look 30 minutes back
                        new ThresholdConfiguration
                        {
                            LookbackSpan = TimeSpan.FromMinutes(30),
                            Interval = TimeSpan.FromMinutes(5),
                            LatestRunTime = DateTimeOffset.MinValue
                        },

                        // every minute, look 10 minutes back
                        new ThresholdConfiguration
                        {
                            LookbackSpan = TimeSpan.FromMinutes(10),
                            Interval = TimeSpan.FromMinutes(1),
                            LatestRunTime = DateTimeOffset.MinValue
                        },
                    }
                };
            }

            return JsonConvert.DeserializeObject<ScoringWorkerSettings>(settingsModel.SettingObjectJson);
        }

        private ThresholdConfiguration GetThresholdConfiguration(ScoringWorkerSettings scoringWorkerSettings)
        {
            var now = dependencies.System.Time.UtcNow;


            var nextReferenceTime = scoringWorkerSettings.ThresholdConfigurations.Where(x =>
            {
                // get the time this threshold was run
                var latestRunTime = x.LatestRunTime;

                // get the due time for the next run of this threshold
                var dueTime = latestRunTime + x.Interval;

                // if due time has passed, this is a candidate
                return dueTime < now;
            })
            .OrderByDescending(x => x.LookbackSpan)
            .FirstOrDefault() ?? scoringWorkerSettings.ThresholdConfigurations.OrderBy(x => x.LookbackSpan).First();

            return nextReferenceTime;
        }
    }
}
