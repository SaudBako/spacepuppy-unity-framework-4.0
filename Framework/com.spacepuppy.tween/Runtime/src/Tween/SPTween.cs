﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy.Collections;
using com.spacepuppy.Utils;

//make sure the entirity of SPTween in preserved since this lib heavily relies on reflection
[assembly: UnityEngine.Scripting.Preserve]

namespace com.spacepuppy.Tween
{

    public sealed class SPTween : ServiceComponent<SPTween>, IService
    {

        #region Singleton Interface

        private const string SPECIAL_NAME = "Spacepuppy.SPTween";
        private static SPTween _instance;

        #endregion

        #region Fields

        private TweenerCollection _runningTweens = new TweenerCollection();

        private static Dictionary<TokenPairing, Tweener> _autoKillDict = new Dictionary<TokenPairing, Tweener>(new TokenPairingComparer());

        #endregion

        #region CONSTRUCTOR

        public static void Init()
        {
            if (!object.ReferenceEquals(_instance, null))
            {
                if (ObjUtil.IsDestroyed(_instance))
                {
                    ObjUtil.SmartDestroy(_instance.gameObject);
                    _instance = null;
                }
                else
                {
                    return;
                }
            }

            _instance = Services.Create<SPTween>(true, SPECIAL_NAME);
        }

        protected override void OnValidAwake()
        {
            base.OnValidAwake();

            SceneManager.sceneUnloaded += this.OnSceneUnloaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SceneManager.sceneUnloaded -= this.OnSceneUnloaded;
        }

        #endregion

        #region Properties

        public int RunningTweenCount
        {
            get { return _runningTweens.Count; }
        }

        #endregion

        #region Methods

        internal static bool IsRunning(Tweener tween)
        {
            if (GameLoop.ApplicationClosing) return false;
            if (_instance == null) return false;
            return _instance._runningTweens.Contains(tween);
        }

        internal static void AddReference(Tweener tween)
        {
            if (GameLoop.ApplicationClosing) return;
            if (_instance == null) SPTween.Init();
            _instance._runningTweens.Add(tween);
        }

        internal static void RemoveReference(Tweener tween)
        {
            if (GameLoop.ApplicationClosing) return;
            if (_instance == null) return;
            _instance._runningTweens.Remove(tween);
        }


        public static bool IsActiveAutoKill(object id, object autoKillToken)
        {
            var token = new TokenPairing(id, autoKillToken);
            return _autoKillDict.ContainsKey(token);
        }

        /// <summary>
        /// Flag a Tweener that implements IAutoKillableTweener to be auto killed if another tween targeting the same object is played. 
        /// Until the Tweener is either killed, or finished, it will be eligible for being automatically 
        /// killed if another Tweener starts playing that tweens the same target object. Note that other tweener 
        /// must implement IAutoKillableTweener as well (though doesn't have to be flagged to AutoKill).
        /// </summary>
        /// <param name="tween"></param>
        public static void AutoKill(Tweener tween)
        {
            if (tween == null) throw new System.ArgumentNullException("tween");
            if (tween.Id == null) throw new System.ArgumentException("Can only register a Tweener with a valid 'Id' for autokill.");
            if (GameLoop.ApplicationClosing) return;
            if (!tween.IsPlaying) throw new System.ArgumentException("Can only register a playing Tweener for autokill.");
            if (_instance == null) SPTween.Init();

            var token = new TokenPairing(tween.Id, tween.AutoKillToken);

            Tweener old;
            if (_autoKillDict.TryGetValue(token, out old) && old != tween)
            {
                old.Kill();
            }
            _autoKillDict[token] = tween;
        }


        public static void KillAll()
        {
            if (GameLoop.ApplicationClosing) return;
            if (_instance == null) return;
            if (_instance._runningTweens.Count == 0) return;

            var e = _instance._runningTweens.GetEnumerator();
            while (e.MoveNext())
            {
                e.Current.SetKilled();
            }
            _instance._runningTweens.Clear();
        }
        public static void KillAll(object id)
        {
            if (GameLoop.ApplicationClosing) return;
            if (_instance == null) return;
            if (_instance._runningTweens.Count == 0) return;

            using (var lst = TempCollection.GetList<Tweener>())
            {
                var e = _instance._runningTweens.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.Id == id) lst.Add(e.Current);
                }

                var e2 = lst.GetEnumerator();
                while (e2.MoveNext())
                {
                    e2.Current.Kill();
                }
            }
        }
        public static void KillAll(object id, object token)
        {
            var tk = new TokenPairing(id, token);
            Tweener old;
            if (_autoKillDict.TryGetValue(tk, out old))
            {
                old.Kill();
                _autoKillDict.Remove(tk);
            }
        }

        /// <summary>
        /// Enumerate over all active tweeen and call a function with them as an arg. Return true to stop enumerating.
        /// </summary>
        /// <param name="pred"></param>
        public static Tweener Find(System.Func<Tweener, bool> pred)
        {
            if (pred == null) return null;
            if (GameLoop.ApplicationClosing) return null;
            if (_instance == null) return null;

            return _instance._runningTweens.Find(pred);
        }

        #endregion

        #region Update Methods

        private void Update()
        {
            this.DoUpdate(UpdateSequence.Update);

            //if(Application.isEditor)
            //{
            //    if (this.name.StartsWith(SPECIAL_NAME)) this.name = SPECIAL_NAME + " [ActiveTweens: " + _runningTweens.Count.ToString() + "]";
            //}
        }

        private void FixedUpdate()
        {
            this.DoUpdate(UpdateSequence.FixedUpdate);
        }

        private void LateUpdate()
        {
            this.DoUpdate(UpdateSequence.LateUpdate);
        }

        private void DoUpdate(UpdateSequence updateType)
        {
            _runningTweens.Lock();

            var e = _runningTweens.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.UpdateType == updateType)
                {
                    try
                    {
                        e.Current.Update();
                    }
                    catch
                    {
                        _runningTweens.StageRemove(e.Current);
                    }
                }
            }

            _runningTweens.Unlock();
        }

        #endregion

        #region Event Handlers

        private void OnSceneUnloaded(Scene scene)
        {
            _runningTweens.Lock();

            var e = _runningTweens.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.GetTargetIsDestroyed())
                {
                    _runningTweens.StageRemove(e.Current);
                }
            }

            _runningTweens.Unlock();
        }

        #endregion



        #region Static Interface

        public static readonly TweenCurveFactory CurveFactory = new TweenCurveFactory(false);

        public static TweenHash Tween(object targ)
        {
            return TweenHash.GetTweenHash(targ);
        }

        public static ITweenHash Tween(object id, TweenerUpdateCallback callback, float dur)
        {
            return new CallbackTweenerHash(id, callback, dur);
        }

        public static Tweener PlayCurve(object targ, TweenCurve curve, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, curve);
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayCurve(object targ, string propName, AnimationCurve curve, int option = 0, object autoKillToken = null)
        {
            if (curve == null) throw new System.ArgumentNullException(nameof(curve));
            float dur = (curve.keys.Length > 0) ? curve.keys.Last().time : 0f;
            var tween = new ObjectTweener(targ, CurveFactory.CreateFromTo(targ,
                                                                          CurveFactory.GetAccessor(targ, propName),
                                                                          EaseMethods.FromAnimationCurve(curve),
                                                                          dur, null, null, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayCurve(object targ, string propName, AnimationCurve curve, float dur, int option = 0, object autoKillToken = null)
        {
            if (curve == null) throw new System.ArgumentNullException(nameof(curve));
            var tween = new ObjectTweener(targ, CurveFactory.CreateFromTo(targ,
                                                                          CurveFactory.GetAccessor(targ, propName),
                                                                          EaseMethods.FromAnimationCurve(curve),
                                                                          dur, null, null, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayTo(object targ, string propName, object end, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateTo(targ, CurveFactory.GetAccessor(targ, propName), EaseMethods.Linear, dur, end, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayTo(object targ, string propName, Ease ease, object end, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateTo(targ, CurveFactory.GetAccessor(targ, propName), ease, dur, end, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayFrom(object targ, string propName, object start, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateFrom(targ, CurveFactory.GetAccessor(targ, propName), EaseMethods.Linear, dur, start, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayFrom(object targ, string propName, Ease ease, object start, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateFrom(targ, CurveFactory.GetAccessor(targ, propName), ease, dur, start, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayBy(object targ, string propName, object amt, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateBy(targ, CurveFactory.GetAccessor(targ, propName), EaseMethods.Linear, dur, amt, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayBy(object targ, string propName, Ease ease, object amt, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateBy(targ, CurveFactory.GetAccessor(targ, propName), ease, dur, amt, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayFromTo(object targ, string propName, object start, object end, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateFromTo(targ, CurveFactory.GetAccessor(targ, propName), EaseMethods.Linear, dur, start, end, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        public static Tweener PlayFromTo(object targ, string propName, Ease ease, object start, object end, float dur, int option = 0, object autoKillToken = null)
        {
            var tween = new ObjectTweener(targ, CurveFactory.CreateFromTo(targ, CurveFactory.GetAccessor(targ, propName), ease, dur, start, end, option));
            tween.AutoKillToken = autoKillToken;
            tween.Play();
            return tween;
        }

        #endregion

        #region Special Types

        private readonly struct TokenPairing
        {
            public readonly object Target;
            public readonly object TokenUid;

            public TokenPairing(object targ, object uid)
            {
                this.Target = targ;
                this.TokenUid = uid;
            }

            public bool IsNull { get { return Target == null && TokenUid == null; } }

        }

        private class TokenPairingComparer : IEqualityComparer<TokenPairing>
        {


            public bool Equals(TokenPairing x, TokenPairing y)
            {
                //if (object.ReferenceEquals(x.Target, null)) return object.ReferenceEquals(y.Target, null);
                //if (object.ReferenceEquals(y.Target, null)) return false;
                //if (object.ReferenceEquals(x.TokenUid, null)) return object.ReferenceEquals(y.TokenUid, null);
                //if (object.ReferenceEquals(y.TokenUid, null)) return false;
                //return x.Target.Equals(y.Target) && x.TokenUid.Equals(y.TokenUid);
                return EqualityComparer<object>.Default.Equals(x.Target, y.Target) && EqualityComparer<object>.Default.Equals(x.TokenUid, y.TokenUid);
            }

            public int GetHashCode(TokenPairing obj)
            {
                //int a = (!object.ReferenceEquals(obj.Target, null)) ? obj.Target.GetHashCode() : 0;
                //int b = (!object.ReferenceEquals(obj.TokenUid, null)) ? obj.TokenUid.GetHashCode() : 0;
                //return a ^ b;
                return (obj.Target?.GetHashCode() ?? 0) ^ (obj.Target?.GetHashCode() ?? 0);
            }
        }

        private class TweenerCollection : ICollection<Tweener>
        {

            #region Fields

            private HashSet<Tweener> _tweens = new HashSet<Tweener>();
            private HashSet<Tweener> _toAdd = new HashSet<Tweener>();
            private HashSet<Tweener> _toRemove = new HashSet<Tweener>();
            private int _lockCount = 0;

            #endregion

            #region Properties

            public bool Locked => _lockCount > 0;

            #endregion

            #region Methods

            public Tweener Find(System.Func<Tweener, bool> pred)
            {
                if (this.Count == 0) return null;

                try
                {
                    this.Lock();
                    var e = _instance._runningTweens.GetEnumerator();
                    while (e.MoveNext())
                    {
                        if (pred(e.Current)) return e.Current;
                    }
                    return null;
                }
                finally
                {
                    this.Unlock();
                }
            }

            public void Lock()
            {
                _lockCount++;
            }

            public void Unlock()
            {
                _lockCount--;

                if (_lockCount <= 0)
                {
                    _lockCount = 0;
                    if (_toRemove.Count > 0)
                    {
                        var e = _toRemove.GetEnumerator();
                        while (e.MoveNext())
                        {
                            this.Remove(e.Current);
                        }
                        _toRemove.Clear();
                    }
                    if (_toAdd.Count > 0)
                    {
                        var e = _toAdd.GetEnumerator();
                        while (e.MoveNext())
                        {
                            this.Add(e.Current);
                        }
                        _toAdd.Clear();
                    }
                }
            }

            internal void StageRemove(Tweener tween)
            {
                _toRemove.Add(tween);
            }

            #endregion

            #region ICollection Interface

            public int Count => _tweens.Count;

            bool ICollection<Tweener>.IsReadOnly => false;

            public void Add(Tweener tween)
            {
                if (_lockCount > 0)
                {
                    if (_tweens.Contains(tween) || _toAdd.Contains(tween)) return;
                    _toAdd.Add(tween);
                }
                else
                {
                    if (_tweens.Contains(tween)) return;

                    _tweens.Add(tween);
                    tween.Scrub(0f); //scrub to initialize values, this way if update doesn't happen for an entire frame, we get that init value

                    if (tween.Id != null)
                    {
                        var token = new TokenPairing(tween.Id, tween.AutoKillToken);
                        Tweener old;
                        if (_autoKillDict.TryGetValue(token, out old) && old != tween)
                        {
                            old.Kill();
                        }
                    }
                }
            }

            public bool Remove(Tweener tween)
            {
                if (_lockCount > 0)
                {
                    if (!_tweens.Contains(tween)) return false;
                    if (_toRemove.Contains(tween)) return false;
                    _toRemove.Add(tween);
                    return true;
                }
                else if (_tweens.Remove(tween))
                {
                    if (tween.Id != null && tween.IsComplete)
                    {
                        var token = new TokenPairing(tween.Id, tween.AutoKillToken);
                        Tweener auto;
                        if (_autoKillDict.TryGetValue(token, out auto) && auto == tween)
                        {
                            _autoKillDict.Remove(token);
                        }
                    }
                    return true;
                }

                return false;
            }

            public void Clear()
            {
                if (_lockCount > 0)
                {
                    _toRemove.AddRange(_tweens);
                }
                else
                {
                    _tweens.Clear();
                }
            }

            public bool Contains(Tweener item)
            {
                if (_lockCount > 0)
                {
                    return (_tweens.Contains(item) && !_toRemove.Contains(item)) || _toAdd.Contains(item);
                }
                else
                {
                    return _tweens.Contains(item);
                }
            }

            public void CopyTo(Tweener[] array, int arrayIndex)
            {
                _tweens.CopyTo(array, arrayIndex);
            }

            public HashSet<Tweener>.Enumerator GetEnumerator()
            {
                return _tweens.GetEnumerator();
            }

            IEnumerator<Tweener> IEnumerable<Tweener>.GetEnumerator()
            {
                return _tweens.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _tweens.GetEnumerator();
            }

            #endregion

        }

        #endregion

    }

}
