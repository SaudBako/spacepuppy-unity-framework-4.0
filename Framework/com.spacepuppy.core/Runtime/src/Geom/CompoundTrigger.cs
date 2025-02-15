﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy.Collections;
using com.spacepuppy.Events;
using com.spacepuppy.Utils;

namespace com.spacepuppy.Geom
{

    public interface ICompoundTriggerEnterResponder
    {
        void OnCompoundTriggerEnter(Collider other);
    }

    public interface ICompoundTriggerExitResponder
    {
        void OnCompoundTriggerExit(Collider other);
    }

    public class CompoundTrigger : SPComponent
    {

        #region Fields

        private Dictionary<Collider, CompoundTriggerMember> _colliders = new Dictionary<Collider, CompoundTriggerMember>();
        private HashSet<Collider> _active = new HashSet<Collider>();

        #endregion

        #region CONSTRUCTOR

        protected override void Start()
        {
            base.Start();

            this.SyncTriggers();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            this.PurgeActiveState();
        }

        #endregion

        #region Methods

        public void SyncTriggers()
        {
            using (var lst = TempCollection.GetList<Collider>())
            {
                this.GetComponentsInChildren<Collider>(true, lst);

                //purge entries if necessary
                if (_colliders.Count > 0)
                {
                    using (var purge = TempCollection.GetList<Collider>())
                    {
                        var ed = _colliders.GetEnumerator();
                        while (ed.MoveNext())
                        {
                            if (!ObjUtil.IsObjectAlive(ed.Current.Key) || ed.Current.Value == null || !lst.Contains(ed.Current.Key))
                            {
                                purge.Add(ed.Current.Key);
                                ObjUtil.SmartDestroy(ed.Current.Value);
                            }
                        }
                        if (purge.Count > 0)
                        {
                            var e = purge.GetEnumerator();
                            while (e.MoveNext())
                            {
                                _colliders.Remove(e.Current);
                            }
                        }
                    }
                }

                //fill unknowns
                if (lst.Count > 0)
                {
                    var e = lst.GetEnumerator();
                    while (e.MoveNext())
                    {
                        if (!_colliders.ContainsKey(e.Current))
                        {
                            var m = e.Current.AddComponent<CompoundTriggerMember>();
                            m.Init(this, e.Current);
                            _colliders.Add(e.Current, m);
                        }
                    }
                }
            }
        }

        public Collider[] GetActiveColliders()
        {
            return _active.Count > 0 ? _active.ToArray() : ArrayUtil.Empty<Collider>();
        }

        public int GetActiveColliders(ICollection<Collider> output)
        {
            int cnt = _active.Count;
            if (cnt == 0) return 0;

            var e = _active.GetEnumerator();
            while (e.MoveNext())
            {
                output.Add(e.Current);
            }
            return cnt;
        }

        /// <summary>
        /// Dumps state of what member colliders are intersected currently. 
        /// Caution using this, any member collider currently intersected will not refire OnTriggerEnter. 
        /// This is usually reserved for OnDisable to revert state.
        /// </summary>
        protected void PurgeActiveState()
        {
            _active.Clear();
            var e = _colliders.GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.Value.Active.Clear();
            }
        }

        private void SignalTriggerEnter(CompoundTriggerMember member, Collider other)
        {
            if (_active.Add(other))
            {
                this.OnCompoundTriggerEnter(other);
            }
        }

        private void SignalTriggerExit(CompoundTriggerMember member, Collider other)
        {
            var e = _colliders.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Value.Active.Contains(other)) return;
            }

            if (_active.Remove(other))
            {
                this.OnCompoundTriggerExit(other);
            }
        }

        protected virtual void OnCompoundTriggerEnter(Collider other)
        {
            Messaging.Signal(this.gameObject, other, OnEnterFunctor);
        }

        protected virtual void OnCompoundTriggerExit(Collider other)
        {
            Messaging.Signal(this.gameObject, other, OnExitFunctor);
        }

        #endregion


        #region Special Types

        private class CompoundTriggerMember : MonoBehaviour
        {

            [System.NonSerialized]
            private CompoundTrigger _owner;
            [System.NonSerialized]
            private Collider _collider;
            [System.NonSerialized]
            private HashSet<Collider> _active;

            internal CompoundTrigger Owner
            {
                get { return _owner; }
            }

            internal Collider Collider
            {
                get { return _collider; }
            }

            internal HashSet<Collider> Active
            {
                get { return _active; }
            }

            internal void Init(CompoundTrigger owner, Collider collider)
            {
                _owner = owner;
                _collider = collider;
                _active = new HashSet<Collider>();
            }

            private void OnTriggerEnter(Collider other)
            {
                if (_active.Add(other))
                {
                    if (_owner != null) _owner.SignalTriggerEnter(this, other);
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (_active.Remove(other))
                {
                    if (_owner != null) _owner.SignalTriggerExit(this, other);
                }
            }

        }

        #endregion

        public static readonly System.Action<ICompoundTriggerEnterResponder, Collider> OnEnterFunctor = (x, y) => x.OnCompoundTriggerEnter(y);
        public static readonly System.Action<ICompoundTriggerExitResponder, Collider> OnExitFunctor = (x, y) => x.OnCompoundTriggerExit(y);
    }

}
