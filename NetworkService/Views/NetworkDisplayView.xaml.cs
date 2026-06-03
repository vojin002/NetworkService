using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        public NetworkDisplayView()
        {
            InitializeComponent();
        }

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void CanvasGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void CanvasGrid_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
