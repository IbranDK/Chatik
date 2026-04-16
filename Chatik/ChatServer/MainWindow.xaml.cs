using System;
using System.Windows;
using System.Windows.Controls;

namespace ChatServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Поле для сервера, допускающее значение null до инициализации
        private ChatServer? _server;

        public MainWindow()
        {
            InitializeComponent();
            _server = new ChatServer();
        }

        /// <summary>
        /// Обработчик нажатия кнопки "Запустить сервер"
        /// </summary>
        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null) return;

            // Подписываемся на события логики сервера для вывода сообщений в лог
            _server.OnLog += (message) =>
            {
                // Используем Dispatcher, так как события приходят из фонового потока
                Dispatcher.Invoke(() =>
                {
                    LogBox.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

                    // Автоматическая прокрутка списка к последнему элементу
                    if (LogBox.Items.Count > 0)
                    {
                        LogBox.ScrollIntoView(LogBox.Items[LogBox.Items.Count - 1]);
                    }
                });
            };

            try
            {
                // Запуск прослушивания на порту 5000
                _server.Start(5000);

                // Визуальное подтверждение запуска
                if (sender is Button btn)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Сервер работает";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить сервер: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}