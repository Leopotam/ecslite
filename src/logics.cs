// ----------------------------------------------------------------------------
// The Proprietary or MIT-Red License
// Copyright (c) 2012-2022 Leopotam <leopotam@yandex.ru>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
    public interface IEcsLogic { }

    public interface IEcsPreInitLogic : IEcsLogic {
        void PreInit ();
    }

    public interface IEcsInitLogic : IEcsLogic {
        void Init ();
    }

    public interface IEcsRunLogic : IEcsLogic {
        void Run ();
    }

    public interface IEcsDestroyLogic : IEcsLogic {
        void Destroy ();
    }

    public interface IEcsPostDestroyLogic : IEcsLogic {
        void PostDestroy ();
    }

    public interface IEcsLogics {
        EcsWorld GetWorld ();
        IEcsLogics AddNamedWorld (EcsWorld world, string name);
        IReadOnlyDictionary<string, EcsWorld> GetNamedWorlds ();
        IEcsLogics Add (IEcsLogic logic);
        IReadOnlyList<IEcsLogic> GetLogics ();
        void Init ();
        void Run ();
        void Destroy ();
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public class EcsLogics : IEcsLogics {
        readonly EcsWorld _world;
        readonly Dictionary<string, EcsWorld> _namedWorlds;
        readonly List<IEcsLogic> _allLogics;
        readonly List<IEcsRunLogic> _runLogics;
#if DEBUG
        bool _inited;
#endif

        public EcsLogics (EcsWorld world) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (world == null) { throw new System.Exception ("World cant be null."); }
#endif
            _world = world;
            _namedWorlds = new Dictionary<string, EcsWorld> (32);
            _allLogics = new List<IEcsLogic> (128);
            _runLogics = new List<IEcsRunLogic> (128);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorld () {
            return _world;
        }

        public IEcsLogics AddNamedWorld (EcsWorld world, string name) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("World cant be added after Init() call."); }
#endif
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (world == null) { throw new System.Exception ("World cant be null."); }
            if (string.IsNullOrEmpty (name)) { throw new System.Exception ("World name cant be null or empty."); }
#endif
            _namedWorlds[name] = world;
            return this;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public IReadOnlyDictionary<string, EcsWorld> GetNamedWorlds () {
            return _namedWorlds;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public IEcsLogics Add (IEcsLogic logic) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("Logic cant be added after Init() call."); }
            if (logic == null) { throw new System.Exception ("World cant be null."); }
#endif
            _allLogics.Add (logic);
            if (logic is IEcsRunLogic run) {
                _runLogics.Add (run);
            }
            return this;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<IEcsLogic> GetLogics () {
            return _allLogics;
        }

        public void Init () {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (_inited) { throw new System.Exception ("Already initialized."); }
#endif
            foreach (var ecsLogic in _allLogics) {
                if (ecsLogic is IEcsPreInitLogic logic) {
                    logic.PreInit ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {logic.GetType ().Name}.PreInit()."); }
#endif
                }
            }
            foreach (var ecsLogic in _allLogics) {
                if (ecsLogic is IEcsInitLogic logic) {
                    logic.Init ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {logic.GetType ().Name}.Init()."); }
#endif
                }
            }
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            _inited = true;
#endif
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Run () {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (!_inited) { throw new System.Exception ("Should be initialized before."); }
#endif
            for (int i = 0, iMax = _runLogics.Count; i < iMax; i++) {
                _runLogics[i].Run ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                var worldName = CheckForLeakedEntities (this);
                if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {_runLogics[i].GetType ().Name}.Run()."); }
#endif
            }
        }

        public void Destroy () {
            for (var i = _allLogics.Count - 1; i >= 0; i--) {
                if (_allLogics[i] is IEcsDestroyLogic logic) {
                    logic.Destroy ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {logic.GetType ().Name}.Destroy()."); }
#endif
                }
            }
            for (var i = _allLogics.Count - 1; i >= 0; i--) {
                if (_allLogics[i] is IEcsPostDestroyLogic logic) {
                    logic.PostDestroy ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = CheckForLeakedEntities (this);
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {logic.GetType ().Name}.PostDestroy()."); }
#endif
                }
            }
            _allLogics.Clear ();
            _runLogics.Clear ();
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            _inited = false;
#endif
        }

#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
        public static string CheckForLeakedEntities (IEcsLogics logics) {
            if (logics.GetWorld ().CheckForLeakedEntities ()) { return "default"; }
            foreach (var pair in logics.GetNamedWorlds ()) {
                if (pair.Value.CheckForLeakedEntities ()) {
                    return pair.Key;
                }
            }
            return null;
        }
#endif
    }
}