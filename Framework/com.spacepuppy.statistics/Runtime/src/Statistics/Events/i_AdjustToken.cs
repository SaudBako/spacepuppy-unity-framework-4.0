﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Events;
using com.spacepuppy.Utils;

namespace com.spacepuppy.Statistics.Events
{

    public class i_AdjustToken : AutoTriggerable
    {

        public enum SetMode
        {
            Set = 0,
            Increment = 1,
            Decrement = 2,
        }

        [SerializeField]
        private string _category;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("_id")]
        private string _token;
        [SerializeField]
        private double _value;
        [SerializeField]
        private SetMode _mode;

        #region Properties

        public string Category { get { return _category; } set { _category = value; } }
        public string Token { get { return _token; } set { _token = value; } }
        public double Value { get { return _value; } set { _value = value; } }
        public SetMode Mode { get { return _mode; } set { _mode = value; } }

        #endregion

        #region Trigger Interface

        public override bool CanTrigger
        {
            get
            {
                return base.CanTrigger && !string.IsNullOrEmpty(_token) && Services.Get<IStatisticsTokenLedger>() != null;
            }
        }

        public override bool Trigger(object sender, object arg)
        {
            if (!this.CanTrigger) return false;

            var service = Services.Get<IStatisticsTokenLedger>();
            if (service == null) return false;

            switch (_mode)
            {
                case SetMode.Set:
                    service.SetStat(_category, _value, _token);
                    return true;
                case SetMode.Increment:
                    service.AdjustStat(_category, _value, _token);
                    return true;
                case SetMode.Decrement:
                    service.AdjustStat(_category, -_value, _token);
                    return true;
                default:
                    return false;
            }
        }

        #endregion

    }

}