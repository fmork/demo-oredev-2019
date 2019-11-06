using System.Threading.Tasks;
using demunity.lib.Data;
using demunity.lib.Data.Models;
using demunity.lib.Logging;

namespace demunity.lib.Settings
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingsRepository settingsRepository;
        private readonly ILogWriterFactory logWriterFactory;

        public SettingsService(ISettingsRepository settingsRepository, ILogWriterFactory logWriterFactory)
        {
            this.settingsRepository = settingsRepository ?? throw new System.ArgumentNullException(nameof(settingsRepository));
            this.logWriterFactory = logWriterFactory ?? throw new System.ArgumentNullException(nameof(logWriterFactory));
        }

        public async Task<SettingsModel> GetSettings(string settingsDomain)
        {
            return (await settingsRepository.GetSettings(settingsDomain)) ?? new SettingsModel { Domain = settingsDomain };
        }

        public Task SetSettings(string settingsDomain, SettingsModel settings)
        {
            return settingsRepository.SetSettings(settingsDomain, settings);
        }
    }
}