using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SA_DE_OM_Changer
{
    public partial class AddVersionWindow : Window
    {
        private readonly List<GameInfo> _games;

        public string SelectedGame { get; private set; } = "";
        public string GameVersion { get; private set; } = "";
        public string AddressHex { get; private set; } = "";

        public AddVersionWindow(List<GameInfo> supportedGames)
        {
            InitializeComponent();
            _games = supportedGames;
            GameComboBox.ItemsSource = _games;
            GameComboBox.DisplayMemberPath = "DisplayName";
            if (_games.Any())
                GameComboBox.SelectedIndex = 0;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (GameComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a game.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedGame = (GameInfo)GameComboBox.SelectedItem;
            SelectedGame = selectedGame.ProcessName;
            GameVersion = VersionTextBox.Text.Trim();
            AddressHex = AddressTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(GameVersion))
            {
                MessageBox.Show("Please enter a version string.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressHex))
            {
                MessageBox.Show("Please enter an address.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}