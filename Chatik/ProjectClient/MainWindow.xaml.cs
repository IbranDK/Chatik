using System.Windows;
using System.Windows.Input;

namespace ProjectClient
{
    public partial class MainWindow : Window
    {
        private ChatClient _client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            string host = TxtHost.Text.Trim();
            int port = int.Parse(TxtPort.Text.Trim());
            string nick = TxtNick.Text.Trim();

            if (string.IsNullOrEmpty(nick))
            {
                MessageBox.Show("Введите никнейм!");
                return;
            }

            _client = new ChatClient();

            _client.OnMessageReceived += msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageList.Items.Add(msg);
                    MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);

                    // Обновление списка пользователей
                    if (msg.StartsWith("[SERVER]: Онлайн:"))
                    {
                        string usersPart = msg.Replace("[SERVER]: Онлайн:", "").Trim();
                        UserList.Items.Clear();
                        foreach (var u in usersPart.Split(','))
                            UserList.Items.Add(u.Trim());
                    }
                });
            };

            _client.OnDisconnected += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageList.Items.Add("[Соединение разорвано]");
                });
            };

            try
            {
                _client.Connect(host, port, nick);
                Title = $"Чат — {nick}";
                // Запросить список пользователей после подключения
                _client.SendMessage("/users");
            }
            catch
            {
                MessageBox.Show("Не удалось подключиться к серверу.");
            }
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendCurrentMessage();
        }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SendCurrentMessage();
        }

        private void SendCurrentMessage()
        {
            string text = TxtInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || _client == null) return;

            _client.SendMessage(text);
            MessageList.Items.Add($"Я: {text}");
            TxtInput.Clear();
        }
    }
}