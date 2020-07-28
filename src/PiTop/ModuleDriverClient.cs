﻿using System;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

namespace PiTop
{
    public interface IModuleDriverClient : IDisposable
    {
        event EventHandler<PiTopMessage>? MessageReceived;
        void Start();
    }

    internal class ModuleDriverClient : IModuleDriverClient
    {
        private readonly SubscriberSocket _reponseSocket;
        private readonly NetMQPoller _poller;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly RequestSocket _requestSocket;
        public event EventHandler<PiTopMessage>? MessageReceived;

        public ModuleDriverClient()
        {
            _requestSocket = new RequestSocket();
            _reponseSocket = new SubscriberSocket();
            _cancellationSource = new CancellationTokenSource();
            _reponseSocket.ReceiveReady += ResponseSocketOnReceiveReady;
            _poller = new NetMQPoller
            {
                _reponseSocket
            };
        }

        public void AcquireDisplay()
        {
            var request = new PiTopMessage(PiTopMessageId.REQ_SET_OLED_CONTROL, "1");
            _requestSocket.SendFrame(request.ToString());
        }

        public void ReleaseDisplay()
        {
            var request = new PiTopMessage(PiTopMessageId.REQ_SET_OLED_CONTROL, "0");
            _requestSocket.SendFrame(request.ToString());
        }

        public void Start()
        {
            _requestSocket.Connect("tcp://127.0.0.1:3782");
            _reponseSocket.Connect("tcp://127.0.0.1:3781");
            _reponseSocket.Subscribe("");
            Task.Run(() => { _poller.Run(); }, _cancellationSource.Token);
        }

        private void ResponseSocketOnReceiveReady(object? sender, NetMQSocketEventArgs e)
        {
            var message = PiTopMessage.Parse(e.Socket.ReceiveFrameString());
            MessageReceived?.Invoke(this, message);
        }

        public void Dispose()
        {
            _cancellationSource.Dispose();
            if (_poller.IsRunning)
            {
                _poller.Stop();
            }
            
            _reponseSocket.Disconnect("tcp://127.0.0.1:3781");
            _requestSocket.Disconnect("tcp://127.0.0.1:3782");
            _requestSocket.Dispose();
            _poller.RemoveAndDispose(_reponseSocket);
        }
    }
}