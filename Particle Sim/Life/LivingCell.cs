using System.Collections.Concurrent;
using SFML.Graphics;
using SFML.System;
using ParticlePhysics;



public class LivingCell : Particle
{
    public float energy;
    public float maxEnergy;
    public float energyIntake;
    public float energyOutput;
    public float activation;
    public float lengthMultiplier;
    public bool sticky;
    public float mutationRate;
    public float mutationAmount;

    public LivingCell(Vector2f position, Color color, ParticleSystem ps, float energy, float maxEnergy, float energyIntake, float energyOutput, float lengthMultiplier, bool sticky, float mutationRate, float mutationAmount) : base(position, color, ps)
    {
        this.energy = energy;
        this.maxEnergy = maxEnergy;
        this.energyIntake = energyIntake;
        this.energyOutput = energyOutput;
        this.lengthMultiplier = lengthMultiplier;
        this.sticky = sticky;
        this.mutationRate = mutationRate;
        this.mutationAmount = mutationAmount;
    }

    public virtual void Update(float dt)
    {
        energy -= maxEnergy/240 * dt;
        energy -= activation * dt;

        foreach(LivingCell n in linkedParticles)
        {
            float amountTaken = MathF.Min(energyIntake, n.energyOutput) * ((maxEnergy - energy)/maxEnergy) * dt;
            energy += amountTaken;
            n.energy -= amountTaken;
        }

        if(energy <= 0)
        {
            Destroy();
        }
    }
}


public class Muscle : LivingCell
{
    public Muscle(Vector2f position, Color color, ParticleSystem ps, float energy, float maxEnergy, float energyIntake, float energyOutput, float lengthMultiplier, bool sticky, float mutationRate, float mutationAmount) : base(position, color, ps, energy, maxEnergy, energyIntake, energyOutput, lengthMultiplier, sticky, mutationRate, mutationAmount)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        foreach (var link in links)
        {
            link.length = particleSystem.particleRadius * 2 + activation * lengthMultiplier;
        }
    }
}

public class StemCell : LivingCell
{
    public Queue<Type> cellTypes = new();
    public Queue<object[]> cellArgs = new();

    public StemCell(List<Type> cellTypes, List<object[]> cellArgs, Vector2f position, Color color, ParticleSystem ps, float energy, float maxEnergy, float energyIntake, float energyOutput, float lengthMultiplier, bool sticky, float mutationRate, float mutationAmount) : base(position, color, ps, energy, maxEnergy, energyIntake, energyOutput, lengthMultiplier, sticky, mutationRate, mutationAmount)
    {
        this.cellTypes = new Queue<Type>(cellTypes);
        this.cellArgs = new Queue<object[]>(cellArgs);
    }

    public StemCell(Queue<Type> cellTypes, Queue<object[]> cellArgs, Vector2f position, Color color, ParticleSystem ps, float energy, float maxEnergy, float energyIntake, float energyOutput, float lengthMultiplier, bool sticky, float mutationRate, float mutationAmount) : base(position, color, ps, energy, maxEnergy, energyIntake, energyOutput, lengthMultiplier, sticky, mutationRate, mutationAmount)
    {
        this.cellTypes = cellTypes;
        this.cellArgs = cellArgs;
    }

    public override void Update(float dt)
    {
        foreach(LivingCell n in linkedParticles)
        {
            float amountTaken = MathF.Min(energyIntake * 2, n.energyOutput) * dt;
            energy += amountTaken;
            n.energy -= amountTaken;
        }
        
        object[] argsPeek = cellArgs.Peek();
        if(energy > (float)argsPeek[2] + (float)argsPeek[3]) // if enough energy
        {
            // convert to real cell
            Type type = cellTypes.Dequeue();
            object[] args = cellArgs.Dequeue();
            var c = particleSystem.AddLivingCell(type, args);
            c.energy = (float)args[2]; // transfer energy
            
            // create next stem cell
            if(cellArgs.Count > 0)
            {
                List<object> nextStemArgs = new(cellArgs.Peek());
                nextStemArgs.Insert(0, cellTypes);
                nextStemArgs.Insert(1, cellArgs);
                c = particleSystem.AddLivingCell(typeof(StemCell), nextStemArgs.ToArray());
                c.energy = (float)args[3]; // transfer energy
            }
        }

        Destroy();
    }
}

public class Replicator : LivingCell
{
    public Replicator(Vector2f position, Color color, ParticleSystem ps, float energy, float maxEnergy, float energyIntake, float energyOutput, float lengthMultiplier, bool sticky, float mutationRate, float mutationAmount) : base(position, color, ps, energy, maxEnergy, energyIntake, energyOutput, lengthMultiplier, sticky, mutationRate, mutationAmount)
    {
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        if (activation > 10)
        {
            List<Type> cellTypes = new();
            List<object[]> cellArgs = new();

            List<Particle> linked = GetLinkedParticles();
            
            float energyDrain = 0;
            float firstCellEnergy = 0;

            Random r = new();
            Vector2f positionalOffset = new((r.NextSingle() - 0.5f) * particleSystem.particleRadius * linked.Count, (r.NextSingle() - 0.5f) * particleSystem.particleRadius * linked.Count);
            foreach (Particle particle in linked)
            {
                if (particle is LivingCell)
                {
                    var p = (LivingCell)particle;
                    cellTypes.Add(p.GetType());

                    object[] pArgs = new object[]
                    {
                        p.PositionF + positionalOffset, 
                        Color.White, 
                        particleSystem, 
                        0, 
                        p.maxEnergy + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * mutationAmount : 0), 
                        p.energyIntake + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * mutationAmount : 0),
                        p.energyOutput + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * mutationAmount : 0),
                        p.lengthMultiplier + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * mutationAmount : 0),
                        (r.NextSingle() > (mutationRate + 2)/3) ? !sticky : sticky,
                        mutationRate + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * mutationAmount : 0),
                        Math.Clamp(mutationAmount + ((r.NextSingle() > mutationRate) ? (r.NextSingle() - 0.5f) * Math.Clamp(mutationAmount, -0.1, 0.1) : 0), -1, 1)
                    };
                    cellArgs.Add(pArgs);
                }
                
                firstCellEnergy = (energy - energyDrain)/2;
                energyDrain += firstCellEnergy;
            }
            
            
            object[] args = new object[]{cellTypes, cellArgs};
            object[] argsFinal = new object[args.Length + cellArgs.Count];
            args.CopyTo(argsFinal, 0);
            cellArgs[0].CopyTo(argsFinal, args.Length);
            var c = particleSystem.AddLivingCell(typeof(StemCell), argsFinal);
            c.energy = firstCellEnergy;
            energy -= energyDrain;
        }
    }
}

public class BrainCell
{
    
}

public static class ParticleLivingExtentions
{
    public static LivingCell AddLivingCell(this ParticleSystem particleSystem, Type cellType, object[] args)
    {
        return (Activator.CreateInstance(cellType, args) as LivingCell) ?? throw new Exception("Could not create " + cellType.Name);
    }
}