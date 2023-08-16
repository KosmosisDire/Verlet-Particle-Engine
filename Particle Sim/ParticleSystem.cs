using ComputeSharp;
using SFML.Graphics;
using SFML.System;
using ParticlePhysics.Internal;
using System.Diagnostics;

namespace ParticlePhysics;


public class ParticleSystem : IDisposable
{
    //General Properties
    public int maxParticles;
    public int particleCount = 0;
    public float particleRadius { get; private set; }
    public Vector2i worldExtents { get; private set; }

    //public properties
    public PersistentListDestroyable<Particle> particles;

    // CPU shader buffers
    public float2[] positionsCPU;
    public float2[] lastPositionsCPU;
    public uint[] colorsCPU;
    
    public PersistentListDestroyable<ParticleLink> particleLinks;
    public PersistentList<int4> linksCPU;
    public PersistentList<float> linkStrainCPU;
    public int nextLinkID = 0;
    public const float linkStrength = 50;


    // private properties
    public GridAccess grid;
    readonly float cellDivisor = 3f;

    // shader properties
    readonly ReadWriteBuffer<float2> positions;
    readonly ReadWriteBuffer<float2> lastPositions;
    readonly ReadWriteBuffer<float> travelDistances;
    ReadOnlyBuffer<int4> links;
    ReadWriteBuffer<float> linkStrain;
    readonly ReadOnlyBuffer<uint> colors;
    readonly ReadOnlyBuffer<int> active;

    public Vector2f gravity = new(0, 0);
    public float antiPressurePower = 0.25f;
    public int iterations = 5;


   readonly Stopwatch timer = new();

    public ParticleSystem(int maxParticles, int width, int height, float radius)
    {
        this.maxParticles = maxParticles;
        this.worldExtents = new Vector2i(width, height);
        this.particleRadius = radius;

        var col = Enumerable.Repeat(0xFFFFFFFF, maxParticles).ToArray();

        positions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float2>(maxParticles);
        lastPositions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float2>(maxParticles);
        travelDistances = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>(maxParticles);
        links = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int4>(maxParticles*2);
        linkStrain = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>(maxParticles*2);
        colors = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<uint>(col);
        active = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int>(maxParticles);

        positionsCPU = new float2[maxParticles];
        lastPositionsCPU = new float2[maxParticles];
        colorsCPU = col;

        particles = new PersistentListDestroyable<Particle>(maxParticles, maxParticles, true, true);
        particleLinks = new PersistentListDestroyable<ParticleLink>(maxParticles/2, maxParticles * 2, true, false);
        linksCPU = new PersistentList<int4>(maxParticles/2, maxParticles * 2, true, false);
        linkStrainCPU = new PersistentList<float>(maxParticles/2, maxParticles * 2, true, false);

        Vector2i cellCount = new Vector2i((int)((width/(radius*2))/cellDivisor), (int)((height/(radius*2))/cellDivisor));
        grid = new GridAccess(cellCount, new Vector2i(width, height), maxParticles);
    }

    public Particle AddParticle(Vector2f position, Color? color = null)
    {
        var c = color ?? Color.White;
        return new Particle(position, c, this);
    }

    public void RegenerateGrid()
    {
        Vector2i cellCount = new Vector2i((int)((worldExtents.X/(particleRadius*2))/cellDivisor), (int)((worldExtents.Y/(particleRadius*2))/cellDivisor));
        grid.SetCellCount(cellCount);
    }

    public void SetRadius(float radius)
    {
        this.particleRadius = radius;
        RegenerateGrid();
    }

    public void CopyColorsToGPU()
    {
        colors.CopyFrom(colorsCPU);
    }

    public ReadWriteBuffer<float2> GetGPUPositions()
    {
        return positions;
    }

    public ReadOnlyBuffer<uint> GetGPUColors()
    {
        return colors;
    }

    public ReadOnlyBuffer<int> GetGPUActive()
    {
        return active;
    }

    public ReadOnlyBuffer<int4> GetGPULinks()
    {
        return links;
    }

    public ReadOnlySpan<int> GetParticlesInGridAtPosition(Vector2f position)
    {
        
        var gridIndex = grid.GetIndex(position);
        var gridValueCount = grid.gridValueCountsArray[gridIndex];
        var gridValueStart = grid.gridKeysArray[gridIndex];

        return grid.gridValuesArray.AsSpan(gridValueStart, gridValueCount);
    }

    public ReadOnlySpan<int> GetParticlesInGridPosition(int index)
    {
        var gridValueCount = grid.gridValueCountsArray[index];
        var gridValueStart = grid.gridKeysArray[index];

        return grid.gridValuesArray.AsSpan(gridValueStart, gridValueCount);
    }

    public MovingAverage gridTime = new(100);
    public MovingAverage collisionTime = new(100);
    public MovingAverage linkTime = new(100);
    public MovingAverage copyTime = new(100);
    public MovingAverage totalUpdateTime = new(10);

    public void SolveParticles(float dt)
    {
        lock(positionsCPU) positions.CopyFrom(positionsCPU);
        lock(lastPositionsCPU) lastPositions.CopyFrom(lastPositionsCPU);
        lock(active) active.CopyFrom(particles.GetActiveArray());

        timer.Restart();

        // --------------------------
        RegenerateGrid();
        grid.BuildGridThreaded(positionsCPU, particles.GetActiveArray());
        // --------------------------
        
        timer.Stop();
        gridTime.Add(timer.ElapsedMilliseconds);
    
        timer.Restart();
        // --------------------------
        GraphicsDevice.GetDefault().For(maxParticles, new IntegrationKernel
        (
            positions, 
            lastPositions, 
            travelDistances,
            active,
            grid.gridValues, 
            grid.gridValueCounts,
            grid.gridKeys,
            particleRadius, 
            worldExtents.ToInt2(), 
            grid.cellCount.ToInt2(),
            grid.cellCountLinear,
            grid.cellSize.ToFloat2(),
            dt, 
            gravity.ToFloat2(),
            antiPressurePower,
            iterations
        ));

        timer.Stop();
        collisionTime.Add(timer.ElapsedMilliseconds);

        timer.Restart();
        // --------------------------
        lock(linksCPU)
        {
            if(linksCPU.Count > 0)
            {
                if(linksCPU.Count >= links.Length)
                {
                    links.Dispose();
                    links = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int4>((int)MathF.Ceiling(linksCPU.Count * 1.33f));
                    
                    linkStrain.Dispose();
                    linkStrain = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>((int)MathF.Ceiling(linksCPU.Count * 1.33f));
                }
                
                lock(linksCPU) links.CopyFrom(linksCPU.ToArray());
                lock(linkStrainCPU) linkStrain.CopyFrom(linkStrainCPU.ToArray());

                GraphicsDevice.GetDefault().For(linksCPU.Count, new LinkKernel(links, positions, linkStrain, particleRadius, dt));

                lock(linkStrainCPU) linkStrain.CopyTo(linkStrainCPU.GetArray());
                

                lock(linkStrainCPU) lock(particleLinks) lock(linksCPU)
                {
                    for(var i = 0; i < linkStrainCPU.Count; i++)
                    {
                        if(linksCPU[i].W == 1 && linkStrainCPU[i] > linkStrength)
                        {
                            particleLinks[i].Destroy();
                        }
                    }
                }
            }
        }

        timer.Stop();
        linkTime.Add(timer.ElapsedMilliseconds);
        
        timer.Restart();
        positions.CopyTo(positionsCPU);
        lastPositions.CopyTo(lastPositionsCPU);
        timer.Stop();
        copyTime.Add(timer.ElapsedMilliseconds);

        totalUpdateTime.Add(gridTime + collisionTime + linkTime + copyTime);
    }

    public Particle? Raycast(Vector2f origin, Vector2f direction, float maxDistance, out List<Vector2f> gridIntersections)
    {
        direction = direction.Normalized();
        Vector2f end = origin + direction * maxDistance;

        var gridIndicies = grid.LineIntersectionIndicies(origin, end, out gridIntersections);

        lock (grid.gridKeysArray) lock(grid.gridValueCountsArray) lock(grid.gridValuesArray)
        {
            for (int i = 0; i < gridIndicies.Count; i++)
            {
                Particle closestParticle = null;
                float closestDistance = float.MaxValue;

                var index = gridIndicies[i];
                var gridCellCount = grid.gridValueCountsArray[index];
                var gridCellStart = grid.gridKeysArray[index];
                var gridCellEnd = gridCellStart + gridCellCount;

                for (int j = gridCellStart; j < gridCellEnd; j++)
                {
                    var particleIndex = grid.gridValuesArray[j];
                    var particlePos = positionsCPU[particleIndex].ToVector2f();

                    if(Math2D.LineSegmentIntersectsCircle(origin, end, particlePos, particleRadius))
                    {
                        var distance = (particlePos - origin).SquareMagnitude();
                        if(distance < closestDistance)
                        {
                            closestParticle = particles[particleIndex];
                            closestDistance = distance;
                        }
                    }
                }

                if(closestParticle != null) return closestParticle;
            }
        }

        return null;
    }

    public void Dispose()
    {
        positions.Dispose();
        lastPositions.Dispose();
        travelDistances.Dispose();
        links.Dispose();
        colors.Dispose();
        active.Dispose();
        grid.Dispose();
    }
}



