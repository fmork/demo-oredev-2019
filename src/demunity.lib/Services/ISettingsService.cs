using System.Threading.Tasks;
using demunity.lib.Data.Models;

namespace demunity.lib.Settings
{
    public interface ISettingsService
    {
        Task<SettingsModel> GetSettings(string settingsDomain);
        Task SetSettings(string settingsDomain, SettingsModel settings);
    }
}