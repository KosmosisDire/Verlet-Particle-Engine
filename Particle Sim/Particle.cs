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

    public Vector2f PositionF { get => particleSystem.positionsCPU[ID].ToVector2f(); set => particleSystem.positionsCPU[ID] = value.ToFloat2(); }

    public Vector2f VelocityF
    {
        get => Position.ToVector2f() - particleSystem.lastPositionsCPU[ID].ToVector2f();

        set => particleSystem.lastPositionsCPU[ID] = (PositionF - value).ToFloat2();
    }

    public readonly bool initialized = false;

    public void Accelerate(Vector2f acceleration)
    {
        particleSystem.lastPositionsCPU[ID] = (particleSystem.lastPositionsCPU[ID].ToVector2f() - acceleration).ToFloat2();
    }

    public void SetColor(uint4 color)
    {
        particleSystem.colorsCPU[ID] = Utils.RGBAToInt(color);
    }

    public void Link(Particle other, float length)
    {
        new ParticleLink(this, other, length);
    }

    public List<Particle> GetLinkedParticles(bool recursive = false, bool includeThis = false, List<Particle> list = null)
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

    public Particle(Vector2f position, Color color, ParticleSystem particleSystem)
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