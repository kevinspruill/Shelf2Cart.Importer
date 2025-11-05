using Importer.UI.Services;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Importer.UI.ViewModels
{
    public class LogEntry : BindableBase
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime Timestamp { get; set; }
        public Brush Color { get; set; }
    }

    public class ConsoleViewModel : BindableBase, IDisposable
    {
        private PipeClientService _pipeClientService;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _readTask;

        public ObservableCollection<LogEntry> LogEntries { get; set; } = new ObservableCollection<LogEntry>();

        public ConsoleViewModel()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            AddLogEntry("Connecting to S2C_ImporterLoggingPipe...", "INFO");
            InitializePipeConnection();
        }

        private string _consoleData;
        public string ConsoleData
        {
            get { return _consoleData; }
            set { SetProperty(ref _consoleData, value); }
        }

        private void InitializePipeConnection()
        {
            try
            {
                _pipeClientService = new PipeClientService("S2C_ImporterLoggingPipe");
                AddLogEntry("Connected successfully!", "INFO");
                _readTask = Task.Run(() => ContinuousRead(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                AddLogEntry($"Connection failed: {ex.Message}", "ERROR");
            }
        }

        private async Task ContinuousRead(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _pipeClientService?.IsConnected == true)
            {
                try
                {
                    string message = await _pipeClientService.ReadFromPipeAsync();
                    ParseAndAddLogEntry(message);
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AddLogEntry($"Read error: {ex.Message}", "ERROR");
                    });
                    break;
                }
            }
        }

        private void ParseAndAddLogEntry(string logMessage)
        {
            // Parse NLog format: "2025-01-30 10:15:30.1234 |INFO| Message here"
            try
            {
                var parts = logMessage.Split('|');
                if (parts.Length >= 3)
                {
                    var timestamp = parts[0].Trim();
                    var level = parts[1].Trim();
                    var message = string.Join("|", parts, 2, parts.Length - 2).Trim();

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AddLogEntry(message, level, DateTime.TryParse(timestamp, out var dt) ? dt : DateTime.Now);
                        ConsoleData += logMessage + "\n";
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AddLogEntry(logMessage, "INFO");
                        ConsoleData += logMessage + "\n";
                    });
                }
            }
            catch
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ConsoleData += logMessage + "\n";
                });
            }
        }

        private void AddLogEntry(string message, string level, DateTime? timestamp = null)
        {
            var entry = new LogEntry
            {
                Message = message,
                Level = level,
                Timestamp = timestamp ?? DateTime.Now,
                Color = GetColorForLevel(level)
            };

            LogEntries.Add(entry);

            // Keep only last 1000 entries to prevent memory issues
            while (LogEntries.Count > 1000)
            {
                LogEntries.RemoveAt(0);
            }
        }

        private Brush GetColorForLevel(string level)
        {
            switch (level?.ToUpper())
            {
                case "FATAL":
                    return Brushes.DarkRed;
                case "ERROR":
                    return Brushes.Red;
                case "WARN":
                    return Brushes.Orange;
                case "INFO":
                    return Brushes.Black;
                case "DEBUG":
                    return Brushes.Gray;
                case "TRACE":
                    return Brushes.LightGray;
                default:
                    return Brushes.Black;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
            _pipeClientService?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}