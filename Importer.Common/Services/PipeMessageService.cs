using Importer.Common.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Importer.Common.Services
{
    public class PipeMessageService : IPipeMessageService, IDisposable
    {
        private NamedPipeServerStream _pipeServer;
        private readonly string _pipeName;
        private bool _disposed = false;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private bool _debugMode = true;

        public bool IsConnected => _pipeServer?.IsConnected == true;

        public PipeMessageService(string pipeName)
        {
            _pipeName = pipeName;
            _cancellationTokenSource = new CancellationTokenSource();
            if (_debugMode)
                Debug.WriteLine($"[PipeService] Created with pipe name: {pipeName}");
        }

        public void Initialize()
        {
            if (_debugMode)
                Debug.WriteLine("[PipeService] Initialize called");
            _serverTask = Task.Run(() => RunServer(_cancellationTokenSource.Token));
        }

        private async Task RunServer(CancellationToken cancellationToken)
        {
            if (_debugMode)
                Debug.WriteLine("[PipeService] RunServer started");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_debugMode)
                        Debug.WriteLine($"[PipeService] Creating pipe server: {_pipeName}");

                    _pipeServer = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    if (_debugMode)
                        Debug.WriteLine("[PipeService] Waiting for connection...");

                    // Wait for a client to connect
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    if (_debugMode)
                        Debug.WriteLine("[PipeService] Client connected!");

                    // Send a welcome message immediately after connection
                    await SendMessageAsync("Connected to Importer Service");

                    // Handle communication with the connected client
                    await HandleClientCommunication(cancellationToken);

                    if (_debugMode)
                        Debug.WriteLine("[PipeService] Client disconnected");
                }
                catch (OperationCanceledException)
                {
                    if (_debugMode)
                        Debug.WriteLine("[PipeService] Server cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    if (_debugMode)
                        Debug.WriteLine($"[PipeService] Server error: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }

            if (_debugMode)
                Debug.WriteLine("[PipeService] RunServer ended");
        }

        private async Task HandleClientCommunication(CancellationToken cancellationToken)
        {
            if (_debugMode)
                Debug.WriteLine("[PipeService] HandleClientCommunication started");

            // Process messages while connected
            while (!cancellationToken.IsCancellationRequested && _pipeServer?.IsConnected == true)
            {
                try
                {
                    // Send any queued messages
                    while (_messageQueue.TryDequeue(out string message))
                    {
                        if (_debugMode)
                            Debug.WriteLine($"[PipeService] Dequeued message: {message?.Substring(0, Math.Min(50, message?.Length ?? 0))}...");
                        await SendMessageAsync(message);
                    }

                    // Small delay to prevent CPU spinning
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    if (_debugMode)
                        Debug.WriteLine($"[PipeService] Communication error: {ex.Message}");
                    break;
                }
            }

            if (_debugMode)
                Debug.WriteLine("[PipeService] HandleClientCommunication ended");
        }

        public void SendMessage(string message)
        {
            if (_debugMode)
                Debug.WriteLine($"[PipeService] SendMessage called. Connected: {IsConnected}. Message length: {message?.Length}");

            if (string.IsNullOrEmpty(message))
                return;

            // Always queue the message
            _messageQueue.Enqueue(message);

            if (_debugMode)
                Debug.WriteLine($"[PipeService] Message queued. Queue size: {_messageQueue.Count}");
        }

        private async Task SendMessageAsync(string message)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                if (_debugMode)
                    Debug.WriteLine("[PipeService] SendMessageAsync: Not connected");
                return;
            }

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                await _pipeServer.WriteAsync(lengthBytes, 0, 4);
                await _pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _pipeServer.FlushAsync();

                if (_debugMode)
                    Debug.WriteLine($"[PipeService] Message sent successfully. Bytes: {messageBytes.Length}");
            }
            catch (Exception ex)
            {
                if (_debugMode)
                    Debug.WriteLine($"[PipeService] SendMessageAsync error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_debugMode)
                        Debug.WriteLine("[PipeService] Disposing...");

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
    }
}