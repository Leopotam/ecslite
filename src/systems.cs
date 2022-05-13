// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace EcsKit {
    public interface IEcsSystem { }

    public interface IEcsInitSystem : IEcsSystem {
        void Init (EcsWorld world);
    }

    public interface IEcsRunSystem : IEcsSystem {
        void Run (EcsWorld world);
    }

    public interface IEcsDestroySystem : IEcsSystem {
        void Destroy (EcsWorld world);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsSystems {
        readonly EcsWorld _world;
        readonly List<IEcsSystem> _allSystems;
        readonly List<IEcsRunSystem> _runSystems;

        public EcsSystems (EcsWorld world) {
            _world = world;
            _allSystems = new List<IEcsSystem> (128);
            _runSystems = new List<IEcsRunSystem> (128);
        }

        public int GetAllSystems (ref IEcsSystem[] list) {
            var itemsCount = _allSystems.Count;
            if (itemsCount == 0) { return 0; }
            if (list == null || list.Length < itemsCount) {
                list = new IEcsSystem[itemsCount];
            }
            for (int i = 0, iMax = itemsCount; i < iMax; i++) {
                list[i] = _allSystems[i];
            }
            return itemsCount;
        }

        public int GetRunSystems (ref IEcsRunSystem[] list) {
            var itemsCount = _runSystems.Count;
            if (itemsCount == 0) { return 0; }
            if (list == null || list.Length < itemsCount) {
                list = new IEcsRunSystem[itemsCount];
            }
            for (int i = 0, iMax = itemsCount; i < iMax; i++) {
                list[i] = _runSystems[i];
            }
            return itemsCount;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorld () {
            return _world;
        }

        public void Destroy () {
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                if (_allSystems[i] is IEcsDestroySystem destroySystem) {
                    destroySystem.Destroy (_world);
                }
            }
            _runSystems.Clear ();
            _allSystems.Clear ();
        }

        public EcsSystems Add (IEcsSystem system) {
            _allSystems.Add (system);
            if (system is IEcsRunSystem runSystem) {
                _runSystems.Add (runSystem);
            }
            return this;
        }

        public void Init () {
            for (int i = 0; i < _allSystems.Count; i++) {
                if (_allSystems[i] is IEcsInitSystem initSystem) {
                    initSystem.Init (_world);
                }
            }
        }

        public void Run () {
            for (int i = 0; i < _runSystems.Count; i++) {
                _runSystems[i].Run (_world);
            }
        }
    }
}