using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SmartTravelPlanner.Models
{
    public class Traveler : ICloneable
    {
        readonly string _name;
        string _currentLocation = string.Empty;
        List<string> _route = new();

        public Traveler(string name)
        {
            _name = name;
        }

        private class TravelerData
        {
            public string? Name { get; set; }
            public string? CurrentLocation { get; set; }
            public List<string>? Route { get; set; }
        }

        public override string ToString() => GetTravelSummary();

        public string GetName() => _name;

        public string GetLocation() => _currentLocation;

        public void SetLocation(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                _currentLocation = location;
                return;
            }

            _currentLocation = Capitalize(location);
        }

        public void AddCity(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                throw new ArgumentException("Invalid city");
            }

            _route.Add(Capitalize(city));
        }
        
        public List<string> GetRoute() => new (_route);

        public void ClearRoute() => _route.Clear();

        public int GetStopCount() => _route.Count;

        public bool HasCity(string city) => _route.Contains(Capitalize(city));

        public void SortRoute() => _route.Sort();
        
        public bool RemoveCity(string city) => _route.Remove(Capitalize(city));

        public string? GetNextStop() => _route.FirstOrDefault();

        public string this[int index] => _route[index];

        public override bool Equals(object? @object)
        {
            if (@object is not Traveler other)
                return false;

            return _name == other._name && _currentLocation == other._currentLocation;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_name, _currentLocation);
        }

        public static bool operator ==(Traveler? a, Traveler? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(Traveler? a, Traveler? b)
        {
            return !(a == b);
        }

        public object Clone()
        {
            var clone = new Traveler(_name);
            clone._currentLocation = _currentLocation;
            clone._route = new List<string>(_route);
            return clone;
        }

        public virtual string GetTravelSummary()
        {
            return $"Traveler: {_name} | Location: {_currentLocation} | Route: {string.Join(" -> ", _route)}";
        }

        public void SaveToFile(string filePath)
        {
            var routeItems = string.Join(", ", _route.Select(city => JsonSerializer.Serialize(city)));

            string json = string.Join("\n",
                "{",
                $"  \"name\": {JsonSerializer.Serialize(_name)},",
                $"  \"currentLocation\": {JsonSerializer.Serialize(_currentLocation)},",
                $"  \"route\": [{routeItems}]",
                "}");

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        public static Traveler LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            try
            {
                string json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<TravelerData>(json, options);

                var traveler = new Traveler(data?.Name ?? string.Empty);
                traveler._currentLocation = data?.CurrentLocation ?? string.Empty;
                if (data?.Route != null)
                {
                    traveler._route = new List<string>(data.Route);
                }

                return traveler;
            }
            catch (JsonException)
            {
                throw new FileLoadException("Invalid travel data");
            }
        }

        public bool PlanRouteTo(string destination, CityGraph map)
        {
            string? start = null;

            if (!string.IsNullOrEmpty(_currentLocation))
            {
                start = _currentLocation;
            }
            else if (_route.Count > 0)
            {
                start = _route[0];
            }

            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(destination) || map == null)
            {
                return false;
            }

            string target = destination;
            List<string> path = map.FindShortestPath(start, target);

            if (path.Count == 0)
            {
                target = Capitalize(destination);
                path = map.FindShortestPath(start, target);
            }

            if (path.Count == 0)
            {
                return false;
            }

            _route = new List<string>(path);
            return true;
        }

        protected string Capitalize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input[1..];
        }
    }
}