using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace darts_hub.UI
{
    public partial class Robbel3DConfirmDialog : Window
    {
        public ObservableCollection<string> Items { get; } = new();
        public int LedCount { get; }
        public Robbel3DConfirmDialog(int ledCount)
        {
            LedCount = ledCount;
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public Robbel3DConfirmDialog SetItems(IEnumerable<string> items)
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            return this;
        }

        private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
