using NetworkService.Helpers;
using NetworkService.Model;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : BindableBase
    {
        private ObservableCollection<TemperatureSensor> sensorsInTreeView;
        private ObservableCollection<SensorTypeGroup> sensorGroups;

        public MyICommand AutoArrangeCommand { get; set; }

        public NetworkDisplayViewModel()
        {
            SensorsInTreeView = new ObservableCollection<TemperatureSensor>();
            SensorGroups = new ObservableCollection<SensorTypeGroup>();
            AutoArrangeCommand = new MyICommand(OnAutoArrange);

            RefreshTreeView();

            if (NetworkEntitiesViewModel.AllSensors != null)
                NetworkEntitiesViewModel.AllSensors.CollectionChanged += AllSensors_CollectionChanged;
        }

        private void AllSensors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                int insertIndex = e.NewStartingIndex;
                foreach (TemperatureSensor sensor in e.NewItems)
                {
                    int at = Math.Min(insertIndex, SensorsInTreeView.Count);
                    SensorsInTreeView.Insert(at, sensor);
                    AddToGroupAtIndex(sensor, insertIndex);
                    insertIndex++;
                }
            }
            if (e.OldItems != null)
            {
                foreach (TemperatureSensor sensor in e.OldItems)
                {
                    SensorsInTreeView.Remove(sensor);
                    RemoveFromGroup(sensor);
                }
            }
        }

        public ObservableCollection<TemperatureSensor> SensorsInTreeView
        {
            get { return sensorsInTreeView; }
            set { SetProperty(ref sensorsInTreeView, value); }
        }

        public ObservableCollection<SensorTypeGroup> SensorGroups
        {
            get { return sensorGroups; }
            set { SetProperty(ref sensorGroups, value); }
        }

        private void AddToGroupAtIndex(TemperatureSensor sensor, int allSensorsIndex)
        {
            string typeName = sensor.Type.Name;
            SensorTypeGroup group = null;

            foreach (var g in SensorGroups)
            {
                if (g.TypeName == typeName)
                {
                    group = g;
                    break;
                }
            }

            if (group == null)
            {
                group = new SensorTypeGroup(typeName);
                SensorGroups.Add(group);
            }

            int groupInsertIndex = 0;
            for (int i = 0; i < allSensorsIndex && i < NetworkEntitiesViewModel.AllSensors.Count; i++)
            {
                if (NetworkEntitiesViewModel.AllSensors[i].Type.Name == typeName)
                    groupInsertIndex++;
            }

            groupInsertIndex = Math.Min(groupInsertIndex, group.Sensors.Count);
            group.Sensors.Insert(groupInsertIndex, sensor);
        }

        public void AddToGroup(TemperatureSensor sensor)
        {
            string typeName = sensor.Type.Name;
            SensorTypeGroup group = null;

            foreach (var g in SensorGroups)
            {
                if (g.TypeName == typeName)
                {
                    group = g;
                    break;
                }
            }

            if (group == null)
            {
                group = new SensorTypeGroup(typeName);
                SensorGroups.Add(group);
            }

            group.Sensors.Add(sensor);
        }

        public void RemoveFromGroup(TemperatureSensor sensor)
        {
            foreach (var group in SensorGroups)
            {
                if (group.Sensors.Contains(sensor))
                {
                    group.Sensors.Remove(sensor);
                    if (group.Sensors.Count == 0)
                        SensorGroups.Remove(group);
                    break;
                }
            }
        }

        public void RefreshTreeView()
        {
            SensorsInTreeView.Clear();
            SensorGroups.Clear();

            if (NetworkEntitiesViewModel.AllSensors == null) return;

            foreach (var sensor in NetworkEntitiesViewModel.AllSensors)
            {
                SensorsInTreeView.Add(sensor);
                AddToGroup(sensor);
            }
        }

        private void OnAutoArrange()
        {
        }
    }
}
