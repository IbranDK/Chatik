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
                NetworkStream stream = tcpClient.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream) { AutoFlush = true };

                // Первое сообщение — это /join НИК
                string joinLine = reader.ReadLine();
                if (joinLine == null || !joinLine.StartsWith("/join "))
                {
                    tcpClient.Close();
                    return;
                }
                nickname = joinLine.Substring(6).Trim();

                lock (_lockObj)
                {
                    _clients[nickname] = writer;
                }

                OnClientConnected?.Invoke(nickname);
                Broadcast($"[SERVER]: {nickname} вошёл в чат.", exclude: nickname);

                // Цикл чтения сообщений
                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;

                    if (line == "/users")
                    {
                        // Отправить список юзеров только этому клиенту
                        lock (_lockObj)
                        {
                            string userList = "[SERVER]: Онлайн: " + string.Join(", ", _clients.Keys);
                            writer.WriteLine(userList);
                        }
                    }
                    else if (line.StartsWith("/pm "))
                    {
                        // Личное сообщение: /pm Боб Привет
                        var parts = line.Substring(4).Split(' ', 2);
                        if (parts.Length == 2)
                        {
                            string target = parts[0];
                            string pmText = parts[1];
                            lock (_lockObj)
                            {
                                if (_clients.TryGetValue(target, out var targetWriter))
                                {
                                    targetWriter.WriteLine($"[PM от {nickname}]: {pmText}");
                                    writer.WriteLine($"[PM для {target}]: {pmText}");
                                }
                                else
                                {
                                    writer.WriteLine($"[SERVER]: Пользователь {target} не найден.");
                                }
                            }
                        }
                    }
                    else
                    {
                        // Обычное сообщение
                        string msg = $"{nickname}:{line}";
                        OnMessageReceived?.Invoke(nickname, line);
                        Broadcast(msg, exclude: nickname);
                    }
                }
            }
            catch
            {
                // клиент внезапно отключился — это нормально
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
                    Broadcast($"[SERVER]: {nickname} покинул чат.", exclude: null);
                }
                tcpClient.Close();
            }
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