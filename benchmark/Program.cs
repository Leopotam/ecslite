using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Leopotam.EcsLite;

[MemoryDiagnoser]
public class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<Program>();
    }

    private static int iterations = 1000;
    private static int entities = 1000;

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public void MultiEntitySwitcherLeopotam()
    {
        var w = new EcsWorld();
        var s = new EcsSystems(w);
        s.Add(new AllEntityComponentSwitcherSystem1());
        s.Add(new AllEntityComponentSwitcherSystem2());

        for (var i = 0; i < entities; i++)
        {
            var e = w.NewEntity();
            var pool = w.GetPool<ComponentLeo1>();
            ref ComponentLeo1 c1 = ref pool.Add(e);
        }
        s.Init();

        for (var i = 0; i < iterations / 10; i++)
        {
            s.Run();
        }

        s.Destroy();
    }

    internal class AllEntityComponentSwitcherSystem1 : IEcsSystem, IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            var filter1 = world.Filter<ComponentLeo1>().End();
            var comps1 = world.GetPool<ComponentLeo1>();
            var comps2 = world.GetPool<ComponentLeo2>();

            foreach (var entity in filter1)
            {
                comps2.Add(entity);
                comps1.Del(entity);
            }
        }
    }

    internal class AllEntityComponentSwitcherSystem2 : IEcsSystem, IEcsRunSystem
    {
        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            var filter2 = world.Filter<ComponentLeo2>().End();
            var comps1 = world.GetPool<ComponentLeo1>();
            var comps2 = world.GetPool<ComponentLeo2>();

            foreach (var entity in filter2)
            {
                comps1.Add(entity);
                comps2.Del(entity);
            }
        }
    }

    internal struct ComponentLeo1
    {
        public int Id;
    }

    internal struct ComponentLeo2
    {
    }

}