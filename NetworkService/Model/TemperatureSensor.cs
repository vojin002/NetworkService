using NetworkService.Helpers;

namespace NetworkService.Model
{
    public class TemperatureSensor : BindableBase
    {
        private int id;
        private string name;
        private SensorType type;
        private double? lastMeasuredValue;

        public int Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public SensorType Type
        {
            get { return type; }
            set { SetProperty(ref type, value); }
        }

        public double? LastMeasuredValue
        {
            get { return lastMeasuredValue; }
            set { SetProperty(ref lastMeasuredValue, value); }
        }

        public bool IsValueValid
        {
            get
            {
                if (lastMeasuredValue == null) return true;
                return lastMeasuredValue >= 250 && lastMeasuredValue <= 350;
            }
        }
    }
}
