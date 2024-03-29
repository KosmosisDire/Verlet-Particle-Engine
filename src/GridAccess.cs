using System.Collections.Concurrent;
using ComputeSharp;
using SFML.System;
using SFML.Graphics;
using ProtoEngine;
using ProtoEngine.Rendering;

namespace ParticlePhysics.Internal;

public struct GridAccess : IDisposable
{
    public ReadWriteBuffer<int> gridValues;
    // public ReadWriteBuffer<int> gridValueCounts;
    public ReadWriteBuffer<int> gridKeys;
    
    public Vector2 cellSize;
    public Vector2 cellCount;
    public int cellCountLinear;

    int[] itemIndiciesTemp;
    public int[] gridValuesArray;
    public int[] gridKeysArray;
    public int[] gridValueCountsArray;

    public Vector2 extents;
    readonly int objectCount;

    public GridAccess(Vector2 cellCount, Vector2 extents, int objectCount)
    {
        this.extents = extents;
        this.objectCount = objectCount;
        cellCountLinear = (int)cellCount.X * (int)cellCount.Y;
        gridValueCountsArray = new int[cellCountLinear];


        this.cellCount = cellCount;
        this.cellSize = new Vector2((float)extents.X / cellCount.X, (float)extents.Y / cellCount.Y);

        gridValues = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(objectCount + cellCountLinear); // counts for each cell are also stored in the same buffer
        // gridValueCounts = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);
        gridKeys = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);

        itemIndiciesTemp = new int[objectCount];
        gridValuesArray = new int[objectCount + cellCountLinear]; // counts for each cell are also stored in the same buffer
        gridKeysArray = new int[cellCountLinear];
    }
    
    public void SetCellCount(Vector2 cellCount)
    {
        if (cellCount == this.cellCount) return;

        lock(gridValues) lock(gridKeys) lock (gridKeysArray) lock (itemIndiciesTemp) lock (gridValuesArray)
        {
            this.cellCount = cellCount;
            this.cellSize = new Vector2((float)extents.X / cellCount.X, (float)extents.Y / cellCount.Y);
            this.cellCountLinear = (int)cellCount.X * (int)cellCount.Y;

            gridValues = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(objectCount);
            // gridValueCounts = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);
            gridKeys = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(cellCountLinear);

            itemIndiciesTemp = new int[objectCount];
            gridValuesArray = new int[objectCount];
            gridKeysArray = new int[cellCountLinear];
        }
    }


    public int GetIndex(Vector2 position)
    {
        int x = (int)MathF.Floor(Math.Clamp(position.X / cellSize.X, 0, cellCount.X - 1));
        int y = (int)MathF.Floor(Math.Clamp(position.Y / cellSize.Y, 0, cellCount.Y - 1));
        int index = x + y * (int)cellCount.X;

        return index;
    }

    public bool PositionInsideGrid(Vector2 position)
    {
        return position.X >= 0 && position.X < extents.X && position.Y >= 0 && position.Y < extents.Y;
    }

    public List<int> LineIntersectionIndicies(Vector2 start, Vector2 end, out List<Vector2> intersections)
    {
        lock(gridValueCountsArray)
        {
            // this function will find the intersections of a line with each edge of each column and row of the grid
            // it will then average each neighboring intersection along the line and sample the index of the cell at that point
            
            List<int> indicies = new List<int>();
            intersections = new List<Vector2>();

            // calculate edges of grid

            for (int i = 0; i < cellCount.X + 1; i++)
            {
                var edgeStart = new Vector2(i * cellSize.X, 0);
                var edgeEnd = new Vector2(i * cellSize.X, extents.Y);

                if(Math2D.LineSegmentsIntersect(start, end, edgeStart, edgeEnd, out Vector2 intersection))
                {
                    intersections.Add(intersection);
                }
            }

            for (int i = 0; i < cellCount.Y + 1; i++)
            {
                var edgeStart = new Vector2(0, i * cellSize.Y);
                var edgeEnd = new Vector2(extents.X, i * cellSize.Y);

                if (Math2D.LineSegmentsIntersect(start, end, edgeStart, edgeEnd, out Vector2 intersection))
                {
                    intersections.Add(intersection);
                }
            }

            if (PositionInsideGrid(start)) intersections.Add(start);
            if (PositionInsideGrid(end)) intersections.Add(end);

            intersections.Sort((a, b) => (a - start).Magnitude.CompareTo((b - start).Magnitude));

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

    
    public void DrawGrid(Screen screen, Color color)
    {
        Vector2[] starts = new Vector2[cellCountLinear];
        Vector2[] ends = new Vector2[cellCountLinear];

        for(int i = 0; i < (int)MathF.Max(cellCount.X, cellCount.Y); i++)
        {
            if (i < cellCount.X)
            {
                var edgeStart = new Vector2(i * cellSize.X, 0);
                var edgeEnd = new Vector2(i * cellSize.X, extents.Y);
                starts[i] = edgeStart;
                ends[i] = edgeEnd;
            }
            if (i < cellCount.Y)
            {
                var edgeStart = new Vector2(0, i * cellSize.Y);
                var edgeEnd = new Vector2(extents.X, i * cellSize.Y);
                starts[i + (int)cellCount.X] = edgeStart;
                ends[i + (int)cellCount.X] = edgeEnd;
            }
        }

        screen.DrawLinesCPU(starts, ends, color, 10);
    }

    public void BuildGrid(float2[] positionsArray, int[] activeArray)
    {
        lock(gridValues) lock(gridKeys) lock (gridKeysArray) lock(gridValueCountsArray)
        {
            
            var gridValueCountsLocal = new int[cellCountLinear];

            GridAccess grid = this;

            // store the item indicies in a temp array and
            // count up the number of values in each cell
            for(int i = 0; i < positionsArray.Length; i++)
            {
                if(activeArray[i] == 0) continue;

                var index = GetIndex(new Vector2(positionsArray[i].X, positionsArray[i].Y));
                grid.itemIndiciesTemp[i] = index;
                gridValueCountsLocal[index]++;
            }

            
            // the grid keys are the starting indicies for each cell
            // these are calculated by adding the previous cell's value count
            // set the value counts to 0 because we will use it as a counter in the next pass

            gridKeysArray[0] = 0;

            for (int i = 0; i < grid.gridKeysArray.Length-1; i++)
            {
                grid.gridKeysArray[i+1] = grid.gridKeysArray[i] + gridValueCountsLocal[i];
                gridValueCountsLocal[i] = 0;
            }

            gridValueCountsLocal[gridValueCountsLocal.Length - 1] = 0;

            // store the particle indicies in the grid values array
            // the grid values are the actual indicies that are passed into the particle data arrays to get data about that particle
            for (int i = 0; i < positionsArray.Length; i++)
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
            // gridValueCounts.CopyFrom(gridValueCountsLocal);
            gridKeys.CopyFrom(gridKeysArray);
            gridValueCountsArray = gridValueCountsLocal;

        }
    }

    // runs the build grid method using Parallel.ForEach and a partitioner
    // locks the gridValues, gridValueCounts, gridKeys, and itemIndiciesTemp arrays
    public void BuildGridThreaded(float2[] positionsArray, int[] activeArray, int numThreads)
    {
        lock(gridValues) lock(gridKeys) lock (gridKeysArray) lock(gridValueCountsArray)
        {
            var gridValueCountsLocal = new int[cellCountLinear];

            GridAccess grid = this;

            var partitioner = Partitioner.Create(0, positionsArray.Length, positionsArray.Length / numThreads);

            // store the item indicies in a temp array and
            // count up the number of values in each cell
            Parallel.ForEach(partitioner, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (activeArray[i] == 0) continue;

                    var index = grid.GetIndex(positionsArray[i]);
                    grid.itemIndiciesTemp[i] = index;
                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            // the grid keys are the starting indicies for each cell
            // these are calculated by adding the previous cell's value count
            // set the value counts to 0 because we will use it as a counter in the next pass

            gridKeysArray[0] = 0;
            grid.gridValuesArray[0] = gridValueCountsLocal[0];

            for (int i = 0; i < grid.gridKeysArray.Length - 1; i++)
            {
                int key = grid.gridKeysArray[i] + gridValueCountsLocal[i] + 1; // +1 because the first value is the count of values in the cell
                grid.gridKeysArray[i + 1] = key;
                grid.gridValuesArray[key] = gridValueCountsLocal[i+1];
                gridValueCountsLocal[i] = 0;
            }

            gridValueCountsLocal[gridValueCountsLocal.Length - 1] = 0;

            // store the particle indicies in the grid values array
            // the grid values are the actual indicies that are passed into the particle data arrays to get data about that particle
            Parallel.ForEach(partitioner, range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (activeArray[i] == 0) continue;

                    var index = grid.itemIndiciesTemp[i];

                    var valueIndex = grid.gridKeysArray[index] + gridValueCountsLocal[index] + 1; // +1 because the first value is the count of values in the cell

                    if (valueIndex >= grid.gridValuesArray.Length)
                    {
                        throw new Exception($"ValueIndex ({valueIndex}) must be less than gridValues.Length ({grid.gridValuesArray.Length})");
                    }

                    grid.gridValuesArray[valueIndex] = i;
                    Interlocked.Increment(ref gridValueCountsLocal[index]);
                }
            });

            gridValues.CopyFrom(gridValuesArray);
            // gridValueCounts.CopyFrom(gridValueCountsLocal);
            gridKeys.CopyFrom(gridKeysArray);
            gridValueCountsArray = gridValueCountsLocal;
        }
    }

    public void Dispose()
    {
        gridValues.Dispose();
        // gridValueCounts.Dispose();
        gridKeys.Dispose();
    }
}