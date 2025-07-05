using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace AutoCAD_Layer_Manger.UI
{
    public partial class ProgressForm : Form
    {
        private readonly object _lockObject = new object();
        private string _status = string.Empty;
        private int _progress;
        private int _maximum = 100;
        private bool _isIndeterminate;
        private DateTime _startTime;
        private System.Windows.Forms.Timer? _updateTimer;

        public string Status
        {
            get
            {
                lock (_lockObject)
                {
                    return _status;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _status = value ?? string.Empty;
                }
                UpdateUI();
            }
        }

        public int Progress
        {
            get
            {
                lock (_lockObject)
                {
                    return _progress;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _progress = Math.Max(0, Math.Min(value, _maximum));
                }
                UpdateUI();
            }
        }

        public int Maximum
        {
            get
            {
                lock (_lockObject)
                {
                    return _maximum;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _maximum = Math.Max(1, value);
                    _progress = Math.Min(_progress, _maximum);
                }
                UpdateUI();
            }
        }

        public bool IsIndeterminate
        {
            get
            {
                lock (_lockObject)
                {
                    return _isIndeterminate;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    _isIndeterminate = value;
                }
                UpdateUI();
            }
        }

        public TimeSpan ElapsedTime => DateTime.Now - _startTime;

        public ProgressForm()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            _startTime = DateTime.Now;
            
            // �]�w��s�p�ɾ�
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 100, // �C100�@���s�@��
                Enabled = true
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            
            // ��l�� UI
            UpdateUI();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateTimeDisplay();
        }

        private void UpdateUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateUIInternal));
            }
            else
            {
                UpdateUIInternal();
            }
        }

        private void UpdateUIInternal()
        {
            lock (_lockObject)
            {
                try
                {
                    // ��s���A��r
                    statusLabel.Text = _status;

                    // ��s�i�ױ�
                    progressBar.Style = _isIndeterminate 
                        ? ProgressBarStyle.Marquee 
                        : ProgressBarStyle.Continuous;
                    
                    if (!_isIndeterminate)
                    {
                        progressBar.Maximum = _maximum;
                        progressBar.Value = _progress;
                        
                        // ��s�i�׼���
                        double percentage = _maximum > 0 ? (double)_progress / _maximum * 100 : 0;
                        progressLabel.Text = $"{_progress}/{_maximum} ({percentage:F1}%)";
                        progressLabel.Visible = true;
                    }
                    else
                    {
                        progressLabel.Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Progress form update error: {ex.Message}");
                }
            }
        }

        private void UpdateTimeDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateTimeDisplayInternal));
            }
            else
            {
                UpdateTimeDisplayInternal();
            }
        }

        private void UpdateTimeDisplayInternal()
        {
            try
            {
                var elapsed = ElapsedTime;
                Text = $"�ഫ�i�� - �w�ήɶ�: {elapsed:mm\\:ss}";

                // ����Ѿl�ɶ�
                if (!IsIndeterminate && Progress > 0 && Maximum > 0)
                {
                    double progressPercent = (double)Progress / Maximum;
                    if (progressPercent > 0.01) // �ܤ֧���1%�~����
                    {
                        var totalEstimated = TimeSpan.FromTicks((long)(elapsed.Ticks / progressPercent));
                        var remaining = totalEstimated - elapsed;
                        
                        if (remaining.TotalSeconds > 0)
                        {
                            Text += $" - �w���Ѿl: {remaining:mm\\:ss}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Time display update error: {ex.Message}");
            }
        }

        public void SetProgress(int current, int total, string operation = "")
        {
            Maximum = total;
            Progress = current;
            if (!string.IsNullOrEmpty(operation))
            {
                Status = operation;
            }
        }

        public void Complete(string message = "�ާ@����")
        {
            IsIndeterminate = false;
            Progress = Maximum;
            Status = message;
            
            // �u����ܧ������A������
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(Close));
                }
                else
                {
                    Close();
                }
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _updateTimer = null;
            
            base.OnFormClosing(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            
            // �T�O�����ܦb�̫e��
            TopMost = true;
            BringToFront();
            Focus();
        }
    }
}