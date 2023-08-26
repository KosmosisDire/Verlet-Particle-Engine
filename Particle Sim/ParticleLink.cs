using ProtoEngine;

namespace ParticlePhysics;

public interface IHasID
{
    int ID { get; set;}
}

public interface IDestroyable
{
    void Destroy();
}

public class ParticleLink : IHasID, IDestroyable
{
    public Particle particle1;
    public Particle particle2;
    public float length;
    public int ID { get; set; }

    public int3 IntLink => new(particle1.ID, particle2.ID, Extentions.SingleToInt32Bits(length));

    public readonly bool initialized = false;

    public ParticleLink(Particle particle1, Particle particle2, float length)
    {
        this.particle1 = particle1;
        this.particle2 = particle2;
        this.length = length;

        if(particle1.particleSystem != particle2.particleSystem)
        {
            throw new Exception("Two linked particles are not in the same particle system");
        }

        if(particle1 == particle2)
        {
            throw new Exception("Two linked particles are the same");
        }

        if(particle1.linkedParticles.Count >= particle1.particleSystem.maxLinksPerParticle)
        {
            throw new Exception("Particle 1 has too many links, max is " + particle1.particleSystem.maxLinksPerParticle);
        }

        if(particle2.linkedParticles.Count >= particle2.particleSystem.maxLinksPerParticle)
        {
            throw new Exception("Particle 2 has too many links, max is " + particle2.particleSystem.maxLinksPerParticle);
        }

        particle1.particleSystem.AddLink(this);
        initialized = true;
    }

    public void Destroy()
    {
        particle1.particleSystem.RemoveLink(this);
    }
}