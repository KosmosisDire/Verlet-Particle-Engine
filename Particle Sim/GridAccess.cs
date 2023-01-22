using System.Collections.Concurrent;
using ComputeSharp;
using SFML.System;

namespace ParticlePhysics.Internal;

public struct GridAccess : IDisposable
{
    public ReadWriteBuffer<int> gridValues;
    public ReadWriteBuffer<int> gridValueCounts;
    public ReadWriteBuffer<int> gridKeys;
    
    public Vector2f cellSize;
    public Vector2i cellCount;
    public int cellCountLinear;

    int[] itemIndiciesTemp;
    public int[] gridValuesArray;
    public int[] gridKeysArray;
    public int[] gridValueCountsArray;

    public Vector2i extents;
    int objectCount;

    public GridAccess(Vector2i cellCount, Vector2i extents, int objectCount)
    {
        this.cellCount = cellCount;
        this.extents = extents;
        this.objectCount = objectCount;
        this.cellSize = new Vector2f((float)extents.X / cellCount.X, (float)extents.Y / cellCount.Y);
        this.cellCountLinear = cellCount.X * cellCount.Y;

        gridValues = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(objectCount);
        gridValueCounts = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);
        gridKeys = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);

        itemIndiciesTemp = new int[objectCount];
        gridValuesArray = new int[objectCount];
        gridKeysArray = new int[cellCountLinear];
        gridValueCountsArray = new int[cellCountLinear];
    }
    
    public void SetCellCount(Vector2i cellCount)
    {
        lock(gridValues) lock(gridValueCounts) lock(gridKeys) lock (gridKeysArray)
        {
            this.cellCount = cellCount;
            this.cellSize = new Vector2f((float)extents.X / cellCount.X, (float)extents.Y / cellCount.Y);
            this.cellCountLinear = cellCount.X * cellCount.Y;

            gridValues = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(objectCount);
            gridValueCounts = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);
            gridKeys = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);

            itemIndiciesTemp = new int[objectCount];
            gridValuesArray = new int[objectCount];
            gridKeysArray = new int[cellCountLinear];
        }
    }


    public int GetIndex(Vector2f position)
    {
        int x = (int)MathF.Floor(Math.Clamp(position.X / cellSize.X, 0, cellCount.X - 1));
        int y = (int)MathF.Floor(Math.Clamp(position.Y / cellSize.Y, 0, cellCount.Y - 1));
        int index = x + y * cellCount.X;

        return index;
    }

    public List<int> LineIntersectionIndicies(Vector2f start, Vector2f end, out List<Vector2f> intersections, out List<Vector2f> samples)
    {
        // this function will find the intersections of a line with each edge of each column and row of the grid
        // it will then average each neighboring intersection along the line and sample the index of the cell at that point
        
        List<int> indicies = new List<int>();
        intersections = new List<Vector2f>();

        // calculate edges of grid

        for (int i = 0; i < cellCount.X + 1; i++)
        {
            var edgeStart = new Vector2f(i * cellSize.X, 0);
            var edgeEnd = new Vector2f(i * cellSize.X, extents.Y);

            if(Math2D.LineSegementsIntersect(start, end, edgeStart, edgeEnd, out Vector2f intersection))
            {
                intersections.Add(intersection);
            }
        }

        for (int i = 0; i < cellCount.Y + 1; i++)
        {
            var edgeStart = new Vector2f(0, i * cellSize.Y);
            var edgeEnd = new Vector2f(extents.X, i * cellSize.Y);

            if (Math2D.LineSegementsIntersect(start, end, edgeStart, edgeEnd, out Vector2f intersection))
            {
                intersections.Add(intersection);
            }
        }

        if(start.Length() < end.Length() && start.Length() > 0)
        {
            intersections.Insert(0, start);
        }

        if(end.Length() < start.Length() && end.Length() > 0)
        {
            intersections.Insert(0, end);
        }

        if(start.Length() > end.Length() && start.Length() < ((Vector2f)(extents)).Length())
        {
            intersections.Add(start);
        }

        if(end.Length() > start.Length() && end.Length() < ((Vector2f)(extents)).Length())
        {
            intersections.Add(end);
        }

        intersections.Sort((a, b) => (a - start).Length().CompareTo((b - start).Length()));

        // average intersections, and get index of cell at that point
        samples = new List<Vector2f>();
        for (int i = 0; i < intersections.Count - 1; i++)
        {
            var sample = (intersections[i] + intersections[i + 1]) / 2;
            indicies.Add(GetIndex(sample));
            samples.Add(sample);
        }

        return indicies;
    }
    
    public void BuildGrid(float2[] positionsArray, int[] activeArray)
    {
        lock(gridValues) lock(gridValueCounts) lock(gridKeys) lock (gridKeysArray) lock(gridValueCountsArray)
        {
            var gridValueCountsLocal = new int[cellCountLinear];

            GridAccess grid = this;

            var rangePartitioner = Partitioner.Create(0, positionsArray.Length);

            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                for(int i = range.Item1; i < range.Item2; i++)
                {
                    if(activeArray[i] == 0) continue;
                    var x = (int)Math.Clamp(positionsArray[i].X / grid.cellSize.X, 0, grid.cellCount.X-1);
                    var y = (int)Math.Clamp(positionsArray[i].Y / grid.cellSize.Y, 0, grid.cellCount.Y-1);
                    int index = x + y * grid.cellCount.X;

                    grid.itemIndiciesTemp[i] = index;

                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            for (int i = 0; i < gridKeysArray.Length-1; i++)
            {
                gridKeysArray[i+1] = gridKeysArray[i] + Math.Max(gridValueCountsLocal[i] - 1, 0);
                gridValueCountsLocal[i] = 0;
            }

            Parallel.ForEach(rangePartitioner, (range, loopState) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (activeArray[i] == 0) continue;
                    
                    int index = grid.itemIndiciesTemp[i];

                    int valueIndex = Math.Clamp(grid.gridKeysArray[index] + gridValueCountsLocal[index], 0, grid.gridValues.Length - 1);
                    grid.gridValuesArray[valueIndex] = i;
                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            gridValues.CopyFrom(gridValuesArray);
            gridValueCounts.CopyFrom(gridValueCountsLocal);
            gridKeys.CopyFrom(gridKeysArray);

            this.gridValueCountsArray = gridValueCountsLocal;
        }
    }

    public void Dispose()
    {
        gridValues.Dispose();
        gridValueCounts.Dispose();
        gridKeys.Dispose();
    }
}