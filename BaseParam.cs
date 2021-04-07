using System.Collections;
using MelonLoader;
using UnityEngine;

namespace ParamLib
{
    public class BaseParam
    {
        protected BaseParam(string paramName)
        {
            ParamIndex = ParamLib.GetParamIndex(paramName);
            _paramName = paramName;
        }

        public void ResetParam() => ParamIndex = ParamLib.GetParamIndex(_paramName);
        

        protected double ParamValue
        {
            get => _paramValue;
            set
            {
                if (!ParamIndex.HasValue) return;
                if (ParamLib.SetParameter(ParamIndex.Value, (float) value))
                    _paramValue = value;
            }
        }

        public int? ParamIndex;
        
        private readonly string _paramName;
        protected double _paramValue;
    }

    public class IntBaseParam : BaseParam
    {
        public new int ParamValue
        {
            get => (int) _paramValue;
            set
            {
                base.ParamValue = value;
                _paramValue = value;
            }
        }
        
        public IntBaseParam(string paramName) : base(paramName)
        {
        }
    }
    
    public class FloatBaseParam : BaseParam
    {
        public new float ParamValue
        {
            get => (float) _paramValue;
            set
            {
                base.ParamValue = value;
                _paramValue = value;
            }
        }

        private bool Prioritised
        {
            get => _prioritised;
            set
            {
                if (value && ParamIndex.HasValue)
                    ParamLib.PrioritizeParameter(ParamIndex.Value);
                
                _prioritised = value;
            }
        }
        private bool _prioritised;

        public FloatBaseParam(string paramName, bool prioritised = false) : base(paramName)
        {
            if (!prioritised || !ParamIndex.HasValue) return;
            
            Prioritised = true;
            MelonCoroutines.Start(KeepParamPrioritised());
        } 
        
        private IEnumerator KeepParamPrioritised()
        {
            for (;;)
            {
                yield return new WaitForSeconds(5);
                if (!Prioritised || !ParamIndex.HasValue) continue;
                ParamLib.PrioritizeParameter(ParamIndex.Value);
            }
        }
    }

    public class XYParam
    {
        protected FloatBaseParam X, Y;

        protected Vector2 ParamValue
        {
            set
            {
                X.ParamValue = value.x;
                Y.ParamValue = value.y;
            }
        }

        protected XYParam(FloatBaseParam x, FloatBaseParam y)
        {
            X = x;
            Y = y;
        }

        protected void ResetParams()
        {
            X.ResetParam();
            Y.ResetParam();
        }

        protected void ZeroParams()
        {
            X.ParamIndex = null;
            Y.ParamIndex = null;
        }
    }
}