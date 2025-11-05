using Importer.Common.Helpers;
using Importer.Common.Interfaces;
using Importer.Common.Main;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Importer.Common.Services
{
    public class PipeServer : IPipeMessageService, IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private readonly string _pipeName;
        private bool _disposed = false;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();

        public bool IsConnected => _pipeServer?.IsConnected == true;
        public bool SendEnabled { get; set; }

        public event EventHandler<string> MessageReceived;

        public PipeServer(string pipeName)
        {
            _pipeName = pipeName;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Initialize()
        {
            _serverTask = Task.Run(() => RunServer(_cancellationTokenSource.Token));
        }

        private async Task RunServer(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    await HandleClientCommunication(cancellationToken);

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000, cancellationToken);
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }
        }

        private async Task HandleClientCommunication(CancellationToken cancellationToken)
        {

            var readerTask = ReadLoop(cancellationToken);

            while (!cancellationToken.IsCancellationRequested && _pipeServer?.IsConnected == true)
            {
                try
                {
                    while (_messageQueue.TryDequeue(out string message))
                    {
                        await SendMessageAsync(message);
                    }                    

                    await Task.Delay(50, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    break;
                }
            }

            try { await Task.WhenAny(readerTask, Task.Delay(500, cancellationToken)); } catch { }
        }

        private async Task ReadLoop(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested && _pipeServer?.IsConnected == true)
            {
                try
                {
                    if (_pipeServer == null || !_pipeServer.CanRead)
                    {
                        await Task.Delay(25, cancellationToken);
                        continue;
                    }

                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            int read = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (read == 0)
                            {
                                return; // Disconnected
                            }
                            ms.Write(buffer, 0, read);
                        } while (_pipeServer != null && !_pipeServer.IsMessageComplete);

                        var data = ms.ToArray();
                        var text = DecodeIncoming(data);

                        await HandleCommand(text);

                        if (!string.IsNullOrEmpty(text))
                        {
                            try { MessageReceived?.Invoke(this, text); } catch { }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }

        private static string DecodeIncoming(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            try
            {
                if (data.Length >= 4)
                {
                    int len = BitConverter.ToInt32(data, 0);
                    int remaining = data.Length - 4;
                    if (len >= 0 && len == remaining)
                    {
                        return Encoding.UTF8.GetString(data, 4, len);
                    }
                }
            }
            catch { }

            return Encoding.UTF8.GetString(data);
        }

        public void SendMessage(string message)
        {
            Debug.WriteLine($"[PipeService] SendMessage called. Connected: {IsConnected}. Message length: {message?.Length}");

            if (string.IsNullOrEmpty(message))
                return;

            _messageQueue.Enqueue(message);
                
            Debug.WriteLine($"[PipeService] Message queued. Queue size: {_messageQueue.Count}");
        }

        private async Task SendMessageAsync(string message)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                return;
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                await _pipeServer.WriteAsync(lengthBytes, 0, 4);
                await _pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _pipeServer.FlushAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PipeService] SendMessageAsync error: {ex.Message}");
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Cancel();
                    try
                    {
                        _serverTask?.Wait(TimeSpan.FromSeconds(2));
                    }
                    catch { }
                    _pipeServer?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                _disposed = true;
            }
        }

        public Task HandleCommand(string command)
        {
            var CommandLibrary = new Dictionary<string, Func<Task>>
            {
                { "Process", new Func<Task>(async () => await TriggerProcessDatabase()) },

            };

            if (CommandLibrary.TryGetValue(command, out var action))
            {
                Logger.Info($"Executing command received via pipe: {command}");
                return Task.Run(action);
            }
            else
            {
                Logger.Warn($"Unknown command received via pipe: {command}");
            }

            return Task.CompletedTask;
        }

        private async Task TriggerProcessDatabase()
        {
            Logger.Info("Manually triggering Process Database action");
            try
            {
                ProcessDatabase processDatabase = new ProcessDatabase();
                await processDatabase.ProcessImport();
                Logger.Info("Manual Process Database action finished");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error manually triggering Process Database action - {ex.Message}");
            }
        }
    }
}
