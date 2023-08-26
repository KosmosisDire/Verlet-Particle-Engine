using System.Security.Cryptography;
using ComputeSharp;

namespace ParticlePhysics.Internal;


[AutoConstructor]
internal readonly partial struct IntegrationKernel : IComputeShader
{
    #region Fields
    public readonly ReadWriteBuffer<float2> positions;
    public readonly ReadWriteBuffer<float2> lastPositions;
    public readonly ReadWriteBuffer<float> travelDistances;
    public readonly ReadOnlyBuffer<int> active;

    public readonly ReadWriteBuffer<int> gridValues; // first value of each cell is the count of particles in the cell
    public readonly ReadWriteBuffer<int> gridKeys; // holds the index of the first value of each cell. The index of this array is the cell ID

    public readonly ReadWriteBuffer<int> linkKeys; 
    public readonly ReadWriteBuffer<int3> links; // x = particle id1, y = particle id2, z = length, w = active
    public readonly int maxLinksPerParticle;
    public readonly ReadWriteBuffer<float> linkStrain;

    public readonly float radius;
    public readonly int2 extents;
    public readonly int2 cellCount;
    public readonly int cellCountLinear;
    public readonly float2 cellSize;
    public readonly float dt;
    public readonly float2 gravity;
    public readonly float antiPressurePower;
    public readonly int iterations;
    #endregion

    public void Execute()
    {
        int id = ThreadIds.X;


        if(id >= positions.Length) return;
        if (active[id] == 0) return;

        float2 pos = positions[id];
        float2 lastPos = lastPositions[id];
        float2 velocity = pos - lastPos;

        int localIterations = (int)Hlsl.Ceil(Hlsl.Min((Hlsl.Length(velocity) + epsilon) / radius, 1) * iterations);
        
        // only do as many collision iterations as we need based on velocity
        for(int i = 0; i < localIterations; i++)
        {
            SolveLinks(id);
            Collisions(id);
            ApplyBoundary(id, false);
            Integrate(id, dt/localIterations);
        }

        // do the remaining iterations with no collisions
        int iterationDiff = iterations - localIterations;
        for (int j = 0; j < iterationDiff; j++)
        {
            SolveLinks(id);
            ApplyBoundary(id, false);
        }
    }

    public void Integrate(int id, float _dt)
    {
        float2 pos = positions[id];
        float2 lastPos = lastPositions[id];
        lastPositions[id] = positions[id];

        float2 velocity = pos - lastPos;
        float velocityMag = Hlsl.Length(velocity);

        float inertia = 1.0f + travelDistances[id] / (velocityMag + 1.0f);
        float antiPressure = (float)Hlsl.Pow(1.0f / inertia, 2);
        travelDistances[id] *= 0.5f;

        float2 acceleration = gravity - velocity * 2f;

        positions[id] += velocity + acceleration * antiPressure * (_dt * _dt);
    }

    void ParticleToCellCollisions(int id, int cellID)
    {
        int gridCellStart = gridKeys[cellID] + 1;
        int particleCount = gridValues[gridCellStart - 1]; // the value before the first particle index is the count of particles in the cell
        int particleEnd = gridCellStart + particleCount;
        
        for(int j = gridCellStart; j < particleEnd; j++)
        {
            int other = gridValues[j];
            SolveCollision(id, other);
        }
    }

    void Collisions(int id)
    {
        int2 cellCoord = GetGridCoord(positions[id]);
        int cellID = IndexFromCoord(cellCoord);

        bool leftEdge = cellID % cellCount.X == 0;
        bool rightEdge = cellID % cellCount.X == cellCount.X - 1;
        bool topEdge = cellID < cellCount.X;
        bool bottomEdge = cellID >= cellCountLinear - cellCount.X;

        // int2 startCoord = cellCoord - 1;

        float2 pos = positions[id];
        float2 edgeDist = new float2(pos.X % cellSize.X, pos.Y % cellSize.Y);

        ParticleToCellCollisions(id, cellID); // center

        bool particleEdgeOptimization = true;
        if(particleEdgeOptimization)
        {
            float edgeMargin = radius * 1.1f;
            bool leftEdgeParticle = edgeDist.X < edgeMargin;
            bool rightEdgeParticle = edgeDist.X > cellSize.X - edgeMargin;
            bool topEdgeParticle = edgeDist.Y < edgeMargin;
            bool bottomEdgeParticle = edgeDist.Y > cellSize.Y - edgeMargin;

            if(!rightEdge && rightEdgeParticle) ParticleToCellCollisions(id, cellID + 1); // right
            if(!leftEdge && leftEdgeParticle) ParticleToCellCollisions(id, cellID - 1); // left
            if(!bottomEdge && bottomEdgeParticle) ParticleToCellCollisions(id, cellID + cellCount.X); // bottom
            if(!topEdge && topEdgeParticle) ParticleToCellCollisions(id, cellID - cellCount.X); // top
            if(!bottomEdge && !rightEdge && (bottomEdgeParticle || rightEdgeParticle)) ParticleToCellCollisions(id, cellID + cellCount.X + 1); // bottom right
            if(!bottomEdge && !leftEdge && (bottomEdgeParticle || leftEdgeParticle)) ParticleToCellCollisions(id, cellID + cellCount.X - 1); // bottom left
            if(!topEdge && !rightEdge && (topEdgeParticle || rightEdgeParticle)) ParticleToCellCollisions(id, cellID - cellCount.X + 1); // top right
            if(!topEdge && !leftEdge && (topEdgeParticle || leftEdgeParticle)) ParticleToCellCollisions(id, cellID - cellCount.X - 1); // top left
        }
        else
        {
            if(!rightEdge) ParticleToCellCollisions(id, cellID + 1); 
            if(!leftEdge) ParticleToCellCollisions(id, cellID - 1);
            if(!bottomEdge) ParticleToCellCollisions(id, cellID + cellCount.X); 
            if(!topEdge) ParticleToCellCollisions(id, cellID - cellCount.X); 
            if(!bottomEdge && !rightEdge) ParticleToCellCollisions(id, cellID + cellCount.X + 1);
            if(!bottomEdge && !leftEdge) ParticleToCellCollisions(id, cellID + cellCount.X - 1);
            if(!topEdge && !rightEdge) ParticleToCellCollisions(id, cellID - cellCount.X + 1);
            if(!topEdge && !leftEdge) ParticleToCellCollisions(id, cellID - cellCount.X - 1);
        }
    }

    const float epsilon = 0.0001f;
    float massScalingFunction(float input) 
    {
        return ((-24 * Hlsl.Pow(input, 5) + 60 * Hlsl.Pow(input, 4) + -50 * Hlsl.Pow(input, 3) + 15 * Hlsl.Pow(input, 2)) - 0.5f) * 3f + 0.5f;
    }
    void SolveCollision(int obj, int other)
    {
        float2 otherPos = positions[other];
        float2 objPos = positions[obj];

        float2 diff = objPos - otherPos;
        float sqrDist = Hlsl.Dot(diff, diff);
        float collisionDiameter = radius * 2f;

        if(sqrDist < collisionDiameter * collisionDiameter && sqrDist > epsilon) 
        {
            float2 velocity_obj = objPos - lastPositions[obj];
            float2 velocity_other = otherPos - lastPositions[other];
            float inertia_obj = 1.0f + travelDistances[obj] / (Hlsl.Length(velocity_obj) + 1.0f);
            float inertia_other = 1.0f + travelDistances[other] / (Hlsl.Length(velocity_other) + 1.0f);
            float totalMass = 1 / (inertia_obj + inertia_other);
            float massFactor_obj = massScalingFunction(inertia_obj * totalMass);
            float massFactor_other = massScalingFunction(inertia_other * totalMass);

            var dist = Hlsl.Sqrt(sqrDist);
            var normDir = diff / dist;
            var delta = normDir * (collisionDiameter - dist) * 0.5f;

            Move(obj, delta * massFactor_other);
            Move(other, -delta * massFactor_obj);

            float cohesion = 0.1f;
            float2 velocityDelta = velocity_obj - velocity_other;
            AddVelocity(obj, -cohesion * velocityDelta);
            AddVelocity(other, cohesion * velocityDelta);
        }
    }

    public void SolveLink(int particle1, int particle2, float length, int linkID)
    {
        float2 pos1 = positions[particle1];
        float2 pos2 = positions[particle2];
        float2 axis = pos1 - pos2;
        float dist = Hlsl.Length(axis);

        if(dist <= epsilon) return;

        float delta = length * 1.1f - dist;
        float percent = delta/dist;

        float2 axisNorm = Hlsl.Normalize(axis);

        float2 offset = (axisNorm * 0.9f + axis * 0.1f) * percent * 0.5f;
        Move(particle1, offset);
        Move(particle2, -offset);

        float cohesion = 0.01f;
        float2 pos1Last = lastPositions[particle1];
        float2 pos2Last = lastPositions[particle2];
        float2 velocity_obj = pos1 - pos1Last;
        float2 velocity_other = pos2 - pos2Last;
        float2 velocityDelta = velocity_obj - velocity_other;
        AddVelocity(particle1, -cohesion * velocityDelta);
        AddVelocity(particle2, cohesion * velocityDelta);

        linkStrain[linkID] = Hlsl.Max(linkStrain[linkID] + ((dist / (length*1.25f)) - 1) * 0.025f, 0);
    }

    public void SolveLinks(int id)
    {
        for (int i = 0; i <= maxLinksPerParticle; i++)
        {
            int linkID = linkKeys[id * maxLinksPerParticle + i];
            if (linkID == -1) break;
            int3 link = links[linkID];

            if (link.X == -1) continue; // inactive link

            int p1 = link.X;
            int p2 = link.Y;

            if (p1 == id) continue; // don't solve link twice

            float length = Hlsl.AsFloat(link.Z);


            SolveLink(p1, p2, length, linkID);
        }

        // int linkStart = id * maxLinksPerParticle;
        // int linkEnd = linkStart + maxLinksPerParticle;

        // if (linkKeys[linkStart] == -1) return;

        // for(int i = linkStart; i <= linkEnd; i++)
        // {
        //     int linkID = linkKeys[i];
        //     int4 link = links[linkID];

        //     if (link.W == 0) continue; // inactive link

        //     int p1 = link.X;
        //     int p2 = link.Y;

        //     // if (p1 == id) continue; // don't solve link twice

        //     float length = Hlsl.AsFloat(link.Z);

        //     SolveLink(p1, p2, length, linkID);
        // }
    }
    
    void ApplyBoundary(int id, bool circular)
    {
        float2 pos = positions[id];


        if(circular)
        {
            float2 diff = pos - (float2)extents * 0.5f;
            float dist = Hlsl.Length(diff);
            if(dist > extents.X * 0.5f)
            {
                float2 normDir = diff / dist;
                float2 delta = normDir * (dist - extents.X * 0.5f);
                Move(id, -delta);
            }

            return;
        }

        float top = 0;
        float bottom = extents.Y;
        float left = 0;
        float right = extents.X;

        if(pos.X - radius < left)
        {
            MoveX(id, left - (pos.X - radius));
        }
        else if(pos.X + radius > right)
        {
            MoveX(id, right - (pos.X + radius));
        }

        if(pos.Y + radius > bottom)
        {
            MoveY(id, bottom - (pos.Y + radius));
        }
        else if(pos.Y - radius < top)
        {
            MoveY(id, top - (pos.Y - radius));
        }
    }

    #region Helper Functions

    void Move(int id, float2 offset)
    {
        positions[id] += offset;
        travelDistances[id] += Hlsl.Abs(offset.X) + Hlsl.Abs(offset.Y);
    }
    void MoveX(int id, float offset)
    {
        positions[id].X += offset;
        travelDistances[id] += Hlsl.Abs(offset);
    }

    void MoveY(int id, float offset)
    {
        positions[id].Y += offset;
        travelDistances[id] += Hlsl.Abs(offset);
    }

    void AddVelocity(int id, float2 velocity)
    {
        lastPositions[id] -= velocity;
    }

    public int2 GetGridCoord(float2 position)
    {
        int x = (int)Hlsl.Floor(Hlsl.Clamp(position.X / cellSize.X, 0, cellCount.X - 1));
        int y = (int)Hlsl.Floor(Hlsl.Clamp(position.Y / cellSize.Y, 0, cellCount.Y - 1));
        return new int2(x, y);
    }

    public int IndexFromCoord(int2 coord)
    {
        int index = coord.X + coord.Y * cellCount.X;
        return index;
    }

    #endregion
    
}


