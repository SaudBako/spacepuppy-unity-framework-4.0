﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace com.spacepuppy.SPInput
{

    public delegate bool ButtonDelegate();
    public delegate float AxisDelegate();
    public delegate Vector2 DualAxisDelegate();

    public class DelegatedButtonInputSignature : BaseInputSignature, IButtonInputSignature
    {

        #region Fields

        private ButtonDelegate _delegate;
        private ButtonState _current;
        private ButtonState _currentFixed;
        private double _lastDown = double.NegativeInfinity;
        private double _lastUp = double.NegativeInfinity;

        #endregion

        #region CONSTRUCTOR

        public DelegatedButtonInputSignature(string id, ButtonDelegate del)
            : base(id)
        {
            this.Delegate = del;
        }

        #endregion

        #region Properties

        public ButtonDelegate Delegate
        {
            get { return _delegate; }
            set { _delegate = value ?? (() => false); }
        }

        #endregion

        #region IButtonInputSignature Interface

        public ButtonState CurrentState
        {
            get
            {
                if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
                {
                    return _currentFixed;
                }
                else
                {
                    return _current;
                }
            }
        }

        public ButtonState GetCurrentState(bool getFixedState)
        {
            return (getFixedState) ? _currentFixed : _current;
        }

        public void Consume()
        {
            if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
            {
                _currentFixed = InputUtil.ConsumeButtonState(_currentFixed);
            }
            else
            {
                _current = InputUtil.ConsumeButtonState(_current);
            }
        }

        public double LastDownTime => _lastDown;

        public double LastReleaseTime => _lastUp;

        #endregion

        #region IInputSignature Interfacce

        public override void Update()
        {
            //determine based on history
            _current = InputUtil.GetNextButtonState(_current, _delegate());
            switch(_current)
            {
                case ButtonState.Down:
                    _lastDown = Time.unscaledTimeAsDouble;
                    break;
                case ButtonState.Released:
                    _lastUp = Time.unscaledTimeAsDouble;
                    break;
            }
        }

        public override void FixedUpdate()
        {
            //determine based on history
            _currentFixed = InputUtil.GetNextButtonState(_currentFixed, _delegate());
        }

        public override void Reset()
        {
            _current = ButtonState.None;
            _currentFixed = ButtonState.None;
            _lastDown = double.NegativeInfinity;
            _lastUp = double.NegativeInfinity;
        }

        #endregion

    }

    public class DelegatedAxleInputSignature : BaseInputSignature, IAxleInputSignature
    {

        #region Fields

        private AxisDelegate _delegate;

        #endregion

        #region CONSTRUCTOR

        public DelegatedAxleInputSignature(string id, AxisDelegate del)
            : base(id)
        {
            this.Delegate = del;
        }

        #endregion

        #region Properties

        public AxisDelegate Delegate
        {
            get { return _delegate; }
            set { _delegate = value ?? (() => 0f); }
        }

        public DeadZoneCutoff Cutoff
        {
            get;
            set;
        }

        public bool Invert
        {
            get;
            set;
        }

        #endregion

        #region IAxleInputSignature Interface

        public float CurrentState
        {
            get
            {
                //return _current;

                var v = _delegate();
                if (this.Invert) v *= -1;
                return InputUtil.CutoffAxis(v, this.DeadZone, this.Cutoff);
            }
        }

        public float DeadZone
        {
            get;
            set;
        }

        #endregion

        #region IInputSignature Interfacce

        public override void Update()
        {

        }

        public override void Reset()
        {
        }

        #endregion

    }

    /// <summary>
    /// Dual mode input signature. It allows for axes to act like a button. It behaves exactly as a DelegatedAxleInputSignature, 
    /// but will return appropriate values when treated as a button. Very useful if you have dual inputs for the same 
    /// action that should act like buttons. For instance both pressing up on left stick, or pressing A to jump.
    /// </summary>
    public class DelegatedAxleButtonInputSignature : DelegatedAxleInputSignature, IButtonInputSignature
    {

        #region Fields

        private ButtonState _current;
        private ButtonState _currentFixed;
        private double _lastDown = double.NegativeInfinity;
        private double _lastUp = double.NegativeInfinity;

        #endregion

        #region CONSTRUCTOR

        public DelegatedAxleButtonInputSignature(string id, AxisDelegate del, AxleValueConsideration consideration = AxleValueConsideration.Positive, float axisButtnDeadZone = InputUtil.DEFAULT_AXLEBTNDEADZONE)
            : base(id, del)
        {
            this.AxisButtonDeadZone = axisButtnDeadZone;
            this.Consideration = consideration;
        }

        #endregion

        #region Properties

        /// <summary>
        /// If input is treated as a axis-button, where is the dead-zone of the button. As opposed to the axes' DeadZone.
        /// </summary>
        public float AxisButtonDeadZone
        {
            get;
            set;
        }

        /// <summary>
        /// How should we consider the value returned for the axis.
        /// </summary>
        public AxleValueConsideration Consideration
        {
            get;
            set;
        }

        #endregion

        #region IButtonInputSignature Interface

        public new ButtonState CurrentState
        {
            get
            {
                if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
                {
                    return _currentFixed;
                }
                else
                {
                    return _current;
                }
            }
        }

        public ButtonState GetCurrentState(bool getFixedState)
        {
            return (getFixedState) ? _currentFixed : _current;
        }

        public void Consume()
        {
            if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
            {
                _currentFixed = InputUtil.ConsumeButtonState(_currentFixed);
            }
            else
            {
                _current = InputUtil.ConsumeButtonState(_current);
            }
        }

        public double LastDownTime => _lastDown;

        public double LastReleaseTime => _lastUp;

        #endregion

        #region IInputSignature Interfacce

        public override void Update()
        {
            base.Update();

            float v = base.CurrentState;
            switch (this.Consideration)
            {
                case AxleValueConsideration.Positive:
                    _current = InputUtil.GetNextButtonState(_current, v >= this.AxisButtonDeadZone);
                    break;
                case AxleValueConsideration.Negative:
                    _current = InputUtil.GetNextButtonState(_current, v <= -this.AxisButtonDeadZone);
                    break;
                case AxleValueConsideration.Absolute:
                    //_current = InputUtil.GetNextButtonState(_current, Input.GetButton(this.UnityInputId) || Mathf.Abs(Input.GetAxis(this.UnityInputId)) >= this.AxisButtonDeadZone);
                    _current = InputUtil.GetNextButtonState(_current, v >= this.AxisButtonDeadZone);
                    break;
            }

            switch (_current)
            {
                case ButtonState.Down:
                    _lastDown = Time.unscaledTimeAsDouble;
                    break;
                case ButtonState.Released:
                    _lastUp = Time.unscaledTimeAsDouble;
                    break;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            float v = base.CurrentState;
            switch (this.Consideration)
            {
                case AxleValueConsideration.Positive:
                    //_currentFixed = InputUtil.GetNextButtonState(_currentFixed, Input.GetButton(this.UnityInputId) || Input.GetAxis(this.UnityInputId) >= this.AxisButtonDeadZone);
                    _currentFixed = InputUtil.GetNextButtonState(_currentFixed, v >= this.AxisButtonDeadZone);
                    break;
                case AxleValueConsideration.Negative:
                    //_currentFixed = InputUtil.GetNextButtonState(_currentFixed, Input.GetButton(this.UnityInputId) || Input.GetAxis(this.UnityInputId) <= -this.AxisButtonDeadZone);
                    _currentFixed = InputUtil.GetNextButtonState(_currentFixed, v <= -this.AxisButtonDeadZone);
                    break;
                case AxleValueConsideration.Absolute:
                    //_currentFixed = InputUtil.GetNextButtonState(_currentFixed, Input.GetButton(this.UnityInputId) || Mathf.Abs(Input.GetAxis(this.UnityInputId)) >= this.AxisButtonDeadZone);
                    _currentFixed = InputUtil.GetNextButtonState(_currentFixed, v >= this.AxisButtonDeadZone);
                    break;
            }
        }

        public override void Reset()
        {
            base.Reset();

            _current = ButtonState.None;
            _currentFixed = ButtonState.None;
            _lastDown = double.NegativeInfinity;
            _lastUp = double.NegativeInfinity;
        }

        #endregion

    }

    public class DelegatedDualAxleInputSignature : BaseInputSignature, IDualAxleInputSignature
    {

        #region Fields

        private AxisDelegate _horizontal;
        private AxisDelegate _vertical;

        #endregion

        #region CONSTRUCTOR

        public DelegatedDualAxleInputSignature(string id, AxisDelegate hor, AxisDelegate ver)
            : base(id)
        {
            this.HorizontalDelegate = hor;
            this.VerticalDelegate = ver;
        }

        #endregion

        #region Properties

        public AxisDelegate HorizontalDelegate
        {
            get { return _horizontal; }
            set { _horizontal = value ?? (() => 0f); }
        }

        public AxisDelegate VerticalDelegate
        {
            get { return _vertical; }
            set { _vertical = value ?? (() => 0f); }
        }

        public DeadZoneCutoff Cutoff
        {
            get;
            set;
        }

        public float RadialDeadZone
        {
            get;
            set;
        }

        public DeadZoneCutoff RadialCutoff
        {
            get;
            set;
        }

        public bool InvertX
        {
            get;
            set;
        }

        public bool InvertY
        {
            get;
            set;
        }

        #endregion

        #region IDualAxleInputSignature Interface

        public Vector2 CurrentState
        {
            get
            {
                //return _current;
                Vector2 v = new Vector2(_horizontal(),
                                        _vertical());
                if (this.InvertX) v.x = -v.x;
                if (this.InvertY) v.y = -v.y;
                return InputUtil.CutoffDualAxis(v, this.DeadZone, this.Cutoff, this.RadialDeadZone, this.RadialCutoff);
            }
        }

        public float DeadZone
        {
            get;
            set;
        }

        #endregion

        #region IInputSignature Interface

        public override void Update()
        {

        }

        public override void Reset()
        {
        }

        #endregion

    }

    /// <summary>
    /// Dual mode input signature. It allows for dual axes to act like a button. It behaves exactly as a DelegateDualAxleInputSignature, 
    /// but will return ButtonStates based on the magnitude from center. Very useful if you want to know if the user popped their dual axes. 
    /// </summary>
    public class DelegatedDualAxleButtonInputSignature : DelegatedDualAxleInputSignature, IButtonInputSignature
    {

        #region Fields

        private ButtonState _current;
        private ButtonState _currentFixed;
        private double _lastDown = double.NegativeInfinity;
        private double _lastUp = double.NegativeInfinity;

        #endregion

        #region CONSTRUCTOR

        public DelegatedDualAxleButtonInputSignature(string id, AxisDelegate hor, AxisDelegate ver, float axisButtnDeadZone = InputUtil.DEFAULT_AXLEBTNDEADZONE)
            : base(id, hor, ver)
        {
            this.AxisButtonDeadZone = axisButtnDeadZone;
        }

        #endregion

        #region Properties

        /// <summary>
        /// If input is treated as a axis-button, where is the dead-zone of the button. As opposed to the axes' DeadZone.
        /// </summary>
        public float AxisButtonDeadZone
        {
            get;
            set;
        }

        #endregion

        #region IButtonInputSignature Interface

        public new ButtonState CurrentState
        {
            get
            {
                if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
                {
                    return _currentFixed;
                }
                else
                {
                    return _current;
                }
            }
        }

        public ButtonState GetCurrentState(bool getFixedState)
        {
            return (getFixedState) ? _currentFixed : _current;
        }

        public void Consume()
        {
            if (GameLoop.CurrentSequence == UpdateSequence.FixedUpdate)
            {
                _currentFixed = InputUtil.ConsumeButtonState(_currentFixed);
            }
            else
            {
                _current = InputUtil.ConsumeButtonState(_current);
            }
        }

        public double LastDownTime => _lastDown;

        public double LastReleaseTime => _lastUp;

        #endregion

        #region IInputSignature Interfacce

        public override void Update()
        {
            base.Update();

            float v = base.CurrentState.sqrMagnitude;
            float dz = this.AxisButtonDeadZone;
            _current = InputUtil.GetNextButtonState(_current, v >= (dz * dz));

            switch (_current)
            {
                case ButtonState.Down:
                    _lastDown = Time.unscaledTimeAsDouble;
                    break;
                case ButtonState.Released:
                    _lastUp = Time.unscaledTimeAsDouble;
                    break;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            float v = base.CurrentState.sqrMagnitude;
            float dz = this.AxisButtonDeadZone;
            _currentFixed = InputUtil.GetNextButtonState(_currentFixed, v >= (dz * dz));
        }

        public override void Reset()
        {
            base.Reset();

            _current = ButtonState.None;
            _currentFixed = ButtonState.None;
            _lastDown = double.NegativeInfinity;
            _lastUp = double.NegativeInfinity;
        }

        #endregion

    }

    public class DelegatedCursorInputSignature : BaseInputSignature, ICursorInputSignature
    {

        #region Fields

        private DualAxisDelegate _cursor;

        #endregion

        #region CONSTRUCTOR

        public DelegatedCursorInputSignature(string id, DualAxisDelegate cursor)
            : base(id)
        {
            this.CursorDelegate = cursor;
        }

        public DelegatedCursorInputSignature(string id, AxisDelegate hor, AxisDelegate ver)
            : base(id)
        {
            this.SetCursorDelegate(hor, ver);
        }

        #endregion

        #region Properties

        public DualAxisDelegate CursorDelegate
        {
            get { return _cursor; }
            set { _cursor = value ?? (() => Vector2.zero); }
        }

        public bool InvertX
        {
            get;
            set;
        }

        public bool InvertY
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public void SetCursorDelegate(DualAxisDelegate cursor)
        {
            _cursor = cursor ?? (() => Vector2.zero);
        }

        public void SetCursorDelegate(AxisDelegate hor, AxisDelegate ver)
        {
            if (hor == null && ver == null)
            {
                _cursor = () => Vector2.zero;
            }
            else if (hor == null)
            {
                _cursor = () => new Vector2(0f, ver());
            }
            else if (ver == null)
            {
                _cursor = () => new Vector2(hor(), 0f);
            }
            else
            {
                _cursor = () => new Vector2(hor(), ver());
            }
        }

        #endregion

        #region IDualAxleInputSignature Interface

        public Vector2 CurrentState
        {
            get
            {
                //return _current;
                Vector2 v = _cursor?.Invoke() ?? Vector2.zero;
                if (this.InvertX) v.x = -v.x;
                if (this.InvertY) v.y = -v.y;
                return v;
            }
        }

        #endregion

        #region IInputSignature Interface

        public override void Update()
        {

        }

        public override void Reset()
        {
        }

        #endregion

    }

}
