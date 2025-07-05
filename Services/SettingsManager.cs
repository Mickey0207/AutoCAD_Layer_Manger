using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoCAD_Layer_Manger.Services
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface ISettingsManager
    {
        T GetSettings<T>() where T : class, new();
        void SaveSettings<T>(T settings) where T : class;
        bool SettingsExist<T>() where T : class;
        void ResetSettings<T>() where T : class;
    }

    /// <summary>
    /// JSON 配置管理器
    /// </summary>
    public class JsonSettingsManager : ISettingsManager
    {
        private readonly string _settingsDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonSettingsManager()
        {
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoCAD_Layer_Manager");

            if (!Directory.Exists(_settingsDirectory))
            {
                Directory.CreateDirectory(_settingsDirectory);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public T GetSettings<T>() where T : class, new()
        {
            try
            {
                string filePath = GetSettingsFilePath<T>();
                
                if (!File.Exists(filePath))
                {
                    return new T();
                }

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入設定失敗: {ex.Message}");
                return new T();
            }
        }

        public void SaveSettings<T>(T settings) where T : class
        {
            if (settings == null) return;

            try
            {
                string filePath = GetSettingsFilePath<T>();
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存設定失敗: {ex.Message}");
            }
        }

        public bool SettingsExist<T>() where T : class
        {
            string filePath = GetSettingsFilePath<T>();
            return File.Exists(filePath);
        }

        public void ResetSettings<T>() where T : class
        {
            try
            {
                string filePath = GetSettingsFilePath<T>();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重設設定失敗: {ex.Message}");
            }
        }

        private string GetSettingsFilePath<T>() where T : class
        {
            string fileName = $"{typeof(T).Name}.json";
            return Path.Combine(_settingsDirectory, fileName);
        }
    }

    /// <summary>
    /// 應用程式狀態管理器
    /// </summary>
    public class AppStateManager
    {
        private readonly ISettingsManager _settingsManager;
        private AppSettings? _currentSettings;

        public AppStateManager(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        public AppSettings Settings
        {
            get
            {
                if (_currentSettings == null)
                {
                    LoadSettings();
                }
                return _currentSettings!;
            }
        }

        public void LoadSettings()
        {
            _currentSettings = _settingsManager.GetSettings<AppSettings>();
        }

        public void SaveSettings()
        {
            if (_currentSettings != null)
            {
                _settingsManager.SaveSettings(_currentSettings);
            }
        }

        public void ResetToDefaults()
        {
            _settingsManager.ResetSettings<AppSettings>();
            _currentSettings = AppSettings.Default;
        }

        public void UpdateSettings(Action<AppSettings> updateAction)
        {
            if (updateAction == null) return;

            updateAction(Settings);
            SaveSettings();
        }
    }
}