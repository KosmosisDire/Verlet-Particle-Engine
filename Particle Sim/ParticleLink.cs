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

    public int4 IntLink => new int4(particle1.ID, particle2.ID, Utils.SingleToInt32Bits(length), 1);

    public ParticleLink(Particle particle1, Particle particle2, float length)
    {

        this.particle1 = particle1;
        this.particle2 = particle2;
        this.length = length;

        if(particle1.particleSystem != particle2.particleSystem)
        {
            throw new Exception("Two linked particles are not in the same particle system");
        }

        particle1.linkedParticles.Add(particle2);
        particle2.linkedParticles.Add(particle1);

        particle1.links.Add(this);
        particle2.links.Add(this);

        particle1.particleSystem.particleLinks.Add(this);
        particle1.particleSystem.linksCPU.Add(IntLink);
        particle1.particleSystem.linkStrainCPU.Add(0);
    }

    public void Destroy()
    {
        particle1.linkedParticles.Remove(particle2);
        particle2.linkedParticles.Remove(particle1);
        
        particle1.links.Remove(this);
        particle2.links.Remove(this);

        particle1.particleSystem.particleLinks.Remove(this);
        particle1.particleSystem.linksCPU.Remove(ID);
        particle1.particleSystem.linkStrainCPU.Remove(ID);
    }
}