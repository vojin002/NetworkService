using NetworkService.Helpers;
using NetworkService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Threading;

namespace NetworkService.ViewModel
{
    public class MeasurementGraphViewModel : BindableBase
    {
        private TemperatureSensor selectedSensor;
        private ObservableCollection<TemperatureSensor> sensorList;
        private List<MeasurementPoint> graphPoints;

        private DispatcherTimer refreshTimer;

        public MyICommand RefreshGraphCommand { get; set; }

        public MeasurementGraphViewModel()
        {
            SensorList = new ObservableCollection<TemperatureSensor>();
            GraphPoints = new List<MeasurementPoint>();
            RefreshGraphCommand = new MyICommand(OnRefreshGraph);

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(3);
            refreshTimer.Tick += (s, e) => OnRefreshGraph();
            refreshTimer.Start();

            LoadSensors();

            if (NetworkEntitiesViewModel.AllSensors != null)
                NetworkEntitiesViewModel.AllSensors.CollectionChanged += AllSensors_CollectionChanged;
        }

        private void AllSensors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TemperatureSensor sensor in e.NewItems)
                    SensorList.Add(sensor);
            }
            if (e.OldItems != null)
            {
                foreach (TemperatureSensor sensor in e.OldItems)
                {
                    SensorList.Remove(sensor);
                    if (SelectedSensor == sensor)
                    {
                        SelectedSensor = null;
                        GraphPoints = new List<MeasurementPoint>();
                    }
                }
            }
        }

        public ObservableCollection<TemperatureSensor> SensorList
        {
            get { return sensorList; }
            set { SetProperty(ref sensorList, value); }
        }

        public TemperatureSensor SelectedSensor
        {
            get { return selectedSensor; }
            set
            {
                SetProperty(ref selectedSensor, value);
                OnRefreshGraph();
            }
        }

        public List<MeasurementPoint> GraphPoints
        {
            get { return graphPoints; }
            set { SetProperty(ref graphPoints, value); }
        }

        public void LoadSensors()
        {
            SensorList.Clear();
            if (NetworkEntitiesViewModel.AllSensors == null) return;

            foreach (var s in NetworkEntitiesViewModel.AllSensors)
                SensorList.Add(s);
        }

        public void OnRefreshGraph()
        {
            if (SelectedSensor == null) return;

            GraphPoints = LoadLastFiveFromLog(SelectedSensor.Name);
        }

        private List<MeasurementPoint> LoadLastFiveFromLog(string sensorName)
        {
            var points = new List<MeasurementPoint>();
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "measurements.txt");

            if (!File.Exists(logPath)) return points;

            var lines = File.ReadAllLines(logPath);
            var matching = new List<string>();

            foreach (var line in lines)
            {
                if (line.Contains("| " + sensorName + " |"))
                    matching.Add(line);
            }

            int start = matching.Count > 5 ? matching.Count - 5 : 0;

            for (int i = start; i < matching.Count; i++)
            {
                var parts = matching[i].Split('|');
                if (parts.Length < 3) continue;

                double val;
                if (!double.TryParse(parts[2].Trim(), out val)) continue;

                points.Add(new MeasurementPoint
                {
                    Timestamp = parts[0].Trim(),
                    Value = val,
                    IsValid = val >= 250 && val <= 350
                });
            }

            return points;
        }
    }

    public class MeasurementPoint
    {
        public string Timestamp { get; set; }
        public double Value { get; set; }
        public bool IsValid { get; set; }
    }
}
