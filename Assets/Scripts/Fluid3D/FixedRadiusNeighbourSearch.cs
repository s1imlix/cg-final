using System;
using System.Threading.Tasks;
using UnityEngine;

public class FixedRadiusNeighbourSearch
{
    private Entry[] spatialLookup;
    private int[] startIndices;
    private Vector2[] points;
    private float radius;

    public Entry[] SpatialLookup => spatialLookup;
    public int[] StartIndices => startIndices;

    public void UpdateSpatialLookup(Vector2[] points, float radius)
    {
        this.points = points;
        this.radius = radius;

        spatialLookup = new Entry[points.Length];
        startIndices = new int[points.Length];

        Parallel.For(0, points.Length, i =>
        {
            (int cellX, int cellY) = PositionToCellCoord(points[i], radius);
            uint cellKey = GetKeyFromHash(HashCell(cellX, cellY));
            spatialLookup[i] = new Entry(i, cellKey);
            startIndices[i] = int.MaxValue;
        });

        Array.Sort(spatialLookup);

        Parallel.For(0, points.Length, i =>
        {
            uint key = spatialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spatialLookup[i - 1].cellKey;
            if (key != keyPrev)
            {
                startIndices[key] = i;
            }
        });
    }

    private (int, int) PositionToCellCoord(Vector2 point, float radius)
    {
        int cellX = (int)(point.x / radius);
        int cellY = (int)(point.y / radius);
        return (cellX, cellY);
    }

    private uint HashCell(int cellX, int cellY)
    {
        uint a = (uint)cellX * 15823;
        uint b = (uint)cellY * 9737333;
        return a + b;
    }

    private uint GetKeyFromHash(uint hash)
    {
        return hash % (uint)spatialLookup.Length;
    }

    [Serializable]
    public struct Entry : IComparable<Entry>
    {
        public int particleIndex;
        public uint cellKey;

        public Entry(int index, uint key)
        {
            particleIndex = index;
            cellKey = key;
        }

        public int CompareTo(Entry other)
        {
            return cellKey.CompareTo(other.cellKey);
        }
    }
} 