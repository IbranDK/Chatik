using System;
using System.Windows;

namespace ProjectServer
{
    public partial class MainWindow : Window
    {
        private ChatServer _server;
        private int _connectionCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            _server = new ChatServer();

            _server.OnClientConnected += nick =>
            {
                _connectionCount++;
                Dispatcher.Invoke(() =>
                {
                    LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] Подключился: {nick}");
                    TxtConnections.Text = $"Подключений: {_connectionCount}";
                });
            };

            _server.OnClientDisconnected += nick =>
            {
                _connectionCount--;
                Dispatcher.Invoke(() =>
                {
                    LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] Отключился: {nick}");
                    TxtConnections.Text = $"Подключений: {_connectionCount}";
                });
            };

            _server.OnMessageReceived += (nick, text) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {nick}: {text}");
                });
            };
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _server.Start(5000);
            TxtStatus.Text = "Статус: запущен на порту 5000";
            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] Сервер запущен на порту 5000");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
            TxtStatus.Text = "Статус: остановлен";
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            LogList.Items.Add($"[{DateTime.Now:HH:mm:ss}] Сервер остановлен");
        }
    }
}