using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace AutoCAD_Layer_Manger
{
    /// <summary>
    /// 圖層信息模型
    /// </summary>
    public class LayerInfo : INotifyPropertyChanged, IEquatable<LayerInfo>
    {
        private string _name = string.Empty;
        private bool _isLocked;
        private bool _isFrozen;
        private bool _isOff;
        private string _color = string.Empty;
        private LineWeight _lineWeight = LineWeight.ByLayer;
        private string _plotStyleName = string.Empty;
        private bool _isPlottable = true;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public bool IsLocked
        {
            get => _isLocked;
            set
            {
                if (_isLocked != value)
                {
                    _isLocked = value;
                    OnPropertyChanged(nameof(IsLocked));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public bool IsFrozen
        {
            get => _isFrozen;
            set
            {
                if (_isFrozen != value)
                {
                    _isFrozen = value;
                    OnPropertyChanged(nameof(IsFrozen));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public bool IsOff
        {
            get => _isOff;
            set
            {
                if (_isOff != value)
                {
                    _isOff = value;
                    OnPropertyChanged(nameof(IsOff));
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsAvailable));
                }
            }
        }

        public string Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                }
            }
        }

        public LineWeight LineWeight
        {
            get => _lineWeight;
            set
            {
                if (_lineWeight != value)
                {
                    _lineWeight = value;
                    OnPropertyChanged(nameof(LineWeight));
                }
            }
        }

        public string PlotStyleName
        {
            get => _plotStyleName;
            set
            {
                if (_plotStyleName != value)
                {
                    _plotStyleName = value;
                    OnPropertyChanged(nameof(PlotStyleName));
                }
            }
        }

        public bool IsPlottable
        {
            get => _isPlottable;
            set
            {
                if (_isPlottable != value)
                {
                    _isPlottable = value;
                    OnPropertyChanged(nameof(IsPlottable));
                }
            }
        }

        // 計算屬性
        public string Status => GetStatusText();
        public bool IsAvailable => !IsLocked && !IsFrozen && !IsOff;
        public string DisplayName => $"{Name} {(IsAvailable ? "" : $"({Status})")}";

        private string GetStatusText()
        {
            var statuses = new List<string>();
            if (IsLocked) statuses.Add("鎖定");
            if (IsFrozen) statuses.Add("凍結");
            if (IsOff) statuses.Add("關閉");
            return statuses.Count > 0 ? string.Join(", ", statuses) : "正常";
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEquatable Implementation

        public bool Equals(LayerInfo? other)
        {
            return other != null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as LayerInfo);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// 轉換結果模型
    /// </summary>
    public class ConversionResult
    {
        public int ConvertedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public int TotalProcessed => ConvertedCount + SkippedCount + ErrorCount;
        public bool HasErrors => ErrorCount > 0;
        public bool IsSuccessful => ConvertedCount > 0 && ErrorCount == 0;

        public string GetSummary()
        {
            var summary = $"轉換完成！\n成功轉換: {ConvertedCount} 個物件\n跳過物件: {SkippedCount} 個";
            
            if (ErrorCount > 0)
            {
                summary += $"\n錯誤: {ErrorCount} 個";
            }

            return summary;
        }

        public string GetDetailedReport()
        {
            var report = GetSummary();
            
            if (Errors.Count > 0)
            {
                report += "\n\n錯誤詳情:\n" + string.Join("\n", Errors.Take(10));
                if (Errors.Count > 10)
                {
                    report += $"\n... 以及其他 {Errors.Count - 10} 個錯誤";
                }
            }

            return report;
        }

        public void Merge(ConversionResult other)
        {
            if (other == null) return;

            ConvertedCount += other.ConvertedCount;
            SkippedCount += other.SkippedCount;
            ErrorCount += other.ErrorCount;
            Errors.AddRange(other.Errors);
        }
    }

    /// <summary>
    /// 應用程式設定
    /// </summary>
    public class AppSettings
    {
        public bool ShowProgressDialog { get; set; } = true;
        public bool AutoUnlockTargetLayer { get; set; } = true;
        public bool SkipLockedObjects { get; set; } = true;
        public bool ProcessBlocks { get; set; } = true;
        public int MaxRecursionDepth { get; set; } = 50;
        public bool RememberLastSettings { get; set; } = true;
        public string LastSelectedLayer { get; set; } = string.Empty;

        public static AppSettings Default => new AppSettings();
    }
}