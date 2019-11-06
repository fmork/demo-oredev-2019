using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Data
{
    public interface ISettingsRepository
    {
        Task<SettingsModel> GetSettings(string settingsDomain);
        Task SetSettings(string settingsDomain, SettingsModel settings);

    }
}