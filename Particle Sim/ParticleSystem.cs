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
    public int maxLinks;
    public int maxLinksPerParticle = 6;
    private int particleCount = 0;
    private int linkCount = 0;
    
    public int ParticleCount => particleCount;
    public int LinkCount => linkCount;

    public float particleRadius { get; private set; }
    public Vector2i boundsSize { get; private set; }

    //public properties
    public PersistentListDestroyable<Particle> particles;

    // CPU shader buffers
    public float2[] positionsCPU;
    public float2[] lastPositionsCPU;
    public uint[] colorsCPU;
    
    public PersistentListDestroyable<ParticleLink> particleLinks;
    public PersistentList<float> linkStrainCPU;
    public int nextLinkID = 0;
    public const float linkStrength = 1;


    // private properties
    public GridAccess grid;

    // shader properties
    readonly ReadWriteBuffer<float2> positions;
    readonly ReadWriteBuffer<float2> lastPositions;
    readonly ReadWriteBuffer<float> travelDistances;
    readonly ReadWriteBuffer<float> linkStrain;
    readonly ReadOnlyBuffer<uint> colors;
    readonly ReadOnlyBuffer<int> active;

    public readonly ReadWriteBuffer<int> linkKeys;
    public readonly ReadWriteBuffer<int3> links;
    public int3[] linksArray;
    private int[] linkKeysArray;



    public Vector2f gravity = new(0, 0);
    public float antiPressurePower = 0.25f;
    public int iterations = 5;

    private const float cellDivisor = 5;


    readonly Stopwatch timer = new();

    public Thread buildDataThread;

    public ParticleSystem(int maxParticles, int maxLinks, int width, int height, float radius)
    {
        this.maxParticles = maxParticles;
        this.maxLinks = maxLinks;
        this.boundsSize = new Vector2i(width, height);
        this.particleRadius = radius;

        var col = Enumerable.Repeat(0xFFFFFFFF, maxParticles).ToArray();

        positions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float2>(maxParticles);
        lastPositions = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float2>(maxParticles);
        travelDistances = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>(maxParticles);
        colors = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<uint>(col);
        active = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int>(maxParticles);

        linkStrain = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>(maxLinks);
        linkKeys = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int>(maxParticles*maxLinksPerParticle);
        links = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<int3>(maxLinks);

        positionsCPU = new float2[maxParticles];
        lastPositionsCPU = new float2[maxParticles];
        colorsCPU = col;

        particles = new PersistentListDestroyable<Particle>(maxParticles, maxParticles, true, true);
        particleLinks = new PersistentListDestroyable<ParticleLink>(maxParticles/4, maxLinks, true, false);
        linkStrainCPU = new PersistentList<float>(maxParticles/4, maxLinks, true, true);
        linksArray = Enumerable.Repeat(new int3(-1, -1, -1), maxLinks).ToArray();
        linkKeysArray = Enumerable.Repeat(-1, maxParticles*maxLinksPerParticle).ToArray();


        Vector2i cellCount = new Vector2i((int)((width/(radius*2))/cellDivisor), (int)((height/(radius*2))/cellDivisor));
        grid = new GridAccess(cellCount, new Vector2i(width, height), maxParticles);
    }

    public Particle AddParticle(Vector2f position, Color? color = null)
    {
        var c = color ?? Color.White;
        var p = new Particle(position, c, this);
        return p;
    }

    public Particle AddParticle(Particle particle, Vector2f position, Color color)
    {
        if(particle.initialized) return particle;

        lock(particles) particles.Add(particle);
        lock(positionsCPU) positionsCPU[particle.ID] = position.ToFloat2();
        lock(lastPositionsCPU) lastPositionsCPU[particle.ID] = position.ToFloat2();
        lock(colorsCPU) colorsCPU[particle.ID] = color.ToUInt32();
        
        particleCount++;

        return particle;
    }

    public void RemoveParticle(Particle particle)
    {
        lock(particles) particles.Remove(particle);
        for(int i = 0; i < particle.links.Count;)
        {
            particle.links[i].Destroy();
        }
        particleCount--;
    }

    public void AddLink(ParticleLink link)
    {
        if (link.initialized) return;

        lock(link.particle1.linkedParticles) link.particle1.linkedParticles.Add(link.particle2);
        lock(link.particle2.linkedParticles) link.particle2.linkedParticles.Add(link.particle1);

        lock(link.particle1.links) link.particle1.links.Add(link);
        lock(link.particle2.links) link.particle2.links.Add(link);

        lock(particleLinks) particleLinks.Add(link);
        lock(linksArray) linksArray[link.ID] = link.IntLink;
        lock(linkKeysArray) linkKeysArray[link.particle1.ID*maxLinksPerParticle + link.particle1.links.Count - 1] = link.ID;
        lock(linkKeysArray) linkKeysArray[link.particle2.ID*maxLinksPerParticle + link.particle2.links.Count - 1] = link.ID;
        lock(linkStrainCPU) linkStrainCPU.Add(0);
        linkCount++;
    }

    public void RemoveLink(ParticleLink link)
    {
        lock(link.particle1.linkedParticles) link.particle1.linkedParticles.Remove(link.particle2);
        lock(link.particle2.linkedParticles) link.particle2.linkedParticles.Remove(link.particle1);
        
        lock(link.particle1.links) link.particle1.links.Remove(link);
        lock(link.particle2.links) link.particle2.links.Remove(link);

        lock(particleLinks) particleLinks.Remove(link);
        lock(linksArray) linksArray[link.ID] = new int3(-1, -1, -1);
        lock(linkKeysArray) linkKeysArray[link.particle1.ID*maxLinksPerParticle + link.particle1.links.Count] = -1;
        lock(linkKeysArray) linkKeysArray[link.particle2.ID*maxLinksPerParticle + link.particle2.links.Count] = -1;
        lock(linkStrainCPU) linkStrainCPU.Remove(link.ID);
        linkCount--;
    }

    public void RegenerateGrid()
    {
        Vector2i cellCount = new Vector2i((int)((boundsSize.X/(particleRadius*2))/cellDivisor), (int)((boundsSize.Y/(particleRadius*2))/cellDivisor));
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



    public MovingAverage gridTime = new(10);
    public MovingAverage collisionTime = new(10);
    public MovingAverage linkTime = new(10);
    public MovingAverage strainTime = new(10);
    public MovingAverage copyTime = new(10);
    public MovingAverage waitTime = new(10);
    public MovingAverage totalSolveTime = new(4);
    public MovingAverage totalBuildTime = new(4);

    public void BuildData()
    {
        timer.Restart();
        var linkActive = linkStrainCPU.GetActiveArray();
        for(int i = 0; i < linkStrainCPU.Capacity; i++)
        {
            if(linkActive[i] == 1 && linkStrainCPU[i] > linkStrength)
            {
                particleLinks[i].Destroy();
            }
        }
        timer.Stop();
        strainTime.Add(timer.ElapsedMilliseconds);



        timer.Restart();
        RegenerateGrid();
        grid.BuildGridThreaded(positionsCPU, particles.GetActiveArray(), Environment.ProcessorCount);
        timer.Stop();
        gridTime.Add(timer.ElapsedMilliseconds);

        totalBuildTime.Add(gridTime + linkTime + strainTime);
    }

    int lastLinksCount = -1;
    public void SolveParticles(float dt)
    {
        BuildData();

        timer.Restart();
        lock(positionsCPU) positions.CopyFrom(positionsCPU);
        lock(lastPositionsCPU) lastPositions.CopyFrom(lastPositionsCPU);
        lock(active) active.CopyFrom(particles.GetActiveArray());
        lock(linkKeysArray) linkKeys.CopyFrom(linkKeysArray);
        lock(linksArray) links.CopyFrom(linksArray);
    
        timer.Stop();
        float copyTimeTemp = timer.ElapsedMilliseconds;
    
        timer.Restart();
        GraphicsDevice.GetDefault().For(maxParticles, 1, 1, 1024, 1, 1, new IntegrationKernel
        (
            positions, 
            lastPositions, 
            travelDistances,
            active,
            grid.gridValues, 
            grid.gridKeys,
            linkKeys,
            links,
            maxLinksPerParticle,
            linkStrain,
            particleRadius, 
            boundsSize.ToInt2(), 
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
        positions.CopyTo(positionsCPU);
        lastPositions.CopyTo(lastPositionsCPU);
        linkStrain.CopyTo(linkStrainCPU.ToArray());
        timer.Stop();
        copyTime.Add(copyTimeTemp + timer.ElapsedMilliseconds);

        totalSolveTime.Add(collisionTime + copyTime);
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
        colors.Dispose();
        active.Dispose();
        grid.Dispose();
        linkKeys.Dispose();
        linkStrain.Dispose();
    }
}



