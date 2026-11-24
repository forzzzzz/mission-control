using System;
using System.Windows;
using System.Windows.Controls;
using SmartTravelPlanner.Models;
using Microsoft.Win32;

namespace SmartTravelPlanner
{
    public partial class MainWindow : Window
    {
        
        private Traveler? _traveler;
        private CityGraph? _cityGraph;
        
        private bool _hasUnsavedChanges = false;
        private bool _isLoadingData = false;
        
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ShowInfo(string message, string title = "Success")
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ShowConfirm(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }
        
        private void UpdateUI()
        {
            if (_traveler == null)
            {
                RouteTextBox.Text = string.Empty;
                TotalDistanceTextBlock.Text = "0";
            }
            else
            {
                var route = _traveler.GetRoute();
                RouteTextBox.Text = string.Join(" → ", route);

                if (_cityGraph != null)
                {
                    var pathDistance = _cityGraph.GetPathDistance(route);
                    if (pathDistance == -1) { TotalDistanceTextBlock.Text = "N/A"; }
                    else { TotalDistanceTextBlock.Text = pathDistance.ToString(); }
                }
                else { TotalDistanceTextBlock.Text = "0"; }
            }        
        }
        
        private void OnExitBtnClick(object sender, RoutedEventArgs e) { Close(); }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                if (!ShowConfirm("You have unsaved changes. Are you sure you want to exit?")) { e.Cancel = true; }
            }

            base.OnClosing(e);
        }
        
        private void OnCreateTravelerBtnClick(object sender, RoutedEventArgs e)
        {
            string name = TravelerNameTextBox.Text.Trim();
            string location = TravelerLocationTextBox.Text.Trim();
            
            if (!string.IsNullOrWhiteSpace(name))
            {
                
                var oldRoute = _traveler?.GetRoute();
                
                _traveler = new Traveler(name);
                _traveler.SetLocation(location);
                
                if (oldRoute != null)
                {
                    foreach (var city in oldRoute)
                    {
                        _traveler.AddCity(city);
                    }
                }
                
                _hasUnsavedChanges = true;
                UpdateUI();
                
                ShowInfo("Traveler successfully created.");
            }
            else
            {
                ShowError("Please enter traveler name.");
            }
        }

        private void OnLoadMapBtnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _cityGraph = CityGraph.LoadFromFile(dialog.FileName);
                    var connections = _cityGraph.GetAllConnections();
                    RouteListBox.ItemsSource = connections;
                    
                    UpdateUI();
                }
                catch (Exception)
                {
                    ShowError("The map file was not found or is in an invalid format.");
                }
            }
        }
        
        private void OnAddCityBtnClick(object sender, RoutedEventArgs e)
        {
            if (_traveler == null)
            {
                ShowError("Please create or load a traveler first.");
                return;
            }

            var city = CityTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                ShowError("Please enter a city name.");
                return;
            }

            _traveler.AddCity(city);
            _hasUnsavedChanges = true;
            CityTextBox.Clear();
            UpdateUI();
        }
        
        private void OnRemoveCityBtnClick(object sender, RoutedEventArgs e)
        {
            if (_traveler == null)
            {
                ShowError("Please create or load a traveler first.");
                return;
            }

            var city = CityTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(city))
            {
                ShowError("Please enter a city name.");
                return;
            }

            if (_traveler.RemoveCity(city))
            {
                _hasUnsavedChanges = true;
                CityTextBox.Clear();
                UpdateUI();
            }
            else
            {
                ShowError($"City '{city}' is not in the current route.");
            }
        }

        private void OnClearRouteBtnClick(object sender, RoutedEventArgs e)
        {
            if (_traveler == null) return;
            
            if (_traveler.GetStopCount() == 0) return;
            
            _traveler.ClearRoute();
            _hasUnsavedChanges = true;
            UpdateUI();
        }

        private void OnPlanRouteBtnClick(object sender, RoutedEventArgs e)
        {
            if (_traveler == null)
            {
                ShowError("Please create or load a traveler first.");
                return;
            }
            
            if (_cityGraph == null)
            {
                ShowError("Please load a city map first.");
                return;
            }
            
            var destination = CityTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(destination))
            {
                ShowError("Please enter a city name.");
                return;
            }
            
            var success = _traveler.PlanRouteTo(destination, _cityGraph);
            
            if (!success)
            {
                ShowError("There is no route on the map, or an invalid starting location has been specified.");
                return;
            }
            
            _hasUnsavedChanges = true;
            CityTextBox.Clear();
            UpdateUI();
        }

        private void OnSaveTravelerBtnClick(object sender, RoutedEventArgs e)
        {
            if (_traveler == null)
            {
                ShowError("Please create or load a traveler first.");
                return;
            }
            
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{_traveler.GetName()}.json"
            };

            try
            {
                if (dialog.ShowDialog() == true)
                {
                    _traveler.SaveToFile(dialog.FileName);
                    _hasUnsavedChanges = false;
                    
                    ShowInfo("Traveler successfully saved.");
                }
            }
            catch (Exception exception)
            {
                ShowError("Error: " + exception.Message);
            }
        }

        private void OnLoadFromJsonBtnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _traveler = Traveler.LoadFromFile(dialog.FileName);

                    _isLoadingData = true;
                    TravelerNameTextBox.Text = _traveler.GetName();
                    TravelerLocationTextBox.Text = _traveler.GetLocation();
                    _isLoadingData = false;

                    _hasUnsavedChanges = false;
                    UpdateUI();
                    
                    ShowInfo("Traveler successfully loaded.");
                }
                catch (Exception)
                {
                    ShowError("Invalid travel data or file not found.");
                }
            }
        }

        private void OnTravelerInfoChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingData) return;

            string name = TravelerNameTextBox.Text.Trim();
            string location = TravelerLocationTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name)) return;

            if (_traveler == null)
            {
                _traveler = new Traveler(name);
            }
            else
            {
                _traveler.SetName(name);
            }

            _traveler.SetLocation(location);
            _hasUnsavedChanges = true;
            UpdateUI();
        }
    }
}
