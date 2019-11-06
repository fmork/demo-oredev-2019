using demunity.lib;
using demunity.lib.Logging;
using demunity.lib.Settings;

namespace demunity.popscore
{
    public interface IScoringWorkerDependencies
    {
        void SetLogWriterFactory(ILogWriterFactory logWriterFactory);
        ILogWriterFactory LogWriterFactory { get; }
        ISystem System { get; }
        IScoreCalculator ScoreCalculator { get; }
        IPhotosService PhotosService { get; }
        ISettingsService SettingsService { get; }
    }
}
