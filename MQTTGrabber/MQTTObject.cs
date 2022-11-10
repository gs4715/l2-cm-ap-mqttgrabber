using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using MQTTnet;
using System.Threading;
using System;

namespace MQTTGrabber
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Topic { get; set; }
        public byte[] Payload { get; set; }
        public MessageReceivedEventArgs(string topic, byte[] payload)
        {
            Topic = topic;
            Payload = payload;
        }
    }
    public class MQTTObject
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public delegate void MessageRecievedHandler(object sender, MessageReceivedEventArgs e);
        public event MessageRecievedHandler MessageReceived;
        private readonly string _server = string.Empty;
        private readonly List<string> _tags;
        private IMqttClient _client = null;
        private bool _isConnected = false;

        public MQTTObject(string server, List<string> tags)
        {
            _server = server;
            _tags = tags;
            IMqttFactory mqttFactory = new MqttFactory();
            _client = mqttFactory.CreateMqttClient();
        }
        public bool IsConnected { get => _isConnected; set => _isConnected = value; } // Don't think I need this now.

        public async void Start()
        {
            _client.UseDisconnectedHandler(DisconnectHandler);
            _client.UseConnectedHandler(ConnectHandler);
            _client.UseApplicationMessageReceivedHandler(MessageReceivedHandler);
            await ConnectToMQttBroker();
        }
        private async Task ConnectToMQttBroker()
        {
            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_server)
                .Build();
            MqttClientConnectResult response = await _client.ConnectAsync(mqttClientOptions, CancellationToken.None);
            foreach (string tag in _tags)
            {
                await _client.SubscribeAsync(tag);
            }
        }
        private async Task MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            MessageReceived(this, new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload));
        }
        private async Task ConnectHandler(MqttClientConnectedEventArgs e)
        {
            IsConnected = true;
            RaisePropertyChanged(nameof(IsConnected));
        }
        private async Task DisconnectHandler(MqttClientDisconnectedEventArgs e)
        {
            IsConnected = false;
            RaisePropertyChanged(nameof(IsConnected));
            Thread.Sleep(5000);
            await ConnectToMQttBroker();
        }
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
