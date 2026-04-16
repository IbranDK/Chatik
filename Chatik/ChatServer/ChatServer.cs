using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    public class ChatServer
    {
        // Добавляем ?, так как эти поля инициализируются позже в методе Start
        private TcpListener? _listener;
        private readonly List<StreamWriter> _clients = new List<StreamWriter>();
        private readonly object _lockObj = new object();

        // События теперь могут быть null (подписчиков может не быть в начале)
        public event Action<string>? OnLog;

        public void Start(int port = 5000)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            OnLog?.Invoke($"Сервер запущен на порту {port}...");

            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        // Используем !, чтобы сказать компилятору: "Тут точно не null"
                        TcpClient client = _listener!.AcceptTcpClient();
                        Thread clientThread = new Thread(HandleClient);
                        clientThread.Start(client);
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke("Ошибка приема: " + ex.Message);
                        break;
                    }
                }
            }).Start();
        }

        // object? позволяет передавать null-аргументы
        private void HandleClient(object? obj)
        {
            if (obj == null) return;
            TcpClient client = (TcpClient)obj;
            StreamWriter? writer = null;
            string userName = "Неизвестный";

            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    writer = new StreamWriter(stream) { AutoFlush = true };

                    string? firstLine = reader.ReadLine();
                    if (firstLine != null && firstLine.StartsWith("/join "))
                    {
                        userName = firstLine.Substring(6);
                        lock (_lockObj) { _clients.Add(writer); }
                        OnLog?.Invoke($"Подключился: {userName}");
                        Broadcast($"Система: {userName} вошел в чат");
                    }

                    string? message;
                    while ((message = reader.ReadLine()) != null)
                    {
                        OnLog?.Invoke($"{userName}: {message}");
                        Broadcast($"{userName}: {message}");
                    }
                }
            }
            catch { /* Ошибка или отключение */ }
            finally
            {
                if (writer != null)
                {
                    lock (_lockObj) { _clients.Remove(writer); }
                }
                OnLog?.Invoke($"{userName} покинул чат");
                Broadcast($"Система: {userName} вышел из чата");
                client.Close();
            }
        }

        private void Broadcast(string message)
        {
            lock (_lockObj)
            {
                foreach (var clientWriter in _clients)
                {
                    try { clientWriter.WriteLine(message); } catch { }
                }
            }
        }
    }
}