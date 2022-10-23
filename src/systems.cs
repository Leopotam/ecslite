// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

#nullable enable

namespace Leopotam.EcsLite
{
    internal static class EcsTypeExtensions
    {
        // Code originally written by Brian Rogers in StackOverflow
        // Source: https://stackoverflow.com/questions/17480990/
        internal static string TypeName (this Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            StringBuilder sb = new StringBuilder ();
            sb.Append (type.Name).Append ('<');
            bool appendComma = false;
            foreach (Type t in type.GetGenericArguments ()) {
                sb.Append (appendComma ? "," : "").Append (TypeName(t));
                appendComma = true;
            }
            sb.Append ('>');
            return sb.ToString ();
        }
    }

    internal class KeyedSystem {
        internal string Key { get; init; }
        internal IEcsSystem System { get; init; }
    }

    public interface IEcsSystem { }

    public interface IEcsPreInitSystem : IEcsSystem {
        void PreInit (IEcsSystems systems);
    }

    public interface IEcsInitSystem : IEcsSystem {
        void Init (IEcsSystems systems);
    }

    public interface IEcsRunSystem : IEcsSystem {
        void Run (IEcsSystems systems);
    }

    public interface IEcsDestroySystem : IEcsSystem {
        void Destroy (IEcsSystems systems);
    }

    public interface IEcsPostDestroySystem : IEcsSystem {
        void PostDestroy (IEcsSystems systems);
    }

    public interface IEcsSystems {
        T GetShared<T> () where T : class;
        IEcsSystems AddWorld (EcsWorld world, string name);
        EcsWorld GetWorld (string name = null);
        Dictionary<string, EcsWorld> GetAllNamedWorlds ();
        IEcsSystems Add<T> (T system) where T : IEcsSystem;
        List<IEcsSystem> GetAllSystems ();
        T? GetSystem<T> () where T : IEcsSystem;
        void Init ();
        void Run ();
        void Destroy ();
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class EcsSystems : IEcsSystems {
        readonly EcsWorld _defaultWorld;
        readonly Dictionary<string, EcsWorld> _worlds;
        readonly List<KeyedSystem> _allSystems;
        readonly List<KeyedSystem> _runSystems;
        readonly object _shared;
#if DEBUG
        bool _inited;
#endif

        public EcsSystems (EcsWorld defaultWorld, object shared = null) {
            _defaultWorld = defaultWorld;
            _shared = shared;
            _worlds = new Dictionary<string, EcsWorld> (8);
            _allSystems = new List<KeyedSystem> (128);
            _runSystems = new List<KeyedSystem> (128);
        }

        public virtual T GetShared<T> () where T : class {
            return _shared as T;
        }

        public virtual IEcsSystems AddWorld (EcsWorld world, string name) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("Cant add world after initialization."); }
            if (world == null) { throw new System.Exception ("World cant be null."); }
            if (string.IsNullOrEmpty (name)) { throw new System.Exception ("World name cant be null or empty."); }
            if (_worlds.ContainsKey (name)) { throw new System.Exception ($"World with name \"{name}\" already added."); }
#endif
            _worlds[name] = world;
            return this;
        }

        public virtual EcsWorld GetWorld (string name = null) {
            if (name == null) {
                return _defaultWorld;
            }
            _worlds.TryGetValue (name, out var world);
            return world;
        }

        public virtual Dictionary<string, EcsWorld> GetAllNamedWorlds () {
            return _worlds;
        }

        public virtual IEcsSystems Add<T> (T system) where T : IEcsSystem {
            string key = typeof (T).TypeName ();

#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("Cant add system after initialization."); }
            if (_allSystems.Exists (value => value.Key == key)) { throw new System.Exception ("Cant add systems with identical keys."); }
#endif
            _allSystems.Add (new KeyedSystem () { Key = key, System = system });
            if (system is IEcsRunSystem runSystem) {
                // No need to check for keys here, both system lists are manipulated concurrently.
                _runSystems.Add (new KeyedSystem() { Key = key, System = runSystem });
            }
            return this;
        }

        public virtual List<IEcsSystem> GetAllSystems() {
            List<IEcsSystem> systems = new List<IEcsSystem>(_allSystems.ConvertAll(value => value.System));
            return systems;
        }

        // List.Find () returns null if the value is not found. The existence of the key is checked in debug
        // builds, but it is not guaranteed in release builds. Instead of throwing an exception, we'll notify
        // the user of the possibility of a not-valid system to be returned, and allow for flexibility of the
        // response on their side.
        //
        // Usage: var system = systems.GetSystem<ExampleSystem>();
        public virtual T? GetSystem<T> () where T : IEcsSystem {
            string key = typeof (T).TypeName ();
            KeyedSystem? system = _allSystems.Find (value => value.Key == key);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (system == null) { throw new System.Exception ("System does not exist in the current context."); }
#endif
            return (T?)system.System;
        }

        public virtual void Init () {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("Already initialized."); }
#endif
            foreach (var sysWrapper in _allSystems) {
                if (sysWrapper.System is IEcsPreInitSystem initSystem) {
                    initSystem.PreInit (this);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {initSystem.GetType ().Name}.PreInit()."); }
#endif
                }
            }
            foreach (var sysWrapper in _allSystems) {
                if (sysWrapper.System is IEcsInitSystem initSystem) {
                    initSystem.Init (this);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {initSystem.GetType ().Name}.Init()."); }
#endif
                }
            }
#if DEBUG
            _inited = true;
#endif
        }

        public virtual void Run () {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_inited) { throw new System.Exception ("Cant run without initialization."); }
#endif
            for (int i = 0, iMax = _runSystems.Count; i < iMax; i++) {
                ((IEcsRunSystem)(_runSystems[i].System)).Run (this);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                var worldName = CheckForLeakedEntities (this);
                if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {_runSystems[i].GetType ().Name}.Run()."); }
#endif
            }
        }

        public virtual void Destroy () {
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                if (_allSystems[i].System is IEcsDestroySystem destroySystem) {
                    destroySystem.Destroy (this);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {destroySystem.GetType ().Name}.Destroy()."); }
#endif
                }
            }
            for (var i = _allSystems.Count - 1; i >= 0; i--) {
                if (_allSystems[i].System is IEcsPostDestroySystem postDestroySystem) {
                    postDestroySystem.PostDestroy (this);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {postDestroySystem.GetType ().Name}.PostDestroy()."); }
#endif
                }
            }
            _worlds.Clear ();
            _allSystems.Clear ();
            _runSystems.Clear ();
#if DEBUG
            _inited = false;
#endif
        }

#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
        public static string CheckForLeakedEntities (IEcsSystems systems) {
            if (systems.GetWorld ().CheckForLeakedEntities ()) { return "default"; }
            foreach (var pair in systems.GetAllNamedWorlds ()) {
                if (pair.Value.CheckForLeakedEntities ()) {
                    return pair.Key;
                }
            }
            return null;
        }
#endif
    }
}