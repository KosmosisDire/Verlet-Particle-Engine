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

    public readonly ReadWriteBuffer<int> gridValues;
    public readonly ReadWriteBuffer<int> gridValueCounts;
    public readonly ReadWriteBuffer<int> gridKeys;

    public readonly float radius;
    public readonly int2 extents;
    public readonly int2 cellCount;
    public readonly int cellCountLinear;
    public readonly float2 cellSize;
    public readonly float dt;
    public readonly float2 gravity;
    public readonly float antiPressurePower;
    #endregion

    public void Execute()
    {
        int id = ThreadIds.X;

        if(id >= positions.Length) return;
        if (active[id] == 0) return;

        Integrate(id, dt);
        
        for(int i = 0; i < 15; i++)
        {
            Collisions(id);
            ApplyBoundary(id);
        }
    }

    public void Integrate(int id, float _dt)
    {
        float2 pos = positions[id];
        float2 lastPos = lastPositions[id];
        float2 velocity = (pos - lastPos);
        float velocityMag = Hlsl.Length(velocity);

        float inertia = 1.0f + (travelDistances[id]) / (velocityMag + 1.0f);
        float antiPressure = (float)Hlsl.Pow(1.0f / inertia, 2);

        lastPositions[id] -= gravity * _dt * _dt * antiPressure;

        lastPos = lastPositions[id];
        velocity = (pos - lastPos);

        lastPositions[id] = pos;
        
        positions[id] += velocity * 0.98f * (1 - (antiPressurePower * (1-antiPressure)));
        
        var travelRetention = 0.75f;
        travelDistances[id] *= travelRetention;
    }

    void Collisions(int id)
    {
        int2 centerGridCell = GetGridCoord(positions[id]);
        int2 startCoord = centerGridCell - 1;

        float2 pos = positions[id];
        float2 edgeDist = new float2(pos.X % cellSize.X, pos.Y % cellSize.Y);
        bool isEdge = Hlsl.Any(edgeDist < radius * 1.1f) || Hlsl.Any(edgeDist > cellSize - radius * 1.2f);

        // 9 iterations for a 3x3 grid
        for(int i = 0; i < 9; i++)
        {
            int2 offset = new int2((int)(i % 3.0f), (int)(i / 3.0f));
            int2 cellCoord = startCoord + offset;

            if (cellCoord.X < 0 || cellCoord.Y < 0 || cellCoord.X >= cellCount.X || cellCoord.Y >= cellCount.Y) 
            {
                continue;
            }

            if(!isEdge && i != 4)
            {
                continue;
            }

            int cellIndex = IndexFromCoord(cellCoord);
            int particleCount = gridValueCounts[cellIndex];
            int particleStart = gridKeys[cellIndex];
            int particleEnd = particleStart + particleCount;

            for(int j = particleStart; j < particleEnd; j++)
            {
                int other = gridValues[j];
                SolveCollision(id, other);
            }
        }
    }


    void SolveCollision(int obj, int other)
    {
        if(obj >= other) return;

        float2 otherPos = positions[other];
        float2 objPos = positions[obj];

        float2 diff = objPos - otherPos;
        float sqrDist = Hlsl.Dot(diff, diff);

        
        float collisionDiameter = radius * 2.1f;

        if (sqrDist > collisionDiameter * collisionDiameter) return;

        float dist = Hlsl.Sqrt(sqrDist);
        

        if(dist == 0) 
        {
            positions[obj] += new float2(0.1f, -0.1f);
            otherPos = positions[obj];
            dist = 0.458f;
        }

        var normDir = diff / dist;
        float delta = collisionDiameter - dist;

        Move(obj, normDir * delta * 0.5f);
        Move(other, -normDir * delta * 0.5f);

        // float2 otherVelocity = otherPos - lastPositions[other];
        // float2 objVelocity = objPos - lastPositions[obj];
        // float otherSpeed = Hlsl.Length(otherVelocity);
        // float objSpeed = Hlsl.Length(objVelocity);

        //friction
        // float friction = Hlsl.Abs(Hlsl.Min(Hlsl.Dot(otherVelocity/otherSpeed, objVelocity/objSpeed), 0));
        // lastPositions[obj] += objVelocity * friction;
        // lastPositions[other] += otherVelocity * friction;
    }
    
    void ApplyBoundary(int id)
    {
        float top = 0;
        float bottom = extents.Y;
        float left = 0;
        float right = extents.X;

        float2 pos = positions[id];
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


