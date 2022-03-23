// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
#if LEOECSLITE_FILTER_EVENTS
    public interface IEcsFilterEventListener {
        void OnEntityAdded (int entity);
        void OnEntityRemoved (int entity);
    }
#endif
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsFilter {
        readonly EcsWorld _world;
        readonly EcsWorld.Mask _mask;
        int[] _denseEntities;
        int _entitiesCount;
        internal int[] SparseEntities;
        int _lockCount;
        DelayedOp[] _delayedOps;
        int _delayedOpsCount;
#if LEOECSLITE_FILTER_EVENTS
        IEcsFilterEventListener[] _eventListeners = new IEcsFilterEventListener[4];
        int _eventListenersCount;
#endif

        internal EcsFilter (EcsWorld world, EcsWorld.Mask mask, int denseCapacity, int sparseCapacity) {
            _world = world;
            _mask = mask;
            _denseEntities = new int[denseCapacity];
            SparseEntities = new int[sparseCapacity];
            _entitiesCount = 0;
            _delayedOps = new DelayedOp[512];
            _delayedOpsCount = 0;
            _lockCount = 0;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorld () {
            return _world;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetEntitiesCount () {
            return _entitiesCount;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int[] GetRawEntities () {
            return _denseEntities;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int[] GetSparseIndex () {
            return SparseEntities;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator () {
            _lockCount++;
            return new Enumerator (this);
        }

#if LEOECSLITE_FILTER_EVENTS
        public void AddEventListener (IEcsFilterEventListener eventListener) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (eventListener == null) { throw new Exception ("Listener is null."); }
#endif
            if (_eventListeners.Length == _eventListenersCount) {
                Array.Resize (ref _eventListeners, _eventListenersCount << 1);
            }
            _eventListeners[_eventListenersCount++] = eventListener;
        }

        public void RemoveEventListener (IEcsFilterEventListener eventListener) {
            for (var i = 0; i < _eventListenersCount; i++) {
                if (_eventListeners[i] == eventListener) {
                    _eventListenersCount--;
                    // cant fill gap with last element due listeners order is important.
                    Array.Copy (_eventListeners, i + 1, _eventListeners, i, _eventListenersCount - i);
                    break;
                }
            }
        }
#endif

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void ResizeSparseIndex (int capacity) {
            Array.Resize (ref SparseEntities, capacity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal EcsWorld.Mask GetMask () {
            return _mask;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void AddEntity (int entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (_entitiesCount == _denseEntities.Length) {
                Array.Resize (ref _denseEntities, _entitiesCount << 1);
            }
            _denseEntities[_entitiesCount++] = entity;
            SparseEntities[entity] = _entitiesCount;
#if LEOECSLITE_FILTER_EVENTS
            ProcessEventListeners (true, entity);
#endif
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void RemoveEntity (int entity) {
            if (AddDelayedOp (false, entity)) { return; }
            var idx = SparseEntities[entity] - 1;
            SparseEntities[entity] = 0;
            _entitiesCount--;
            if (idx < _entitiesCount) {
                _denseEntities[idx] = _denseEntities[_entitiesCount];
                SparseEntities[_denseEntities[idx]] = idx + 1;
            }
#if LEOECSLITE_FILTER_EVENTS
            ProcessEventListeners (false, entity);
#endif
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        bool AddDelayedOp (bool added, int entity) {
            if (_lockCount <= 0) { return false; }
            if (_delayedOpsCount == _delayedOps.Length) {
                Array.Resize (ref _delayedOps, _delayedOpsCount << 1);
            }
            ref var op = ref _delayedOps[_delayedOpsCount++];
            op.Added = added;
            op.Entity = entity;
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Unlock () {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_lockCount <= 0) { throw new Exception ($"Invalid lock-unlock balance for \"{GetType ().Name}\"."); }
#endif
            _lockCount--;
            if (_lockCount == 0 && _delayedOpsCount > 0) {
                for (int i = 0, iMax = _delayedOpsCount; i < iMax; i++) {
                    ref var op = ref _delayedOps[i];
                    if (op.Added) {
                        AddEntity (op.Entity);
                    } else {
                        RemoveEntity (op.Entity);
                    }
                }
                _delayedOpsCount = 0;
            }
        }

#if LEOECSLITE_FILTER_EVENTS
        void ProcessEventListeners (bool isAdd, int entity) {
            if (isAdd) {
                for (var i = 0; i < _eventListenersCount; i++) {
                    _eventListeners[i].OnEntityAdded (entity);
                }
            } else {
                for (var i = 0; i < _eventListenersCount; i++) {
                    _eventListeners[i].OnEntityRemoved (entity);
                }
            }
        }
#endif

        public struct Enumerator : IDisposable {
            readonly EcsFilter _filter;
            readonly int[] _entities;
            readonly int _count;
            int _idx;

            public Enumerator (EcsFilter filter) {
                _filter = filter;
                _entities = filter._denseEntities;
                _count = filter._entitiesCount;
                _idx = -1;
            }

            public int Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get => _entities[_idx];
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                return ++_idx < _count;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public void Dispose () {
                _filter.Unlock ();
            }
        }

        struct DelayedOp {
            public bool Added;
            public int Entity;
        }
    }
}