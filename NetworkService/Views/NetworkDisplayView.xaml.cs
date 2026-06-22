using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private Point _dragStartPoint;
        private bool _pendingTreeDrag = false;
        private bool _pendingSlotDrag = false;

        private List<TemperatureSensor[]> connections = new List<TemperatureSensor[]>();
        private Dictionary<TemperatureSensor, PropertyChangedEventHandler> sensorHandlers = new Dictionary<TemperatureSensor, PropertyChangedEventHandler>();
        private Dictionary<TemperatureSensor, Border> sensorSlotMap = new Dictionary<TemperatureSensor, Border>();

        public NetworkDisplayView()
        {
            InitializeComponent();
            Loaded += NetworkDisplayView_Loaded;
        }

        private void NetworkDisplayView_Loaded(object sender, RoutedEventArgs e)
        {
            CreateSlots();
            if (NetworkEntitiesViewModel.AllSensors != null)
                NetworkEntitiesViewModel.AllSensors.CollectionChanged += OnAllSensorsChanged;
        }

        private void OnAllSensorsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null) return;
            foreach (TemperatureSensor sensor in e.OldItems)
                RemoveSensorFromGrid(sensor);
        }

        private void CreateSlots()
        {
            CanvasGrid.Children.Clear();
            for (int i = 0; i < 12; i++)
            {
                var slot = new Border
                {
                    Margin = new Thickness(4),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(218, 226, 236)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    CornerRadius = new CornerRadius(6),
                    AllowDrop = true,
                    Tag = null,
                    Cursor = Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = "+",
                        FontSize = 22,
                        FontWeight = FontWeights.Light,
                        Foreground = new SolidColorBrush(Color.FromRgb(195, 210, 225)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                slot.Drop += Slot_Drop;
                slot.DragOver += Slot_DragOver;
                slot.MouseLeftButtonDown += Slot_MouseLeftButtonDown;
                slot.MouseMove += Slot_MouseMove;
                CanvasGrid.Children.Add(slot);
            }
        }

        private TreeViewItem FindTreeViewItem(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);
            return source as TreeViewItem;
        }

        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;

            if (element != null && element.DataContext is SensorTypeGroup)
            {
                var treeViewItem = FindTreeViewItem(element);
                if (treeViewItem != null)
                {
                    treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
                    e.Handled = true;
                    return;
                }
            }

            TemperatureSensor sensor = null;
            if (element != null)
                sensor = element.DataContext as TemperatureSensor;
            if (sensor == null) return;

            draggedSensor = sensor;
            dragSourceBorder = null;
            _dragStartPoint = e.GetPosition(null);
            _pendingTreeDrag = true;
        }

        private void TreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_pendingTreeDrag || e.LeftButton != MouseButtonState.Pressed)
            {
                _pendingTreeDrag = false;
                return;
            }

            Point pos = e.GetPosition(null);
            bool movedEnough = Math.Abs(pos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;
            if (movedEnough)
            {
                _pendingTreeDrag = false;
                if (draggedSensor != null)
                    DragDrop.DoDragDrop(SensorTreeView, draggedSensor, DragDropEffects.Move);
            }
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
            _dragStartPoint = e.GetPosition(null);
            _pendingSlotDrag = true;
        }

        private void Slot_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_pendingSlotDrag || e.LeftButton != MouseButtonState.Pressed)
            {
                _pendingSlotDrag = false;
                return;
            }

            Point pos = e.GetPosition(null);
            bool movedEnough = Math.Abs(pos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;
            if (movedEnough)
            {
                _pendingSlotDrag = false;
                if (draggedSensor != null && dragSourceBorder != null)
                    DragDrop.DoDragDrop(dragSourceBorder, draggedSensor, DragDropEffects.Move);
            }
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

                var sensorA = firstConnectBorder.Tag as TemperatureSensor;
                var sensorB = slot.Tag as TemperatureSensor;

                bool alreadyConnected = false;
                foreach (var conn in connections)
                {
                    if ((conn[0] == sensorA && conn[1] == sensorB) || (conn[0] == sensorB && conn[1] == sensorA))
                    {
                        alreadyConnected = true;
                        break;
                    }
                }

                if (!alreadyConnected)
                    connections.Add(new TemperatureSensor[] { sensorA, sensorB });

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
            if (sensor != null)
            {
                if (sensor.IsValueValid)
                    slot.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                else
                    slot.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                slot.BorderThickness = new Thickness(3, 1, 1, 1);
            }
            else
            {
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 226, 236));
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
                {
                    vm.SensorsInTreeView.Remove(draggedSensor);
                    vm.RemoveFromGroup(draggedSensor);
                }

                if (existingSensor != null && vm != null)
                {
                    RemoveConnectionsForSensor(existingSensor);
                    vm.SensorsInTreeView.Add(existingSensor);
                    vm.AddToGroup(existingSensor);
                }
            }

            SetSlotContent(targetSlot, draggedSensor);

            DrawLines();

            draggedSensor = null;
            dragSourceBorder = null;
        }

        private void SetSlotContent(Border slot, TemperatureSensor sensor)
        {
            var oldSensor = slot.Tag as TemperatureSensor;
            if (oldSensor != null && sensorSlotMap.ContainsKey(oldSensor) && sensorSlotMap[oldSensor] == slot)
            {
                oldSensor.PropertyChanged -= sensorHandlers[oldSensor];
                sensorHandlers.Remove(oldSensor);
                sensorSlotMap.Remove(oldSensor);
            }

            var panel = new StackPanel { Margin = new Thickness(4) };
            panel.Children.Add(new TextBlock { Text = sensor.Name, Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)), FontWeight = FontWeights.Bold, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = "ID: " + sensor.Id, Foreground = new SolidColorBrush(Color.FromRgb(100, 110, 120)), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center });

            var valueText = new TextBlock { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
            var statusText = new TextBlock { FontSize = 12, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            panel.Children.Add(valueText);
            panel.Children.Add(statusText);

            slot.Child = panel;
            slot.Tag = sensor;
            slot.Background = new SolidColorBrush(Colors.White);

            UpdateSlotValues(slot, sensor, valueText, statusText);

            PropertyChangedEventHandler handler = (s, args) => UpdateSlotValues(slot, sensor, valueText, statusText);
            sensor.PropertyChanged += handler;
            sensorHandlers[sensor] = handler;
            sensorSlotMap[sensor] = slot;
        }

        private void UpdateSlotValues(Border slot, TemperatureSensor sensor, TextBlock valueText, TextBlock statusText)
        {
            if (sensor.LastMeasuredValue.HasValue)
                valueText.Text = "Val: " + sensor.LastMeasuredValue.Value.ToString("F1") + " °C";
            else
                valueText.Text = "Val: N/A";
            if (sensor.IsValueValid)
            {
                statusText.Text = "Normal";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(0, 150, 100));
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            }
            else
            {
                statusText.Text = "! ALARM";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                slot.BorderBrush = new SolidColorBrush(Color.FromRgb(211, 47, 47));
            }
            slot.BorderThickness = new Thickness(3, 1, 1, 1);
        }

        private void ClearSlot(Border slot)
        {
            var oldSensor = slot.Tag as TemperatureSensor;
            if (oldSensor != null && sensorHandlers.ContainsKey(oldSensor))
            {
                oldSensor.PropertyChanged -= sensorHandlers[oldSensor];
                sensorHandlers.Remove(oldSensor);
                sensorSlotMap.Remove(oldSensor);
            }
            slot.Tag = null;
            slot.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 226, 236));
            slot.BorderThickness = new Thickness(1);
            slot.Background = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            slot.CornerRadius = new CornerRadius(6);
            slot.Child = new TextBlock
            {
                Text = "+",
                FontSize = 22,
                FontWeight = FontWeights.Light,
                Foreground = new SolidColorBrush(Color.FromRgb(195, 210, 225)),
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
                vm.RemoveFromGroup(sensor);
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

        private void RemoveConnectionsForSensor(TemperatureSensor sensor)
        {
            if (sensor == null) return;

            var toRemove = new List<TemperatureSensor[]>();
            foreach (var conn in connections)
            {
                if (conn[0] == sensor || conn[1] == sensor)
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
                    if (firstConnectBorder == slot)
                    {
                        firstConnectBorder = null;
                        connectMode = false;
                        ConnectBtn.Content = "Connect Sensors";
                        ConnectBtn.Style = (Style)FindResource("NavButtonStyle");
                    }
                    RemoveConnectionsForSensor(sensor);
                    ClearSlot(slot);
                    var vm = DataContext as NetworkDisplayViewModel;
                    bool sensorStillExists = NetworkEntitiesViewModel.AllSensors != null && NetworkEntitiesViewModel.AllSensors.Contains(sensor);
                    if (vm != null && sensorStillExists)
                    {
                        vm.SensorsInTreeView.Add(sensor);
                        vm.AddToGroup(sensor);
                    }
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
                Border slot1, slot2;
                if (!sensorSlotMap.TryGetValue(conn[0], out slot1)) continue;
                if (!sensorSlotMap.TryGetValue(conn[1], out slot2)) continue;

                var p1 = GetSlotCenter(slot1);
                var p2 = GetSlotCenter(slot2);

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

}
