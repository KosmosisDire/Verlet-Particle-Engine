using ComputeSharp;
using SFML.Graphics;
using SFML.System;
using System.Collections.Generic;
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
    float gridTimeAvg = 0;
    float integrationTimeAvg = 0;
    float cellDivisor = 6f;

    // shader properties
    ReadWriteBuffer<float2> positions;
    ReadWriteBuffer<float2> lastPositions;
    ReadWriteBuffer<float> travelDistances;
    ReadOnlyBuffer<int4> links;
    ReadWriteBuffer<float> linkStrain;
    ReadOnlyBuffer<uint> colors;
    ReadOnlyBuffer<int> active;

    public float showThreshold = 1;
    public Vector2f gravity = new Vector2f(0, 0);
    public float antiPressurePower = 2;


    Stopwatch timer = new Stopwatch();

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


    public void SolveParticles(float dt, bool debugTime = false)
    {
        positions.CopyFrom(positionsCPU);
        lastPositions.CopyFrom(lastPositionsCPU);
        active.CopyFrom(particles.GetActiveArray());


        timer.Restart();

        // --------------------------
        grid.BuildGrid(positionsCPU, particles.GetActiveArray());
        // --------------------------
        
        #region timing
        timer.Stop();
        gridTimeAvg = timer.ElapsedMilliseconds * 0.1f + gridTimeAvg * 0.9f;
        if(debugTime) Console.WriteLine($"Build Grid: {gridTimeAvg}ms");
        timer.Restart();
        #endregion

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
            antiPressurePower
        ));

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

                // if(linkData.Count < links.Length * 0.5f)
                // {
                //     links.Dispose();
                //     links = GraphicsDevice.GetDefault().AllocateReadOnlyBuffer<int3>((int)MathF.Ceiling(linkData.Count * 1.33f));

                //     linkStrain.Dispose();
                //     linkStrain = GraphicsDevice.GetDefault().AllocateReadWriteBuffer<float>((int)MathF.Ceiling(linkData.Count * 1.33f));
                // }

                links.CopyFrom(linksCPU.ToArray());
                linkStrain.CopyFrom(linkStrainCPU.ToArray());

                GraphicsDevice.GetDefault().For(linksCPU.Count, new LinkKernel(links, positions, linkStrain, particleRadius, dt));

                linkStrain.CopyTo(linkStrainCPU.GetArray());

                for(int i = 0; i < linkStrainCPU.Count; i++)
                {
                    if(linksCPU[i].W == 1 && linkStrainCPU[i] > linkStrength)
                    {
                        particleLinks[i].Destroy();
                    }
                }
            }
        }

        

        positions.CopyTo(positionsCPU);
        lastPositions.CopyTo(lastPositionsCPU);

        #region timing
        timer.Stop();
        integrationTimeAvg = timer.ElapsedMilliseconds * 0.1f + integrationTimeAvg * 0.9f;
        if(debugTime) Console.WriteLine($"Integration: {integrationTimeAvg}ms");
        #endregion
    }

    public Particle? Raycast(Vector2f origin, Vector2f direction, float maxDistance, out List<Vector2f> gridIntersections, out List<Vector2f> samples)
    {
        direction = direction.Normalized();
        Vector2f end = origin + direction * maxDistance;

        // Particle closestParticle = null;
        // float closestDistance = float.MaxValue;

        // for (int i = 0; i < particles.Count; i++)
        // {
        //     var particle = particles[i];
        //     if(particle == null) continue;
        //     var particlePos = particle.PositionF;
        //     var originToParticle = particlePos - origin;
        //     var originToEnd = end - origin;

        //     if (originToParticle.LengthSquared() > originToEnd.LengthSquared()) continue;

        //     var perpendicularToIdeal = originToParticle.PerpendicularClockwise().Normalized() * particleRadius;
        //     var intersectRayWithPerpendicular = Math2D.LineSegementsIntersect(origin, end, particlePos + perpendicularToIdeal, particlePos - perpendicularToIdeal, out Vector2f rayIntersection);
            
        //     if(intersectRayWithPerpendicular)
        //     {
        //         var distance = (particlePos - origin).LengthSquared();
        //         if(distance < closestDistance)
        //         {
        //             closestParticle = particle;
        //             closestDistance = distance;
        //         }
        //     }
        // }

        // gridIntersections = null;
        // samples = null;

        // return closestParticle;
        
        var gridIndicies = grid.LineIntersectionIndicies(origin, end, out gridIntersections, out samples);

        lock (grid.gridKeysArray) lock(grid.gridValueCountsArray) lock(grid.gridValuesArray)
        {
            for (int i = 0; i < gridIndicies.Count; i++)
            {
                var index = gridIndicies[i];
                var gridCellStart = grid.gridKeysArray[index];
                var gridCellCount = grid.gridValueCountsArray[index];

                Particle closestParticle = null;
                float closestDistance = float.MaxValue;

                for (int j = 0; j < gridCellCount; j++)
                {
                    var particleIndex = grid.gridValuesArray[gridCellStart + j];
                    var particle = particles[particleIndex];

                    if(particle == null) continue;

                    var particlePos = particle.PositionF;

                    var originToParticle = particlePos - origin;
                    var originToEnd = end - origin;

                    if (originToParticle.LengthSquared() > originToEnd.LengthSquared()) continue;

                    var perpendicularToIdeal = originToParticle.PerpendicularClockwise().Normalized() * particleRadius;

                    var intersectRayWithPerpendicular = Math2D.LineSegementsIntersect(origin, end, particlePos + perpendicularToIdeal, particlePos - perpendicularToIdeal, out Vector2f rayIntersection);

                    if(intersectRayWithPerpendicular)
                    {
                        var distance = (particlePos - origin).LengthSquared();
                        if(distance < closestDistance)
                        {
                            closestParticle = particle;
                            closestDistance = distance;
                        }

                        closestParticle = particle;
                    }
                }

                if (gridCellCount > 0) return closestParticle;

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



