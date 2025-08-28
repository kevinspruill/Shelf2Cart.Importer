using DocumentFormat.OpenXml.Office2016.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Importer.UI.Services
{
    public class PipeClientService : IDisposable
    {
        private NamedPipeClientStream _clientStream;
        private bool _disposed = false;

        public bool IsConnected => _clientStream?.IsConnected == true;

        public PipeClientService(string pipeName)
        {
            Connect(pipeName);
        }

        public void Connect(string pipeName)
        {
            try
            {
                _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                _clientStream.Connect(5000);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException("Connection timeout. Make sure the server is running.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Connection error: {ex.Message}");
            }
        }

        public async Task<string> ReadFromPipeAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Not connected to server.");

            try
            {
                // Read message length
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await _clientStream.ReadAsync(lengthBuffer, 0, 4);
                if (bytesRead != 4)
                    throw new IOException("Failed to read message length.");

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                // Read the message
                byte[] messageBuffer = new byte[messageLength];
                bytesRead = await _clientStream.ReadAsync(messageBuffer, 0, messageLength);
                if (bytesRead != messageLength)
                    throw new IOException("Failed to read complete message.");

                return Encoding.UTF8.GetString(messageBuffer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Read error: {ex.Message}");
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
                    _clientStream?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
