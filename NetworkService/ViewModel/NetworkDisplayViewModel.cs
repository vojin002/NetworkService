using NetworkService.Helpers;
using NetworkService.Model;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NetworkService.ViewModel
{
    public class NetworkDisplayViewModel : BindableBase
    {
        private ObservableCollection<TemperatureSensor> sensorsInTreeView;

        public MyICommand AutoArrangeCommand { get; set; }

        public NetworkDisplayViewModel()
        {
            SensorsInTreeView = new ObservableCollection<TemperatureSensor>();
            AutoArrangeCommand = new MyICommand(OnAutoArrange);

            RefreshTreeView();

            if (NetworkEntitiesViewModel.AllSensors != null)
                NetworkEntitiesViewModel.AllSensors.CollectionChanged += AllSensors_CollectionChanged;
        }

        private void AllSensors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TemperatureSensor sensor in e.NewItems)
                    SensorsInTreeView.Add(sensor);
            }
            if (e.OldItems != null)
            {
                foreach (TemperatureSensor sensor in e.OldItems)
                    SensorsInTreeView.Remove(sensor);
            }
        }

        public ObservableCollection<TemperatureSensor> SensorsInTreeView
        {
            get { return sensorsInTreeView; }
            set { SetProperty(ref sensorsInTreeView, value); }
        }

        public void RefreshTreeView()
        {
            SensorsInTreeView.Clear();

            if (NetworkEntitiesViewModel.AllSensors == null) return;

            foreach (var sensor in NetworkEntitiesViewModel.AllSensors)
            {
                SensorsInTreeView.Add(sensor);
            }
        }

        private void OnAutoArrange()
        {
        }
    }
}
