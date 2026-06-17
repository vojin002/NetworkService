using System.Collections.ObjectModel;

namespace NetworkService.Model
{
    public class SensorTypeGroup
    {
        public string TypeName { get; set; }
        public ObservableCollection<TemperatureSensor> Sensors { get; set; }

        public SensorTypeGroup(string typeName)
        {
            TypeName = typeName;
            Sensors = new ObservableCollection<TemperatureSensor>();
        }
    }
}
