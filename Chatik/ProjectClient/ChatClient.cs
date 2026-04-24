using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace ProjectClient
{
    public class ChatClient
    {
        private TcpClient _tcpClient;
        private StreamWriter _writer;
        private StreamReader _reader;

        public event Action<string> OnMessageReceived;
        public event Action OnDisconnected;

        public void Connect(string host, int port, string nickname)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(host, port);

            var stream = _tcpClient.GetStream();
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _reader = new StreamReader(stream);

            _writer.WriteLine($"/join {nickname}");

            new Thread(ReadLoop)
            {
                IsBackground = true
            }.Start();
        }

        private void ReadLoop()
        {
            try
            {
                while (_tcpClient?.Connected == true)
                {
                    var line = _reader.ReadLine();
                    if (line == null) break;

                    OnMessageReceived?.Invoke(line);
                }
            }
            catch
            {
                // соединение потеряно
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                _writer?.WriteLine(message);
            }
            catch
            {
                OnDisconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            try
            {
                _tcpClient?.Close();
            }
            catch { }
        }
    }
}