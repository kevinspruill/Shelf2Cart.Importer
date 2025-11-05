using System;

namespace Importer.Common.Interfaces
{
    public interface IPipeMessageService
    {
        // Outbound
        void SendMessage(string message);
        bool IsConnected { get; }
        bool SendEnabled { get; set; }
        
        // Lifecycle
        void Initialize();
        void Dispose();
        
        // Inbound
        event EventHandler<string> MessageReceived;
        
    }
}
