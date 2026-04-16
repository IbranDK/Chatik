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

            NetworkStream stream = _tcpClient.GetStream();
            _writer = new StreamWriter(stream) { AutoFlush = true };
            _reader = new StreamReader(stream);

            // Регистрация никнейма
            _writer.WriteLine($"/join {nickname}");

            // Фоновый поток чтения
            Thread readThread = new Thread(ReadLoop);
            readThread.IsBackground = true;
            readThread.Start();
        }

        private void ReadLoop()
        {
            try
            {
                while (true)
                {
                    string line = _reader.ReadLine();
                    if (line == null) break;
                    OnMessageReceived?.Invoke(line);
                }
            }
            catch
            {
                // соединение разорвано
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
                // соединение потеряно
            }
        }

        public void Disconnect()
        {
            _tcpClient?.Close();
        }
    }
}