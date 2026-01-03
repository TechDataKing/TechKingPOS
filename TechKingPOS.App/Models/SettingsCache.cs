using TechKingPOS.App.Models;

namespace TechKingPOS.App.Data
{
    public static class SettingsCache
    {
        public static AppSetting Current { get; private set; }
        public static long Version { get; private set; }

        public static void Load()
        {
            Current = SettingsRepository.Get() ?? new AppSetting();
            Version++;
        }

        public static void ApplyChanges(AppSetting updated)
        {
            Current = updated;
            Version++;
        }

        public static void Save()
        {
            if (Current != null)
                SettingsRepository.Save(Current);
        }
    }
}
