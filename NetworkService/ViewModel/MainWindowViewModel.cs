using NetworkService.Helpers;
using NetworkService.Model;
using Notification.Wpf;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BindableBase
    {
        public MyICommand<string> NavCommand { get; private set; }

        public NetworkEntitiesViewModel networkEntitiesViewModel;
        public NetworkDisplayViewModel NetworkDisplayVM { get; set; }
        public MeasurementGraphViewModel measurementGraphViewModel;

        private BindableBase currentViewModel;
        private NotificationManager notificationManager;

        public MainWindowViewModel()
        {
            notificationManager = new NotificationManager();

            NavCommand = new MyICommand<string>(OnNav);

            networkEntitiesViewModel = new NetworkEntitiesViewModel();
            networkEntitiesViewModel.ShowNotification = ShowToast;
            NetworkDisplayVM = new NetworkDisplayViewModel();
            measurementGraphViewModel = new MeasurementGraphViewModel();

            CurrentViewModel = networkEntitiesViewModel;

            CreateListener();
        }

        public BindableBase CurrentViewModel
        {
            get { return currentViewModel; }
            set
            {
                SetProperty(ref currentViewModel, value);
                OnPropertyChanged("IsEntitiesViewActive");
                OnPropertyChanged("IsGraphViewActive");
            }
        }

        public bool IsEntitiesViewActive
        {
            get { return currentViewModel is NetworkEntitiesViewModel; }
        }

        public bool IsGraphViewActive
        {
            get { return currentViewModel is MeasurementGraphViewModel; }
        }

        private void OnNav(string destination)
        {
            switch (destination)
            {
                case "entities":
                    CurrentViewModel = networkEntitiesViewModel;
                    break;
                case "graph":
                    measurementGraphViewModel.LoadSensors();
                    CurrentViewModel = measurementGraphViewModel;
                    break;
            }
        }

        public void ShowToast(string title, string message, NotificationType type)
        {
            notificationManager.Show(new NotificationContent
            {
                Title = title,
                Message = message,
                Type = type
            }, "WindowNotificationArea");
        }

        private void CreateListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        byte[] bytes = new byte[1024];
                        int i = stream.Read(bytes, 0, bytes.Length);
                        string incomming = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (incomming.Equals("Need object count"))
                        {
                            int count = NetworkEntitiesViewModel.AllSensors != null
                                ? NetworkEntitiesViewModel.AllSensors.Count : 0;
                            byte[] data = System.Text.Encoding.ASCII.GetBytes(count.ToString());
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            string[] parts = incomming.Split(':');
                            if (parts.Length == 2)
                            {
                                string sensorName = parts[0].Trim();
                                double value;
                                if (double.TryParse(parts[1].Trim(), out value))
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        UpdateSensorValue(sensorName, value);
                                    });
                                }
                            }
                        }
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }

        private void UpdateSensorValue(string sensorName, double value)
        {
            if (NetworkEntitiesViewModel.AllSensors == null) return;

            foreach (var sensor in NetworkEntitiesViewModel.AllSensors)
            {
                if (sensor.Name == sensorName)
                {
                    sensor.LastMeasuredValue = value;
                    sensor.OnPropertyChanged("IsValueValid");
                    WriteToLog(sensor.Name, value);
                    break;
                }
            }
        }

        private void WriteToLog(string sensorName, double value)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "measurements.txt");
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + sensorName + " | " + value;
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
    }
}
