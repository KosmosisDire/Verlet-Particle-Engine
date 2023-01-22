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
        get
        {
            return (Position.ToVector2f() - particleSystem.lastPositionsCPU[ID].ToVector2f());
        }

        set => particleSystem.lastPositionsCPU[ID] = (Position.ToVector2f() - value).ToFloat2();
    }

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
        particleSystem.particles.Add(this);

        this.particleSystem = particleSystem;
        this.PositionF = position;
        particleSystem.lastPositionsCPU[ID] = position.ToFloat2();
        particleSystem.colorsCPU[ID] = color.ToUInt32();
        particleSystem.particleCount++;
    }

    public void Destroy()
    {
        particleSystem.particles.Remove(this);
        particleSystem.particleCount--;
        
        for(int i = 0; i < links.Count;)
        {
            links[i].Destroy();
        }
    }
}