using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlateRecognation
{
    public class DBSCAN
    {
        private double _epsilon;
        private int _minPoints;

        public DBSCAN(double epsilon, int minPoints)
        {
            _epsilon = epsilon;
            _minPoints = minPoints;
        }

        public int[] Fit(double[][] data)
        {
            int clusterId = 0;
            int[] labels = Enumerable.Repeat(-1, data.Length).ToArray(); // -1: Gürültü
            bool[] visited = new bool[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                if (visited[i])
                    continue;

                visited[i] = true;
                List<int> neighbors = GetNeighbors(data, i);

                if (neighbors.Count < _minPoints)
                {
                    labels[i] = -1; // Gürültü noktası
                }
                else
                {
                    clusterId++;
                    ExpandCluster(data, labels, visited, i, neighbors, clusterId);
                }
            }

            return labels;
        }

        private void ExpandCluster(double[][] data, int[] labels, bool[] visited, int pointIndex, List<int> neighbors, int clusterId)
        {
            labels[pointIndex] = clusterId;
            Queue<int> queue = new Queue<int>(neighbors);

            while (queue.Count > 0)
            {
                int neighborIndex = queue.Dequeue();

                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    List<int> newNeighbors = GetNeighbors(data, neighborIndex);

                    if (newNeighbors.Count >= _minPoints)
                    {
                        foreach (var newNeighbor in newNeighbors)
                        {
                            if (!queue.Contains(newNeighbor))
                            {
                                queue.Enqueue(newNeighbor);
                            }
                        }
                    }
                }

                if (labels[neighborIndex] == -1) // Gürültü olabilir, kümeye ekle
                {
                    labels[neighborIndex] = clusterId;
                }
            }
        }

        private List<int> GetNeighbors(double[][] data, int pointIndex)
        {
            List<int> neighbors = new List<int>();

            for (int i = 0; i < data.Length; i++)
            {
                if (i != pointIndex && Distance(data[pointIndex], data[i]) <= _epsilon)
                {
                    neighbors.Add(i);
                }
            }

            return neighbors;
        }

        private double Distance(double[] a, double[] b)
        {
            if (a.Length < 1 || b.Length < 1)
                throw new ArgumentException("Distance function expects at least one-dimensional points.");

            return Math.Abs(a[0] - b[0]); // Sadece X ekseninde mesafe hesabı yap
        }
    }
}
