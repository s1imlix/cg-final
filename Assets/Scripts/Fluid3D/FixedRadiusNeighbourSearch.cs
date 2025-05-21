using System;
using System.Threading.Tasks;
using UnityEngine;

public class FixedRadiusNeighbourSearch
{
    private Entry[] spatialLookup;
    private int[] startIndices;
    private Vector3[] points;
    private float radius;


    public void UpdateSpatialLookup(Vector3[] points, float radius)
    {
        this.points = points;
        this.radius = radius;

        spatialLookup = new Entry[points.Length]; // Stored h(cell(point)) 
        startIndices = new int[points.Length]; // startIndices[key] stores the first index of key after sort

        Parallel.For(0, points.Length, i =>
        {
            (int x, int y, int z) = PositionToCellCoord(points[i], radius);
            uint cellKey = GetKeyFromHash(HashCell(x, y, z));
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

    private (int, int, int) PositionToCellCoord(Vector3 point, float radius)
    {
        int cellX = (int)(point.x / radius);
        int cellY = (int)(point.y / radius);
        int cellZ = (int)(point.z / radius);
        return (cellX, cellY, cellZ);
    }

    private uint HashCell(int cellX, int cellY, int cellZ)
    {
        uint a = (uint)(cellX) * 73856093;
        uint b = (uint)(cellY) * 19349663;
        uint c = (uint)(cellZ) * 83492791;
        return a ^ b ^ c;
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