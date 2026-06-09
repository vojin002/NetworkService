using NetworkService.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.Views
{
    public partial class MeasurementGraphView : UserControl
    {
        private MeasurementGraphViewModel vm;

        public MeasurementGraphView()
        {
            InitializeComponent();
            DataContextChanged += MeasurementGraphView_DataContextChanged;
            SizeChanged += (s, e) => DrawGraph();
        }

        private void MeasurementGraphView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (vm != null)
                vm.PropertyChanged -= Vm_PropertyChanged;

            vm = DataContext as MeasurementGraphViewModel;

            if (vm != null)
                vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "GraphPoints")
                DrawGraph();
        }

        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;

            if (vm == null || vm.GraphPoints == null || vm.GraphPoints.Count == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "No data",
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 190, 210)),
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold
                };
                noDataText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(noDataText, (canvasWidth - noDataText.DesiredSize.Width) / 2);
                Canvas.SetTop(noDataText, canvasHeight / 2 - 30);
                GraphCanvas.Children.Add(noDataText);

                var subText = new TextBlock
                {
                    Text = "Select a sensor and wait for measurements",
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 190, 210)),
                    FontSize = 13
                };
                subText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(subText, (canvasWidth - subText.DesiredSize.Width) / 2);
                Canvas.SetTop(subText, canvasHeight / 2 + 5);
                GraphCanvas.Children.Add(subText);
                return;
            }

            List<MeasurementPoint> points = vm.GraphPoints;

            if (canvasWidth < 10 || canvasHeight < 10) return;

            double circleRadius = 28;
            double xAxisY = canvasHeight - 40;
            double topMargin = 40;

            int count = points.Count;
            double spacing = (canvasWidth - 2 * circleRadius) / (count > 1 ? count - 1 : 1);

            double maxVal = 400;
            double minVal = 200;

            var centerPoints = new List<Point>();

            for (int i = 0; i < count; i++)
            {
                double x = circleRadius + i * spacing;
                double normalizedY = (points[i].Value - minVal) / (maxVal - minVal);
                double y = xAxisY - topMargin - normalizedY * (xAxisY - topMargin - circleRadius);

                centerPoints.Add(new Point(x, y));
            }

            for (int i = 0; i < centerPoints.Count - 1; i++)
            {
                var line = new Line
                {
                    X1 = centerPoints[i].X,
                    Y1 = centerPoints[i].Y,
                    X2 = centerPoints[i + 1].X,
                    Y2 = centerPoints[i + 1].Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    StrokeThickness = 2
                };
                GraphCanvas.Children.Add(line);
            }

            for (int i = 0; i < count; i++)
            {
                bool isValid = points[i].IsValid;
                var fillColor = isValid
                    ? Color.FromRgb(0, 212, 170)
                    : Color.FromRgb(255, 68, 68);

                var circle = new Ellipse
                {
                    Width = circleRadius * 2,
                    Height = circleRadius * 2,
                    Fill = new SolidColorBrush(fillColor),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.5
                };

                Canvas.SetLeft(circle, centerPoints[i].X - circleRadius);
                Canvas.SetTop(circle, centerPoints[i].Y - circleRadius);
                GraphCanvas.Children.Add(circle);

                var valueText = new TextBlock
                {
                    Text = points[i].Value.ToString("F0"),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Width = circleRadius * 2
                };

                Canvas.SetLeft(valueText, centerPoints[i].X - circleRadius);
                Canvas.SetTop(valueText, centerPoints[i].Y - 8);
                GraphCanvas.Children.Add(valueText);

                string timeLabel = points[i].Timestamp;
                if (timeLabel.Length > 8)
                    timeLabel = timeLabel.Substring(timeLabel.Length - 8);

                var timeText = new TextBlock
                {
                    Text = timeLabel,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    FontSize = 10,
                    TextAlignment = TextAlignment.Center,
                    Width = 80
                };

                Canvas.SetLeft(timeText, centerPoints[i].X - 40);
                Canvas.SetTop(timeText, xAxisY - 15);
                GraphCanvas.Children.Add(timeText);
            }

            var xAxis = new Line
            {
                X1 = 0,
                Y1 = xAxisY,
                X2 = canvasWidth,
                Y2 = xAxisY,
                Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(xAxis);

            var legendValid = new Ellipse
            {
                Width = 12, Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(0, 212, 170))
            };
            Canvas.SetLeft(legendValid, canvasWidth - 160);
            Canvas.SetTop(legendValid, 5);
            GraphCanvas.Children.Add(legendValid);

            var legendValidText = new TextBlock
            {
                Text = "Valid (250-350°C)",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 11
            };
            Canvas.SetLeft(legendValidText, canvasWidth - 143);
            Canvas.SetTop(legendValidText, 3);
            GraphCanvas.Children.Add(legendValidText);

            var legendInvalid = new Ellipse
            {
                Width = 12, Height = 12,
                Fill = new SolidColorBrush(Color.FromRgb(255, 68, 68))
            };
            Canvas.SetLeft(legendInvalid, canvasWidth - 160);
            Canvas.SetTop(legendInvalid, 22);
            GraphCanvas.Children.Add(legendInvalid);

            var legendInvalidText = new TextBlock
            {
                Text = "Out of range",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 11
            };
            Canvas.SetLeft(legendInvalidText, canvasWidth - 143);
            Canvas.SetTop(legendInvalidText, 20);
            GraphCanvas.Children.Add(legendInvalidText);
        }
    }
}
