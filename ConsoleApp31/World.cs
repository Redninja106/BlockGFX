using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal class World
{
    private readonly List<IGameComponent> components = new();
    private readonly Queue<IGameComponent> componentsToAdd = new();
    private readonly Queue<IGameComponent> componentsToRemove = new();

    public IList<IGameComponent> Components => components;

    public BlockChunkManager ChunkManager => components.OfType<BlockChunkManager>().Single();

    public World()
    {
    }
    
    public void Update(float dt)
    {
        foreach (var component in components)
        {
            component.Update(dt);
        }

        ClearQueues();
    }

    public void Render(Camera camera)
    {
        foreach (var component in components)
        {
            component.Render(camera);
        }
    }

    public void Remove(IGameComponent component)
    {
        componentsToRemove.Enqueue(component);
    }

    public void Add(IGameComponent component)
    {
        componentsToAdd.Enqueue(component);
    }

    public void ClearQueues()
    {
        while (componentsToRemove.Count > 0)
        {
            components.Remove(componentsToRemove.Dequeue());
        }

        while (componentsToAdd.Count > 0)
        {
            components.Add(componentsToAdd.Dequeue());
        }
    }
}
