using NetworkService.Model;
using NetworkService.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Views
{
    public partial class NetworkDisplayView : UserControl
    {
        private TemperatureSensor draggedSensor;
        private Border dragSourceBorder;

        private bool connectMode = false;
        private Border firstConnectBorder = null;

        private List<Border[]> connections = new List<Border[]>();

        public NetworkDisplayView()
        {
            InitializeComponent();
            Loaded += NetworkDisplayView_Loaded;
        }

        private void NetworkDisplayView_Loaded(object sender, RoutedEventArgs e)
        {
            CreateSlots();
        }

        private void CreateSlots()
        {
            CanvasGrid.Children.Clear();
            for (int i = 0; i < 12; i++)
            {
                var slot = new Border
                {
                    Margin = new Thickness(4),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(240, 244, 248)),
                    CornerRadius = new CornerRadius(4),
                    AllowDrop = true,
                    Tag = null,
                    Cursor = Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = "+",
                        FontSize = 18,
                        Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                slot.Drop += Slot_Drop;
                slot.DragOver += Slot_DragOver;
                slot.MouseLeftButtonDown += Slot_MouseLeftButtonDown;
                CanvasGrid.Children.Add(slot);
            }
        }

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var sensor = (e.OriginalSource as FrameworkElement)?.DataContext as TemperatureSensor;
            if (sensor == null) return;

            draggedSensor = sensor;
            dragSourceBorder = null;
            DragDrop.DoDragDrop(SensorTreeView, sensor, DragDropEffects.Move);
        }

        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var slot = sender as Border;
            if (slot == null) return;

            if (connectMode)
            {
                HandleConnectClick(slot);
                return;
            }

            var sensor = slot.Tag as TemperatureSensor;
            if (sensor == null) return;

            draggedSensor = sensor;
            dragSourceBorder = slot;
            DragDrop.DoDragDrop(slot, sensor, DragDropEffects.Move);
        }

        private void HandleConnectClick(Border slot)
        {
            if (slot.Tag as TemperatureSensor == null) return;

            if (firstConnectBorder == null)
            {
                firstConnectBorder = slot;
                slot.BorderBrush = new SolidColorBrush(Colors.Yellow);
                slot.BorderThickness = new Thickness(2);
            }
            else
            {
                if (firstConnectBorder == slot)
                {
                    ResetSlotBorder(firstConnectBorder);
                    firstConnectBorder = null;
                    return;
                }

                bool alreadyConnected = false;
                foreach (var conn in connections)
                {
                    if ((conn[0] == firstConnectBorder && conn[1] == slot) ||
                        (conn[0] == slot && conn[1] == firstConnectBorder))
                    {
                        alreadyConnected = true;
                        break;
                    }
                }

                if (!alreadyConnected)
                    connections.Add(new Border[] { firstConnectBorder, slot });

                ResetSlotBorder(firstConnectBorder);
                firstConnectBorder = null;
                DrawLines();

                connectMode = false;
                ConnectBtn.Content = "Connect Sensors";
                ConnectBtn.Style = (Style)FindResource("NavButtonStyle");
            }
        }

        private void ResetSlotBorder(Border slot)
        {
            var sensor = slot.Tag as TemperatureSensor;
            if (sensor != null && !sensor.IsValueValid)
            {
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                slot.BorderThickness = new Thickness(2);
            }
            else
            {
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 224));
                slot.BorderThickness = new Thickness(1);
            }
        }

        private void Slot_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
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

        private void Slot_Drop(object sender, DragEventArgs e)
        {
            var targetSlot = sender as Border;
            if (targetSlot == null || draggedSensor == null) return;

            var existingSensor = targetSlot.Tag as TemperatureSensor;
            var vm = DataContext as NetworkDisplayViewModel;

            if (dragSourceBorder != null)
            {
                ClearSlot(dragSourceBorder);

                if (existingSensor != null)
                    SetSlotContent(dragSourceBorder, existingSensor);
            }
            else
            {
                if (vm != null)
                    vm.SensorsInTreeView.Remove(draggedSensor);

                if (existingSensor != null && vm != null)
                    vm.SensorsInTreeView.Add(existingSensor);
            }

            SetSlotContent(targetSlot, draggedSensor);

            DrawLines();

            draggedSensor = null;
            dragSourceBorder = null;
        }

        private void SetSlotContent(Border slot, TemperatureSensor sensor)
        {
            var panel = new StackPanel { Margin = new Thickness(4) };

            var nameText = new TextBlock
            {
                Text = sensor.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };

            var idText = new TextBlock
            {
                Text = "ID: " + sensor.Id,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                FontSize = 10
            };

            var valueText = new TextBlock
            {
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80))
            };
            var valueBinding = new System.Windows.Data.Binding("LastMeasuredValue")
            {
                Source = sensor,
                StringFormat = "Val: {0:F1} °C"
            };
            valueText.SetBinding(TextBlock.TextProperty, valueBinding);

            var statusText = new TextBlock { FontSize = 10, FontWeight = FontWeights.Bold };
            statusText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("IsValueValid")
            {
                Source = sensor,
                Converter = new BoolToStatusConverter()
            });
            statusText.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("IsValueValid")
            {
                Source = sensor,
                Converter = new BoolToColorConverter()
            });

            panel.Children.Add(nameText);
            panel.Children.Add(idText);
            panel.Children.Add(valueText);
            panel.Children.Add(statusText);

            slot.Child = panel;
            slot.Tag = sensor;

            if (!sensor.IsValueValid)
            {
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 68, 68));
                slot.BorderThickness = new Thickness(2);
            }
            else
            {
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 224));
                slot.BorderThickness = new Thickness(1);
            }
        }

        private void ClearSlot(Border slot)
        {
            slot.Tag = null;
            slot.BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 224));
            slot.BorderThickness = new Thickness(1);
            slot.Child = new TextBlock
            {
                Text = "+",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 220)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void AutoArrangeBtn_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as NetworkDisplayViewModel;
            if (vm == null) return;

            var toPlace = new List<TemperatureSensor>(vm.SensorsInTreeView);

            foreach (Border slot in CanvasGrid.Children)
            {
                if (toPlace.Count == 0) break;
                if (slot.Tag is TemperatureSensor) continue;

                var sensor = toPlace[0];
                toPlace.RemoveAt(0);
                vm.SensorsInTreeView.Remove(sensor);
                SetSlotContent(slot, sensor);
            }

            DrawLines();
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            connectMode = !connectMode;

            if (connectMode)
            {
                ConnectBtn.Content = "Cancel Connect";
                ConnectBtn.Style = (Style)FindResource("DangerButtonStyle");
                firstConnectBorder = null;
            }
            else
            {
                ConnectBtn.Content = "Connect Sensors";
                ConnectBtn.Style = (Style)FindResource("NavButtonStyle");
                if (firstConnectBorder != null)
                {
                    ResetSlotBorder(firstConnectBorder);
                    firstConnectBorder = null;
                }
            }
        }

        private void RemoveConnectionsForSlot(Border slot)
        {
            if (slot == null) return;

            var toRemove = new List<Border[]>();
            foreach (var conn in connections)
            {
                if (conn[0] == slot || conn[1] == slot)
                    toRemove.Add(conn);
            }
            foreach (var conn in toRemove)
                connections.Remove(conn);
        }

        public void RemoveSensorFromGrid(TemperatureSensor sensor)
        {
            foreach (Border slot in CanvasGrid.Children)
            {
                if (slot.Tag == sensor)
                {
                    RemoveConnectionsForSlot(slot);
                    ClearSlot(slot);
                    var vm = DataContext as NetworkDisplayViewModel;
                    if (vm != null)
                        vm.SensorsInTreeView.Add(sensor);
                    break;
                }
            }
            DrawLines();
        }

        private void DrawLines()
        {
            LinesCanvas.Children.Clear();

            foreach (var conn in connections)
            {
                var p1 = GetSlotCenter(conn[0]);
                var p2 = GetSlotCenter(conn[1]);

                if (p1 == null || p2 == null) continue;

                var line = new Line
                {
                    X1 = p1.Value.X,
                    Y1 = p1.Value.Y,
                    X2 = p2.Value.X,
                    Y2 = p2.Value.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 212, 170)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 }
                };

                LinesCanvas.Children.Add(line);
            }
        }

        private Point? GetSlotCenter(Border slot)
        {
            if (slot == null) return null;

            try
            {
                var parent = LinesCanvas.Parent as UIElement;
                if (parent == null) return null;
                var transform = slot.TransformToAncestor(parent);
                var topLeft = transform.Transform(new Point(0, 0));
                return new Point(topLeft.X + slot.ActualWidth / 2, topLeft.Y + slot.ActualHeight / 2);
            }
            catch
            {
                return null;
            }
        }
    }

    public class BoolToStatusConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;
            return b ? "Normal" : "! ALARM";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class BoolToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;
            if (b)
                return new SolidColorBrush(Color.FromRgb(0, 150, 100));
            return new SolidColorBrush(Color.FromRgb(211, 47, 47));
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
