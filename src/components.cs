// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2023 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
    public interface IEcsPool {
        void Resize (int capacity);
        bool Has (int entity);
        void Del (int entity);
        void AddRaw (int entity, object dataRaw);
        object GetRaw (int entity);
        void SetRaw (int entity, object dataRaw);
        int GetId ();
        Type GetComponentType ();
        void Copy (int srcEntity, int dstEntity);
    }

    public interface IEcsAutoReset<T> where T : struct {
        void AutoReset (ref T c);
    }

    public interface IEcsAutoCopy<T> where T : struct {
        void AutoCopy (ref T src, ref T dst);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsPool<T> : IEcsPool where T : struct {
        readonly Type _type;
        readonly EcsWorld _world;
        readonly short _id;
        readonly AutoResetHandler _autoResetHandler;
        readonly AutoCopyHandler _autoCopyHandler;
        // 1-based index.
        T[] _denseItems;
        int[] _sparseItems;
        int _denseItemsCount;
        int[] _recycledItems;
        int _recycledItemsCount;
#if ENABLE_IL2CPP && !UNITY_EDITOR
        T _fakeInstance;
#endif

        internal EcsPool (EcsWorld world, short id, int denseCapacity, int sparseCapacity, int recycledCapacity) {
            _type = typeof (T);
            _world = world;
            _id = id;
            _denseItems = new T[denseCapacity + 1];
            _sparseItems = new int[sparseCapacity];
            _denseItemsCount = 1;
            _recycledItems = new int[recycledCapacity];
            _recycledItemsCount = 0;
            // autoreset init.
            var isAutoReset = typeof (IEcsAutoReset<T>).IsAssignableFrom (_type);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!isAutoReset && _type.GetInterface ("IEcsAutoReset`1") != null) {
                throw new Exception ($"IEcsAutoReset should have <{typeof (T).Name}> constraint for component \"{typeof (T).Name}\".");
            }
#endif
            if (isAutoReset) {
                var autoResetMethod = typeof (T).GetMethod (nameof (IEcsAutoReset<T>.AutoReset));
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                if (autoResetMethod == null) {
                    throw new Exception (
                        $"IEcsAutoReset<{typeof (T).Name}> explicit implementation not supported, use implicit instead.");
                }
#endif
                _autoResetHandler = (AutoResetHandler) Delegate.CreateDelegate (
                    typeof (AutoResetHandler),
#if ENABLE_IL2CPP && !UNITY_EDITOR
                    _fakeInstance,
#else
                    null,
#endif
                    autoResetMethod);
            }
            // autocopy init.
            var isAutoCopy = typeof (IEcsAutoCopy<T>).IsAssignableFrom (_type);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!isAutoCopy && _type.GetInterface ("IEcsCopy`1") != null) {
                throw new Exception ($"IEcsCopy should have <{typeof (T).Name}> constraint for component \"{typeof (T).Name}\".");
            }
#endif
            if (isAutoCopy) {
                var copyMethod = typeof (T).GetMethod (nameof (IEcsAutoCopy<T>.AutoCopy));
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                if (copyMethod == null) {
                    throw new Exception (
                        $"IEcsCopy<{typeof (T).Name}> explicit implementation not supported, use implicit instead.");
                }
#endif
                _autoCopyHandler = (AutoCopyHandler) Delegate.CreateDelegate (
                    typeof (AutoCopyHandler),
#if ENABLE_IL2CPP && !UNITY_EDITOR
                    _fakeInstance,
#else
                    null,
#endif
                    copyMethod);
            }
        }

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        void ReflectionSupportHack () {
            _world.GetPool<T> ();
            _world.Filter<T> ().Exc<T> ().End ();
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorld () {
            return _world;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetId () {
            return _id;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Type GetComponentType () {
            return _type;
        }

        void IEcsPool.Resize (int capacity) {
            Array.Resize (ref _sparseItems, capacity);
        }

        object IEcsPool.GetRaw (int entity) {
            return Get (entity);
        }

        void IEcsPool.SetRaw (int entity, object dataRaw) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (dataRaw == null || dataRaw.GetType () != _type) { throw new Exception ($"Invalid component data, valid \"{typeof (T).Name}\" instance required."); }
            if (_sparseItems[entity] <= 0) { throw new Exception ($"Component \"{typeof (T).Name}\" not attached to entity."); }
#endif
            _denseItems[_sparseItems[entity]] = (T) dataRaw;
        }

        void IEcsPool.AddRaw (int entity, object dataRaw) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (dataRaw == null || dataRaw.GetType () != _type) { throw new Exception ($"Invalid component data, valid \"{typeof (T).Name}\" instance required."); }
#endif
            ref var data = ref Add (entity);
            data = (T) dataRaw;
        }

        public T[] GetRawDenseItems () {
            return _denseItems;
        }

        public ref int GetRawDenseItemsCount () {
            return ref _denseItemsCount;
        }

        public int[] GetRawSparseItems () {
            return _sparseItems;
        }

        public int[] GetRawRecycledItems () {
            return _recycledItems;
        }

        public ref int GetRawRecycledItemsCount () {
            return ref _recycledItemsCount;
        }

        public ref T Add (int entity) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_world.IsEntityAliveInternal (entity)) { throw new Exception ("Cant touch destroyed entity."); }
            if (_sparseItems[entity] > 0) { throw new Exception ($"Component \"{typeof (T).Name}\" already attached to entity."); }
#endif
            int idx;
            if (_recycledItemsCount > 0) {
                idx = _recycledItems[--_recycledItemsCount];
            } else {
                idx = _denseItemsCount;
                if (_denseItemsCount == _denseItems.Length) {
                    Array.Resize (ref _denseItems, _denseItemsCount << 1);
                }
                _denseItemsCount++;
                _autoResetHandler?.Invoke (ref _denseItems[idx]);
            }
            _sparseItems[entity] = idx;
            _world.OnEntityChangeInternal (entity, _id, true);
            _world.AddComponentToRawEntityInternal (entity, _id);
#if DEBUG || LEOECSLITE_WORLD_EVENTS
            _world.RaiseEntityChangeEvent (entity, _id, true);
#endif
            return ref _denseItems[idx];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Get (int entity) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_world.IsEntityAliveInternal (entity)) { throw new Exception ("Cant touch destroyed entity."); }
            if (_sparseItems[entity] == 0) { throw new Exception ($"Cant get \"{typeof (T).Name}\" component - not attached."); }
#endif
            return ref _denseItems[_sparseItems[entity]];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Has (int entity) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_world.IsEntityAliveInternal (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            return _sparseItems[entity] > 0;
        }

        public void Del (int entity) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_world.IsEntityAliveInternal (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            ref var sparseData = ref _sparseItems[entity];
            if (sparseData > 0) {
                _world.OnEntityChangeInternal (entity, _id, false);
                if (_recycledItemsCount == _recycledItems.Length) {
                    Array.Resize (ref _recycledItems, _recycledItemsCount << 1);
                }
                _recycledItems[_recycledItemsCount++] = sparseData;
                if (_autoResetHandler != null) {
                    _autoResetHandler.Invoke (ref _denseItems[sparseData]);
                } else {
                    _denseItems[sparseData] = default;
                }
                sparseData = 0;
                var componentsCount = _world.RemoveComponentFromRawEntityInternal (entity, _id);
#if DEBUG || LEOECSLITE_WORLD_EVENTS
                _world.RaiseEntityChangeEvent (entity, _id, false);
#endif
                if (componentsCount == 0) {
                    _world.DelEntity (entity);
                }
            }
        }

        public void Copy (int srcEntity, int dstEntity) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_world.IsEntityAliveInternal (srcEntity)) { throw new Exception ("Cant touch destroyed src-entity."); }
            if (!_world.IsEntityAliveInternal (dstEntity)) { throw new Exception ("Cant touch destroyed dest-entity."); }
#endif
            if (Has (srcEntity)) {
                ref var srcData = ref Get (srcEntity);
                if (!Has (dstEntity)) {
                    Add (dstEntity);
                }
                ref var dstData = ref Get (dstEntity);
                if (_autoCopyHandler != null) {
                    _autoCopyHandler.Invoke (ref srcData, ref dstData);
                } else {
                    dstData = srcData;
                }
            }
        }

        delegate void AutoResetHandler (ref T component);

        delegate void AutoCopyHandler (ref T srcComponent, ref T dstComponent);
    }
}
