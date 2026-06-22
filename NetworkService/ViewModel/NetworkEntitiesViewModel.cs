using NetworkService.Helpers;
using NetworkService.Model;
using Notification.Wpf;
using System;
using System.Collections.ObjectModel;

namespace NetworkService.ViewModel
{
    public class NetworkEntitiesViewModel : BindableBase
    {
        public static ObservableCollection<TemperatureSensor> AllSensors { get; set; }

        public Action<string, string, NotificationType> ShowNotification { get; set; }
        public Action<string, string, Action> ShowDeleteWithUndo { get; set; }

        private ObservableCollection<TemperatureSensor> filteredSensors;
        private TemperatureSensor selectedSensor;

        private bool searchByName;
        private bool searchByType;
        private string searchText;

        private string selectedSavedSearch;
        private ObservableCollection<string> savedSearches;

        private string typeError;

        private bool showDeleteConfirmation;

        public MyICommand AddSensorCommand { get; set; }
        public MyICommand DeleteSensorCommand { get; set; }
        public MyICommand ConfirmDeleteCommand { get; set; }
        public MyICommand CancelDeleteCommand { get; set; }
        public MyICommand SearchCommand { get; set; }
        public MyICommand ClearSearchCommand { get; set; }
        public MyICommand SaveSearchCommand { get; set; }
        public MyICommand<string> LoadSavedSearchCommand { get; set; }

        private SensorType selectedTypeToAdd;
        private ObservableCollection<SensorType> availableTypes;

        public NetworkEntitiesViewModel()
        {
            if (AllSensors == null)
                AllSensors = new ObservableCollection<TemperatureSensor>();

            FilteredSensors = new ObservableCollection<TemperatureSensor>(AllSensors);
            SavedSearches = new ObservableCollection<string>();
            SearchByName = true;

            availableTypes = new ObservableCollection<SensorType>();
            availableTypes.Add(new SensorType { Name = "RTD", ImagePath = "pack://application:,,,/NetworkService;component/Resources/rtd.png" });
            availableTypes.Add(new SensorType { Name = "TermoSprega", ImagePath = "pack://application:,,,/NetworkService;component/Resources/termosprega.png" });

            SelectedTypeToAdd = availableTypes[0];

            AddSensorCommand = new MyICommand(OnAddSensor);
            DeleteSensorCommand = new MyICommand(OnDeleteSensor, CanDeleteSensor);
            ConfirmDeleteCommand = new MyICommand(OnConfirmDelete);
            CancelDeleteCommand = new MyICommand(OnCancelDelete);
            SearchCommand = new MyICommand(OnSearch);
            ClearSearchCommand = new MyICommand(OnClearSearch);
            SaveSearchCommand = new MyICommand(OnSaveSearch);
            LoadSavedSearchCommand = new MyICommand<string>(OnLoadSavedSearch);

            if (AllSensors.Count == 0)
                LoadInitialSensors();
        }

        private void LoadInitialSensors()
        {
            var rtd = new SensorType { Name = "RTD", ImagePath = "pack://application:,,,/NetworkService;component/Resources/rtd.png" };
            var termo = new SensorType { Name = "TermoSprega", ImagePath = "pack://application:,,,/NetworkService;component/Resources/termosprega.png" };

            AllSensors.Add(new TemperatureSensor { Id = 1, Name = "Reactor_1", Type = rtd });
            AllSensors.Add(new TemperatureSensor { Id = 2, Name = "Reactor_2", Type = termo });
            AllSensors.Add(new TemperatureSensor { Id = 3, Name = "Reactor_3", Type = rtd });

            FilteredSensors = new ObservableCollection<TemperatureSensor>(AllSensors);
        }

        public ObservableCollection<TemperatureSensor> FilteredSensors
        {
            get { return filteredSensors; }
            set { SetProperty(ref filteredSensors, value); }
        }

        public TemperatureSensor SelectedSensor
        {
            get { return selectedSensor; }
            set
            {
                SetProperty(ref selectedSensor, value);
                DeleteSensorCommand.RaiseCanExecuteChanged();
            }
        }

        public bool SearchByName
        {
            get { return searchByName; }
            set { SetProperty(ref searchByName, value); }
        }

        public bool SearchByType
        {
            get { return searchByType; }
            set { SetProperty(ref searchByType, value); }
        }

        public string SearchText
        {
            get { return searchText; }
            set { SetProperty(ref searchText, value); }
        }

        public string SelectedSavedSearch
        {
            get { return selectedSavedSearch; }
            set
            {
                SetProperty(ref selectedSavedSearch, value);
                if (value != null)
                    OnLoadSavedSearch(value);
            }
        }

        public ObservableCollection<string> SavedSearches
        {
            get { return savedSearches; }
            set { SetProperty(ref savedSearches, value); }
        }

        public SensorType SelectedTypeToAdd
        {
            get { return selectedTypeToAdd; }
            set { SetProperty(ref selectedTypeToAdd, value); }
        }

        public ObservableCollection<SensorType> AvailableTypes
        {
            get { return availableTypes; }
        }

        public string TypeError
        {
            get { return typeError; }
            set { SetProperty(ref typeError, value); }
        }

        public bool ShowDeleteConfirmation
        {
            get { return showDeleteConfirmation; }
            set
            {
                SetProperty(ref showDeleteConfirmation, value);
                DeleteSensorCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnAddSensor()
        {
            TypeError = "";

            if (SelectedTypeToAdd == null)
            {
                TypeError = "Please select a type.";
                return;
            }

            int newId = 1;
            if (AllSensors.Count > 0)
            {
                foreach (var s in AllSensors)
                {
                    if (s.Id >= newId)
                        newId = s.Id + 1;
                }
            }

            int nameNumber = 1;
            while (true)
            {
                string candidate = "Reactor_" + nameNumber;
                bool taken = false;
                foreach (var s in AllSensors)
                {
                    if (s.Name == candidate)
                    {
                        taken = true;
                        break;
                    }
                }
                if (!taken) break;
                nameNumber++;
            }

            var newSensor = new TemperatureSensor
            {
                Id = newId,
                Name = "Reactor_" + nameNumber,
                Type = new SensorType { Name = SelectedTypeToAdd.Name, ImagePath = SelectedTypeToAdd.ImagePath }
            };

            AllSensors.Add(newSensor);
            OnClearSearch();
            RestartSimulator();

            if (ShowNotification != null)
                ShowNotification("Sensor Added", newSensor.Name + " (" + newSensor.Type.Name + ")", NotificationType.Success);
        }

        private void OnDeleteSensor()
        {
            if (SelectedSensor == null) return;
            ShowDeleteConfirmation = true;
        }

        private void OnConfirmDelete()
        {
            if (SelectedSensor == null) return;

            var deletedSensor = SelectedSensor;
            int deletedIndex = AllSensors.IndexOf(deletedSensor);
            int filteredIndex = FilteredSensors.IndexOf(deletedSensor);
            AllSensors.Remove(deletedSensor);
            FilteredSensors.Remove(deletedSensor);
            ShowDeleteConfirmation = false;
            RestartSimulator();

            if (ShowDeleteWithUndo != null)
            {
                string msg = deletedSensor.Name + " (" + deletedSensor.Type.Name + ")";
                ShowDeleteWithUndo("Sensor Deleted", msg, () =>
                {
                    AllSensors.Insert(Math.Min(deletedIndex, AllSensors.Count), deletedSensor);
                    FilteredSensors.Insert(Math.Min(filteredIndex, FilteredSensors.Count), deletedSensor);
                    RestartSimulator();
                    if (ShowNotification != null)
                        ShowNotification("Restored", deletedSensor.Name + " restored", NotificationType.Success);
                });
            }
        }

        private void OnCancelDelete()
        {
            ShowDeleteConfirmation = false;
        }

        private bool CanDeleteSensor()
        {
            return SelectedSensor != null && !ShowDeleteConfirmation;
        }

        private void OnSearch()
        {
            FilteredSensors = new ObservableCollection<TemperatureSensor>();

            foreach (var sensor in AllSensors)
            {
                if (SearchByName)
                {
                    if (string.IsNullOrEmpty(SearchText) || sensor.Name.ToLower().Contains(SearchText.ToLower()))
                        FilteredSensors.Add(sensor);
                }
                else if (SearchByType)
                {
                    if (string.IsNullOrEmpty(SearchText) || sensor.Type.Name.ToLower().Contains(SearchText.ToLower()))
                        FilteredSensors.Add(sensor);
                }
            }
        }

        private void OnClearSearch()
        {
            SearchText = "";
            FilteredSensors = new ObservableCollection<TemperatureSensor>(AllSensors);
        }

        private void OnSaveSearch()
        {
            if (string.IsNullOrEmpty(SearchText)) return;

            string searchOption = SearchByName ? "Name" : "Type";
            string searchEntry = searchOption + ": " + SearchText;

            if (!SavedSearches.Contains(searchEntry))
                SavedSearches.Add(searchEntry);
        }

        private void OnLoadSavedSearch(string savedSearch)
        {
            if (string.IsNullOrEmpty(savedSearch)) return;

            string[] parts = savedSearch.Split(':');
            if (parts.Length != 2) return;

            if (parts[0] == "Name")
            {
                SearchByName = true;
                SearchByType = false;
            }
            else
            {
                SearchByType = true;
                SearchByName = false;
            }

            SearchText = parts[1].Trim();
            OnSearch();
        }

        private void RestartSimulator()
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("MeteringSimulator");
            foreach (var p in processes)
                p.Kill();

            string simulatorPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\MeteringSimulator\\MeteringSimulator\\bin\\Debug\\MeteringSimulator.exe");

            if (System.IO.File.Exists(simulatorPath))
                System.Diagnostics.Process.Start(simulatorPath);
        }
    }
}
