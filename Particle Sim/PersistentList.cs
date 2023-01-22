namespace ParticlePhysics.Internal;

public class PersistentList<T>
{
    protected T[] items;
    protected int[] active;
    protected Queue<int> reusableIDs = new Queue<int>();
    protected int nextID;
    protected int maxCapacity;
    protected bool reuseOldAtMaxCapacity;
    protected bool buildActiveList;

    public PersistentList(int startingCapacity, int maxCapacity, bool reuseOldAtMaxCapacity = false, bool buildActiveList = false)
    {
        if(typeof(T).IsAssignableFrom(typeof(IDestroyable)))
        {
            throw new Exception("PersistentListClass can not be used with IDestroyable objects! Please use PersistentListDestroyable instead");
        }
        else if(typeof(T).IsAssignableFrom(typeof(IHasID)))
        {
            throw new Exception("PersistentListClass can not be used with IHasID objects! Please use PersistentListID instead");
        }

        items = new T[startingCapacity];
        if(buildActiveList)
            active = new int[startingCapacity];
        this.maxCapacity = maxCapacity;
        this.reuseOldAtMaxCapacity = reuseOldAtMaxCapacity;
        nextID = 0;
        this.buildActiveList = buildActiveList;
    }

    public T this[int id]
    {
        get => items[id];
        set => items[id] = value;
    }

    public int Count => items.Length;

    public virtual int Add(T item)
    {
        int newID;
        if (reusableIDs.Count > 0)
        {
            newID = reusableIDs.Dequeue();
        }
        else 
        {
            if (nextID >= items.Length)
            {
                if(items.Length < maxCapacity)
                {
                    Array.Resize(ref items, (int)MathF.Min(items.Length * 2, maxCapacity));
                    if(buildActiveList)
                        Array.Resize(ref active, (int)MathF.Min(items.Length * 2, maxCapacity));
                }
                else if(reuseOldAtMaxCapacity)
                {
                    nextID = 0;
                }
                else
                {
                    throw new Exception("Too many items in PersistentList");
                }
            }
            
            newID = nextID;
            nextID++;
        }

        items[newID] = item;
        
        if(buildActiveList)
            active[newID] = 1;
        return newID;
    }

    public void Remove(int id)
    {
        reusableIDs.Enqueue(id);
        items[id] = default;
        if(buildActiveList)
            active[id] = 0;
    }

    public T[] ToArray()
    {
        return items;
    }

    public ref int[] GetActiveArray()
    {
        return ref active;
    }

    public void UpdateArray(T[] newItems)
    {
        items = newItems;
    }

    public ref T[] GetArray()
    {
        return ref items;
    }

    public void Clear()
    {
        Array.Clear(items, 0, items.Length);
        nextID = 0;
        reusableIDs.Clear();
    }
}

public class PersistentListID<T> : PersistentList<T> where T : IHasID
{
    public PersistentListID(int startingCapacity, int maxCapacity, bool reuseOldAtMaxCapacity = false, bool buildActiveList = false) : base(startingCapacity, maxCapacity, reuseOldAtMaxCapacity, buildActiveList)
    {
        if(typeof(T).IsAssignableFrom(typeof(IDestroyable)))
        {
            throw new Exception("PersistentListClass can not be used with IDestroyable objects! Please use PersistentListDestroyable instead");
        }
    }

    public override int Add(T item)
    {
        int newID = base.Add(item);
        item.ID = newID;
        return newID;
    }

    public void Remove(T item)
    {
        base.Remove(item.ID);
    }
}

public class PersistentListDestroyable<T> : PersistentList<T> where T : IHasID, IDestroyable
{
    public PersistentListDestroyable(int startingCapacity, int maxCapacity, bool reuseOldAtMaxCapacity = false, bool buildActiveList = false) : base(startingCapacity, maxCapacity, reuseOldAtMaxCapacity, buildActiveList)
    {
    }

    public override int Add(T item)
    {
        if(nextID < items.Length && reusableIDs.Count == 0 && items[nextID] != null)
        {
            items[nextID].Destroy();
            nextID++;
        }

        int newID;
        if (reusableIDs.Count > 0)
        {
            newID = reusableIDs.Dequeue();
        }
        else 
        {
            if (nextID >= items.Length)
            {
                if(items.Length < maxCapacity)
                {
                    Array.Resize(ref items, (int)MathF.Min(items.Length * 2, maxCapacity));
                    if(buildActiveList)
                        Array.Resize(ref active, (int)MathF.Min(items.Length * 2, maxCapacity));
                }
                else if(reuseOldAtMaxCapacity)
                {
                    nextID = 0;
                }
                else
                {
                    throw new Exception("Too many items in PersistentList");
                }
            }
            
            newID = nextID;
            nextID++;
        }

        items[newID] = item;
        if(buildActiveList)
            active[newID] = 1;
        item.ID = newID;
        return newID;
    }

    public void Remove(T item)
    {
        base.Remove(item.ID);
    }
}