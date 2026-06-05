using NetworkService.Helpers;
using NetworkService.Model;
using System.Collections.ObjectModel;

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
