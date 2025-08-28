using Importer.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
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

        public bool IsConnected => _pipeServer?.IsConnected == true;

        public PipeMessageService(string pipeName)
        {
            _pipeName = pipeName;
            _cancellationTokenSource = new CancellationTokenSource();
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

                    // Keep the connection alive
                    while (!cancellationToken.IsCancellationRequested && _pipeServer.IsConnected)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Server was stopped, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    // Log using the static logger (but be careful of circular dependencies)
                    await Task.Delay(1000, cancellationToken);
                }
                finally
                {
                    _pipeServer?.Dispose();
                }
            }
        }

        public void SendMessage(string message)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                _pipeServer.Write(lengthBytes, 0, 4);
                _pipeServer.Write(messageBytes, 0, messageBytes.Length);
                _pipeServer.Flush();
            }
            catch (Exception ex)
            {
                // Log using the static logger (but be careful of circular dependencies)
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
                    _cancellationTokenSource.Cancel();
                    _serverTask?.Wait(1000);
                    _pipeServer?.Dispose();
                    _cancellationTokenSource?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
