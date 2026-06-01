namespace NetworkService.Model
{
    public class SensorType
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
