using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Events;
using com.spacepuppy.Utils;

namespace com.spacepuppy.Statistics.Events
{

    public class i_CheckTokenByCategory : AutoTriggerable, IObservableTrigger
    {

        public enum Modes
        {
            Any = 0,
            All = 1,
        }

        [SerializeField]
        private string _category;
        [SerializeField]
        private Modes _mode;
        [SerializeField]
        private double _value;
        [SerializeField]
        private ComparisonOperator _comparison = ComparisonOperator.Equal;

        [SerializeField]
        [SPEvent.Config("daisy chained object (object)")]
        private SPEvent _onSuccess = new SPEvent("OnSuccess");
        [SerializeField]
        [SPEvent.Config("daisy chained object (object)")]
        private SPEvent _onFailure = new SPEvent("OnFailure");

        #region Properties

        public string Category { get { return _category; } set { _category = value; } }
        public Modes Mode { get { return _mode; } set { _mode = value; } }
        public double Value { get { return _value; } set { _value = value; } }
        public ComparisonOperator Comparison { get { return _comparison; } set { _comparison = value; } }

        public SPEvent OnSuccess { get { return _onSuccess; } }
        public SPEvent OnFailure { get { return _onFailure; } }

        #endregion

        #region Trigger Interface

        public override bool CanTrigger
        {
            get
            {
                return base.CanTrigger && !string.IsNullOrEmpty(_category) && Services.Get<IStatisticsTokenLedger>() != null;
            }
        }

        public override bool Trigger(object sender, object arg)
        {
            if (!this.CanTrigger) return false;

            var service = Services.Get<IStatisticsTokenLedger>();
            if (service == null) return false;

            bool result = false;
            switch (_mode)
            {
                case Modes.Any:
                    foreach (var stat in service.EnumerateStats(_category))
                    {
                        if (string.IsNullOrEmpty(stat.Token)) continue;

                        result = CompareUtil.Compare(_comparison, stat.Value ?? 0d, _value);
                        if(result)
                        {
                            goto Finish;
                        }
                    }
                    break;
                case Modes.All:
                    foreach (var stat in service.EnumerateStats(_category))
                    {
                        if (string.IsNullOrEmpty(stat.Token)) continue;

                        result = CompareUtil.Compare(_comparison, stat.Value ?? 0d, _value);
                        if(!result)
                        {
                            goto Finish;
                        }
                    }
                    break;
            }

            Finish:
            if(result)
            {
                if (_onSuccess.HasReceivers) _onSuccess.ActivateTrigger(this, arg);
            }
            else
            {
                if (_onFailure.HasReceivers) _onFailure.ActivateTrigger(this, arg);
            }

            return true;
        }

        #endregion

        #region IObservableTrigger Interface

        BaseSPEvent[] IObservableTrigger.GetEvents()
        {
            return new BaseSPEvent[] { _onSuccess, _onFailure };
        }

        #endregion

    }

}
