using System.Collections.Concurrent;
using ComputeSharp;
using SFML.System;
using Engine.Rendering;
using SFML.Graphics;

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
    readonly int objectCount;

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
        if (cellCount == this.cellCount) return;

        lock(gridValues) lock(gridValueCounts) lock(gridKeys) lock (gridKeysArray) lock (itemIndiciesTemp) lock (gridValuesArray)
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

    public bool PositionInsideGrid(Vector2f position)
    {
        return position.X >= 0 && position.X < extents.X && position.Y >= 0 && position.Y < extents.Y;
    }

    public List<int> LineIntersectionIndicies(Vector2f start, Vector2f end, out List<Vector2f> intersections)
    {
        lock(gridValueCountsArray)
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

                if(Math2D.LineSegmentsIntersect(start, end, edgeStart, edgeEnd, out Vector2f intersection))
                {
                    intersections.Add(intersection);
                }
            }

            for (int i = 0; i < cellCount.Y + 1; i++)
            {
                var edgeStart = new Vector2f(0, i * cellSize.Y);
                var edgeEnd = new Vector2f(extents.X, i * cellSize.Y);

                if (Math2D.LineSegmentsIntersect(start, end, edgeStart, edgeEnd, out Vector2f intersection))
                {
                    intersections.Add(intersection);
                }
            }

            if (PositionInsideGrid(start)) intersections.Add(start);
            if (PositionInsideGrid(end)) intersections.Add(end);

            intersections.Sort((a, b) => (a - start).Magnitude().CompareTo((b - start).Magnitude()));

            // average intersections, and get index of cell at that point
            
            for (int i = 0; i < intersections.Count - 1; i++)
            {
                var sample = (intersections[i] + intersections[i + 1]) / 2;
                var index = GetIndex(sample);

                if(index > gridValueCountsArray.Length) continue;

                if(gridValueCountsArray[index] > 0)
                {
                    indicies.Add(index);
                }
            }
            
            return indicies;

        }
    }

    
    public void DrawGrid(Canvas canvas, Color color)
    {
        Vector2f[] starts = new Vector2f[cellCountLinear];
        Vector2f[] ends = new Vector2f[cellCountLinear];

        for(var i = 0; i < (int)MathF.Max(cellCount.X, cellCount.Y); i++)
        {
            if (i < cellCount.X)
            {
                var edgeStart = new Vector2f(i * cellSize.X, 0);
                var edgeEnd = new Vector2f(i * cellSize.X, extents.Y);
                starts[i] = edgeStart;
                ends[i] = edgeEnd;
            }
            if (i < cellCount.Y)
            {
                var edgeStart = new Vector2f(0, i * cellSize.Y);
                var edgeEnd = new Vector2f(extents.X, i * cellSize.Y);
                starts[i + (int)cellCount.X] = edgeStart;
                ends[i + (int)cellCount.X] = edgeEnd;
            }
        }

        canvas.DrawLines(starts, ends, color);
    }

    public void BuildGrid(float2[] positionsArray, int[] activeArray)
    {
        lock(gridValues) lock(gridValueCounts) lock(gridKeys) lock (gridKeysArray) lock(gridValueCountsArray)
        {
            
            var gridValueCountsLocal = new int[cellCountLinear];

            GridAccess grid = this;

            // store the item indicies in a temp array and
            // count up the number of values in each cell
            for(var i = 0; i < positionsArray.Length; i++)
            {
                if(activeArray[i] == 0) continue;

                var index = GetIndex(new Vector2f(positionsArray[i].X, positionsArray[i].Y));
                grid.itemIndiciesTemp[i] = index;
                gridValueCountsLocal[index]++;
            }

            
            // the grid keys are the starting indicies for each cell
            // these are calculated by adding the previous cell's value count
            // set the value counts to 0 because we will use it as a counter in the next pass

            gridKeysArray[0] = 0;

            for (var i = 0; i < grid.gridKeysArray.Length-1; i++)
            {
                grid.gridKeysArray[i+1] = grid.gridKeysArray[i] + gridValueCountsLocal[i];
                gridValueCountsLocal[i] = 0;
            }

            gridValueCountsLocal[gridValueCountsLocal.Length - 1] = 0;

            // store the particle indicies in the grid values array
            // the grid values are the actual indicies that are passed into the particle data arrays to get data about that particle
            for (var i = 0; i < positionsArray.Length; i++)
            {
                if (activeArray[i] == 0) continue;
                
                var index = grid.itemIndiciesTemp[i];

                var valueIndex = grid.gridKeysArray[index] + gridValueCountsLocal[index];

                if (valueIndex >= grid.gridValuesArray.Length)
                {
                    throw new Exception($"ValueIndex ({valueIndex}) must be less than gridValues.Length ({grid.gridValuesArray.Length})");
                }

                grid.gridValuesArray[valueIndex] = i;
                gridValueCountsLocal[index]++;
            }

            gridValues.CopyFrom(gridValuesArray);
            gridValueCounts.CopyFrom(gridValueCountsLocal);
            gridKeys.CopyFrom(gridKeysArray);
            gridValueCountsArray = gridValueCountsLocal;

        }
    }

    // runs the build grid method using Parallel.ForEach and a partitioner
    // locks the gridValues, gridValueCounts, gridKeys, and itemIndiciesTemp arrays
    public void BuildGridThreaded(float2[] positionsArray, int[] activeArray)
    {
        lock(gridValues) lock(gridValueCounts) lock(gridKeys) lock (gridKeysArray) lock(gridValueCountsArray)
        {
            var gridValueCountsLocal = new int[cellCountLinear];

            GridAccess grid = this;

            var partitioner = Partitioner.Create(0, positionsArray.Length, positionsArray.Length / Environment.ProcessorCount);

            // store the item indicies in a temp array and
            // count up the number of values in each cell
            Parallel.ForEach(partitioner, range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    if (activeArray[i] == 0) continue;

                    var index = grid.GetIndex(positionsArray[i].ToVector2f());
                    grid.itemIndiciesTemp[i] = index;
                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            // the grid keys are the starting indicies for each cell
            // these are calculated by adding the previous cell's value count
            // set the value counts to 0 because we will use it as a counter in the next pass

            gridKeysArray[0] = 0;

            for (var i = 0; i < grid.gridKeysArray.Length - 1; i++)
            {
                grid.gridKeysArray[i + 1] = grid.gridKeysArray[i] + gridValueCountsLocal[i];
                gridValueCountsLocal[i] = 0;
            }

            gridValueCountsLocal[gridValueCountsLocal.Length - 1] = 0;

            // store the particle indicies in the grid values array
            // the grid values are the actual indicies that are passed into the particle data arrays to get data about that particle
            Parallel.ForEach(partitioner, range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    if (activeArray[i] == 0) continue;

                    var index = grid.itemIndiciesTemp[i];

                    var valueIndex = grid.gridKeysArray[index] + gridValueCountsLocal[index];

                    if (valueIndex >= grid.gridValuesArray.Length)
                    {
                        throw new Exception($"ValueIndex ({valueIndex}) must be less than gridValues.Length ({grid.gridValuesArray.Length})");
                    }

                    grid.gridValuesArray[valueIndex] = i;
                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            gridValues.CopyFrom(gridValuesArray);
            gridValueCounts.CopyFrom(gridValueCountsLocal);
            gridKeys.CopyFrom(gridKeysArray);
            gridValueCountsArray = gridValueCountsLocal;
            
        }
    }

    public void Dispose()
    {
        gridValues.Dispose();
        gridValueCounts.Dispose();
        gridKeys.Dispose();
    }
}