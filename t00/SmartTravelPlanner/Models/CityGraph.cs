using System;
using System.Collections.Generic;
using System.IO;

namespace SmartTravelPlanner.Models
{
    public class CityGraph
    {
        private Dictionary<string, List<(string City, int Distance)>> adjacencyList = new();
        private readonly List<string> _edgesOrder = new();
        private readonly List<string> _connections = new();
        
        
        public List<string> GetAllConnections() => new(_connections);

        public static CityGraph LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var graph = new CityGraph();
            string[] lines = File.ReadAllLines(filePath);

            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    continue;
                }

                string line = rawLine.Trim();

                string[] parts = line.Split(',');
                if (parts.Length != 2)
                {
                    throw new FormatException("Invalid map file format.");
                }

                string[] cities = parts[0].Split('-');
                if (cities.Length != 2)
                {
                    throw new FormatException("Invalid map file format.");
                }

                string cityA = cities[0].Trim();
                string cityB = cities[1].Trim();

                if (!int.TryParse(parts[1].Trim(), out int distance))
                {
                    throw new FormatException("Invalid map file format.");
                }

                graph.AddEdge(cityA, cityB, distance);
                graph.AddEdge(cityB, cityA, distance);
                
                graph._connections.Add($"{cityA} - {cityB}: {distance} km");
                graph._connections.Add($"{cityB} - {cityA}: {distance} km");

                graph._edgesOrder.Add($"{cityA}-{cityB},{distance}");
                graph._edgesOrder.Add($"{cityB}-{cityA},{distance}");
            }

            if (graph.adjacencyList.Count == 0)
            {
                throw new FormatException("Invalid map file format.");
            }
            
            return graph;
        }

        public List<string> FindShortestPath(string from, string to)
        {
            var path = new List<string>();

            if (!adjacencyList.ContainsKey(from) || !adjacencyList.ContainsKey(to))
            {
                return path;
            }

            if (from == to)
            {
                return new List<string> { from };
            }

            var distances = new Dictionary<string, int>();
            var previous = new Dictionary<string, string?>();
            var priorityQueue = new PriorityQueue<string, int>();

            foreach (var node in adjacencyList.Keys)
            {
                distances[node] = int.MaxValue;
                previous[node] = null;
            }

            distances[from] = 0;
            priorityQueue.Enqueue(from, 0);

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();

                if (current == to)
                {
                    break;
                }

                if (!adjacencyList.TryGetValue(current, out var neighbors))
                {
                    continue;
                }

                foreach (var (neighbor, distance) in neighbors)
                {
                    int currentDist = distances[current];
                    if (currentDist == int.MaxValue)
                    {
                        continue;
                    }

                    int newDistance = currentDist + distance;

                    if (newDistance < distances.GetValueOrDefault(neighbor, int.MaxValue))
                    {
                        distances[neighbor] = newDistance;
                        previous[neighbor] = current;
                        priorityQueue.Enqueue(neighbor, newDistance);
                    }
                }
            }

            if (distances[to] == int.MaxValue)
            {
                return path;
            }

            string? step = to;
            while (step != null)
            {
                path.Add(step);
                step = previous.GetValueOrDefault(step);
            }

            path.Reverse();
            return path;
        }

        public int GetPathDistance(List<string> path)
        {
            int totalDistance = 0;

            if (path.Count < 2)
            {
                return totalDistance;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                string fromCity = path[i];
                string toCity = path[i + 1];

                if (!adjacencyList.TryGetValue(fromCity, out var neighbors))
                {
                    return -1;
                }

                bool edgeFound = false;
                foreach (var (city, distance) in neighbors)
                {
                    if (city == toCity)
                    {
                        totalDistance += distance;
                        edgeFound = true;
                        break;
                    }
                }

                if (!edgeFound)
                {
                    return -1;
                }
            }

            return totalDistance;
        }

        private void AddEdge(string fromCity, string toCity, int distance)
        {
            if (!adjacencyList.TryGetValue(fromCity, out var edges))
            {
                edges = new List<(string City, int Distance)>();
                adjacencyList[fromCity] = edges;
            }

            edges.Add((toCity, distance));
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _edgesOrder);
        }
    }
}