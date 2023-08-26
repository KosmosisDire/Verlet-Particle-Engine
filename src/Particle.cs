using ProtoEngine;
using SFML.Graphics;
using SFML.System;

namespace ParticlePhysics;

public class Particle : IHasID, IDestroyable
{

    public int ID { get; set; }
    public ParticleSystem particleSystem { get; private set; }
    public List<Particle> linkedParticles = new();
    public List<ParticleLink> links = new();

    public float2 Position { get => particleSystem.positionsCPU[ID]; set => particleSystem.positionsCPU[ID] = value; }

    public Vector2 PositionF { get => particleSystem.positionsCPU[ID]; set => particleSystem.positionsCPU[ID] = value; }

    public Vector2 VelocityF
    {
        get => Position - particleSystem.lastPositionsCPU[ID];

        set => particleSystem.lastPositionsCPU[ID] = PositionF - value;
    }

    public readonly bool initialized = false;

    public void Accelerate(Vector2 acceleration)
    {
        particleSystem.lastPositionsCPU[ID] = (Vector2)particleSystem.lastPositionsCPU[ID] - acceleration;
    }

    public void SetColor(uint4 color)
    {
        particleSystem.colorsCPU[ID] = new Color((byte)color.X, (byte)color.Y, (byte)color.Z, (byte)color.W).ToInteger();
    }

    public void Link(Particle other, float length)
    {
        new ParticleLink(this, other, length);
    }

    public List<Particle> GetLinkedParticles(bool recursive = false, bool includeThis = false, List<Particle>? list = null)
    {
        if(!recursive) return linkedParticles;

        if (list == null)
        {
            list = new();
            if(includeThis) list.Add(this);
        }
        
        foreach (var p in linkedParticles)
        {
            if (!list.Contains(p))
            {
                list.Add(p);
                p.GetLinkedParticles(true, false, list);
            }
        }
        return list;
    }

    public Particle(Vector2 position, Color color, ParticleSystem particleSystem)
    {
        this.particleSystem = particleSystem;
        particleSystem.AddParticle(this, position, color);
        // this.PositionF = position;
        initialized = true;
    }

    public void Destroy()
    {
        particleSystem.RemoveParticle(this);
    }
}