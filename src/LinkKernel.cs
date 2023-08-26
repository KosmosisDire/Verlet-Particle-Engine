using ComputeSharp;

namespace ParticlePhysics.Internal;


[AutoConstructor]
internal readonly partial struct LinkKernel : IComputeShader
{
    public readonly ReadOnlyBuffer<int4> links;
    public readonly ReadWriteBuffer<float2> positions;
    public readonly ReadWriteBuffer<float> linkStrain;
    public readonly float radius;
    public readonly float dt;
    

    public void Execute()
    {
        for(int i = 0; i < 10; i++)
        {
            int4 link = links[Hlsl.Min(ThreadIds.X + i, links.Length - 1)];
            if(link.W == 0) continue; // inactive link

            int a = link.X;
            int b = link.Y;
            float length = Hlsl.AsFloat(link.Z);

            float2 axis = positions[a] - positions[b];
            float dist = Hlsl.Length(axis);

            if(dist == 0) continue;

            float delta = length * 1.1f - dist;
            float percent = delta/dist;

            float2 axisNorm = Hlsl.Normalize(axis);

            float2 offset = (axisNorm * 0.9f + axis * 0.1f) * percent * 0.5f;
            positions[a] += offset;
            positions[b] -= offset;
        }

        int4 finalLink = links[ThreadIds.X];
        if(finalLink.W == 0) return; // inactive link
        int aFinal = finalLink.X;
        int bFinal = finalLink.Y;
        float lengthFinal = Hlsl.AsFloat(finalLink.Z);
        float2 axisFinal = positions[aFinal] - positions[bFinal];
        float distFinal = Hlsl.Length(axisFinal);

        if(distFinal > lengthFinal * 1.2f)
        {
            linkStrain[ThreadIds.X] += distFinal / (lengthFinal * 1.15f);
        }
        else if (distFinal < lengthFinal / 1.2f)
        {
            linkStrain[ThreadIds.X] -= (distFinal / (lengthFinal / 1.15f)) * 2f;
        }
        else
        {
            linkStrain[ThreadIds.X] -= 0.25f;
        }
    }
}



// using ComputeSharp;

// namespace ParticlePhysics.Internal;


// [AutoConstructor]
// internal readonly partial struct LinkKernel : IComputeShader
// {
//     public readonly ReadOnlyBuffer<int4> links;
//     public readonly ReadWriteBuffer<float2> positions;
//     public readonly ReadWriteBuffer<float> linkStrain;
//     public readonly float radius;
//     public readonly float dt;
    
//     const float epsilon = 0.0001f;

//     // returns whether it needs another iteration
//     public bool SolveJointForID(int a, int b, float length)
//     {
//         float2 axis = positions[b] - positions[a];
//         float distSquared = Hlsl.Dot(axis, axis);

//         if (distSquared < epsilon * epsilon || Hlsl.Abs(distSquared - (length*length)) < radius * 0.05f) return false;

//         float dist = Hlsl.Sqrt(distSquared);

//         float2 axisNorm = axis / dist;
//         float2 attachPoint = positions[b] - axisNorm * length;
//         float2 offset = attachPoint - positions[a];

//         positions[a] += offset * 0.25f;
//         positions[b] -= offset * 0.25f;

//         axis = positions[b] - positions[a];
//         distSquared = Hlsl.Dot(axis, axis);

//         if(distSquared > length * 1.15f || distSquared < length / 1.15f) return true;
//         return false;
//     }

//     public void Execute()
//     {
//         int4 link = links[ThreadIds.X];
//         if (link.W == 0) return; // inactive link

//         int a = link.X;
//         int b = link.Y;

//         float length = Hlsl.AsFloat(link.Z) * 1.1f;

//         int iterationCountdown = 5;
//         bool needsIteration = true;
//         while (iterationCountdown > 0 && needsIteration)
//         {
//             needsIteration = SolveJointForID(a, b, length);
//             iterationCountdown--;

//             // accumulate strain
//             float2 axis = positions[a] - positions[b];
//             float dist = Hlsl.Length(axis);
            
//             linkStrain[ThreadIds.X] = Hlsl.Max(linkStrain[ThreadIds.X] + ((dist / (length*1.1f)) - 1) * 0.25f, 0);
//         }
//     }
// }