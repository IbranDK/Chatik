using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ProjectServer
{
    public class ChatServer
    {
        private TcpListener _listener;
        private readonly object _lockObj = new object();
        private Dictionary<string, StreamWriter> _clients = new Dictionary<string, StreamWriter>();
        private bool _isRunning = false;

        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;
        public event Action<string, string> OnMessageReceived;

        public void Start(int port = 5000)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            Thread acceptThread = new Thread(AcceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        private void AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch
                {
                    // сервер остановлен
                    break;
                }
            }
        }

        private void HandleClient(TcpClient tcpClient)
        {
            StreamReader reader = null;
            StreamWriter writer = null;
            string nickname = null;

            try
            {
                var stream = tcpClient.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                var joinLine = reader.ReadLine();
                if (joinLine == null || !joinLine.StartsWith("/join "))
                {
                    tcpClient.Close();
                    return;
                }

                nickname = joinLine.Substring(6).Trim();

                lock (_lockObj)
                {
                    if (string.IsNullOrWhiteSpace(nickname) || _clients.ContainsKey(nickname))
                    {
                        writer.WriteLine("[SERVER]: Invalid or duplicate nickname.");
                        tcpClient.Close();
                        return;
                    }

                    _clients[nickname] = writer;
                }

                OnClientConnected?.Invoke(nickname);
                Broadcast($"[SERVER]: {nickname} вошёл в чат.", nickname);

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null) break;

                    HandleCommand(line, nickname, writer);
                }
            }
            catch
            {
                // клиент отвалился
            }
            finally
            {
                if (nickname != null)
                {
                    lock (_lockObj)
                    {
                        _clients.Remove(nickname);
                    }

                    OnClientDisconnected?.Invoke(nickname);
                    Broadcast($"[SERVER]: {nickname} покинул чат.", null);
                }

                reader?.Dispose();
                writer?.Dispose();
                tcpClient.Close();
            }
        }
        private void HandleCommand(string line, string nickname, StreamWriter writer)
        {
            if (line == "/users")
            {
                List<string> users;
                lock (_lockObj)
                {
                    users = new List<string>(_clients.Keys);
                }

                writer.WriteLine("[SERVER]: Онлайн: " + string.Join(", ", users));
                return;
            }

            if (line.StartsWith("/pm "))
            {
                var parts = line.Substring(4).Split(' ', 2);
                if (parts.Length < 2) return;

                var target = parts[0];
                var message = parts[1];

                StreamWriter targetWriter = null;

                lock (_lockObj)
                {
                    _clients.TryGetValue(target, out targetWriter);
                }

                if (targetWriter != null)
                {
                    targetWriter.WriteLine($"[PM от {nickname}]: {message}");
                    writer.WriteLine($"[PM для {target}]: {message}");
                }
                else
                {
                    writer.WriteLine($"[SERVER]: Пользователь {target} не найден.");
                }

                return;
            }

            OnMessageReceived?.Invoke(nickname, line);
            Broadcast($"{nickname}: {line}", nickname);
        }
        private void Broadcast(string message, string exclude)
        {
            List<StreamWriter> clientsCopy;
            lock (_lockObj)
            {
                clientsCopy = new List<StreamWriter>(_clients.Values);
            }

            foreach (var writer in clientsCopy)
            {
                try
                {
                    writer.WriteLine(message);
                }
                catch { }
            }
        }
    }
}