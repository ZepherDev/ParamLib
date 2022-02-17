using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ParamLib
{
    public class BaseParam
    {
        protected BaseParam(string paramName, VRCExpressionParameters.ValueType paramType)
        {
            ParamName = paramName;
            _paramType = paramType;
            ResetParam();
        }

        public void ResetParam() => (ParamIndex, ParameterLiteral) = ParamLib.FindParam(ParamName, _paramType);

        public void ZeroParam() => ParamIndex = null;

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
        
        public readonly string ParamName;
        private readonly VRCExpressionParameters.ValueType _paramType;
        protected VRCExpressionParameters.Parameter ParameterLiteral;
        private double _paramValue;
    }

    public class BoolBaseParam : BaseParam
    {
        public new bool ParamValue
        {
            get => Convert.ToBoolean(base.ParamValue);
            set => base.ParamValue = Convert.ToDouble(value);
        }
        
        public BoolBaseParam(string paramName) : base(paramName, VRCExpressionParameters.ValueType.Bool)
        {
        }
    }

    public class IntBaseParam : BaseParam
    {
        public new int ParamValue
        {
            get => (int) base.ParamValue;
            set => base.ParamValue = value;
        }
        
        public IntBaseParam(string paramName) : base(paramName, VRCExpressionParameters.ValueType.Int)
        {
        }
    }
    
    public class FloatBaseParam : BaseParam
    {
        private static readonly List<FloatBaseParam> PrioritisedParams = new List<FloatBaseParam>();
        private readonly bool _wantsPriority;
        
        public new float ParamValue
        {
            get => (float) base.ParamValue;
            set => base.ParamValue = value;
        }
        
        public FloatBaseParam(string paramName, bool prioritised = false) : base(paramName, VRCExpressionParameters.ValueType.Float)
        {
            _wantsPriority = prioritised;
            if (_wantsPriority)
                MelonCoroutines.Start(KeepParamPrioritised());
        }

        public new void ResetParam()
        {
            base.ResetParam();

            // If we found a parameter literal, and this param need priority, and it's one of the first 8 params
            if (!ParamIndex.HasValue || !_wantsPriority) return;    // Check if we have a value since sometimes people don't use both x and y for XY params
            
            // Check if this parameter has an index lower than any of the prioritised params, and if so, replace the parameter it's lower than
            if (PrioritisedParams.Count < 8) // If we have less than 8 params, we can just add it to the end
                PrioritisedParams.Add(this);
            else
                foreach (var param in PrioritisedParams.Where(param => param.ParamIndex.HasValue && ParamIndex.Value < param.ParamIndex.Value))
                {
                    // Prioritise this param
                    PrioritisedParams.Remove(param);
                    PrioritisedParams.Add(this);
                    return;
                }
        }

        public new void ZeroParam()
        {
            base.ZeroParam();

            if (PrioritisedParams.Contains(this))
                PrioritisedParams.Remove(this);
        }
        
        private IEnumerator KeepParamPrioritised()
        {
            for (;;)
            {
                yield return new WaitForSeconds(5);
                if (!PrioritisedParams.Contains(this) || !ParamIndex.HasValue) continue;
                ParamLib.PrioritizeParameter(ParamIndex.Value);
            }
        }
    }
    
    public class BinaryBaseParameter : BaseParam
    {
        public new double ParamValue
        {
            get => (float) base.ParamValue;
            set
            {
                // If the value is negative, make it positive
                if (_negativeParam.ParamIndex == null &&
                    value < 0) // If the negative parameter isn't set, cut the negative values
                    return;
                        
                // Ensure value going into the bitwise shifts is between 0 and 1
                var adjustedValue = Math.Abs(value);

                var bigValue = (int) (adjustedValue * (_maxPossibleBinaryInt - 1));

                foreach (var boolChild in _params)
                    boolChild.Value.ParamValue = ((bigValue >> boolChild.Key) & 1) == 1;

                _negativeParam.ParamValue = value < 0;
                
                base.ParamValue = adjustedValue;
            }
        }

        private readonly Dictionary<int, BoolBaseParam> _params = new Dictionary<int, BoolBaseParam>(); // Int represents binary steps
        private readonly BoolBaseParam _negativeParam;
        private int _maxPossibleBinaryInt;
        private readonly string _paramName;

        /* Pretty complicated, but let me try to explain...
         * As with other ResetParam functions, the purpose of this function is to reset all the parameters.
         * Since we don't actually know what parameters we'll be needing for this new avatar, nor do we know if the parameters we currently have are valid
         * it's just easier to just reset everything.
         *
         * Step 1) Find all valid parameters on the new avatar that start with the name of this binary param, and end with a number.
         * 
         * Step 2) Find the binary steps for that number. That's the number of shifts we need to do. That number could be 8, and it's steps would be 3 as it's 3 steps away from zero in binary
         * This also makes sure the number is a valid base2-compatible number
         *
         * Step 3) Calculate the maximum possible value for the discovered binary steps, then subtract 1 since we count from 0.
         *
         * Step 4) Create each parameter literal that'll be responsible for actually changing parameters. It's output data will be multiplied by the highest possible
         * binary number since we can safely assume the highest possible input float will be 1.0. Then we bitwise shift by the binary steps discovered in step 2.
         * Finally, we use a combination of bitwise AND to get whether the designated index for this param is 1 or 0.
         */
        public new void ResetParam()
        {
            _negativeParam.ResetParam();
        
            // Get all parameters starting with this parameter's name, and of type bool
            var boolParams = ParamLib.GetLocalParams().Where(p => p.valueType == VRCExpressionParameters.ValueType.Bool && p.name.StartsWith(_paramName));

            var paramsToCreate = new Dictionary<string, int>();
            foreach (var param in boolParams)
            {
                // Cut the parameter name to get the index
                if (!int.TryParse(param.name.Substring(_paramName.Length), out var index)) continue;
                // Get the shift steps
                var binaryIndex = GetBinarySteps(index);
                // If this index has a shift step, create the parameter
                if (binaryIndex.HasValue)
                    paramsToCreate.Add(param.name, binaryIndex.Value);
            }

            if (paramsToCreate.Count == 0) return;
            
            // Calculate the highest possible binary number
            _maxPossibleBinaryInt = (int)Math.Pow(2, paramsToCreate.Values.Count);
            foreach (var param in paramsToCreate)
                _params.Add(param.Value, new BoolBaseParam(param.Key));
        }
        
        // This serves both as a test to make sure this index is in the binary sequence, but also returns how many bits we need to shift to find it
        private static int? GetBinarySteps(int index)
        {
            var currSeqItem = 1;
            for (var i = 0; i < index; i++)
            {
                if (currSeqItem == index)
                    return i;
                currSeqItem*=2;
            }
            return null;
        }

        public new void ZeroParam()
        {
            _negativeParam.ZeroParam();
            foreach (var param in _params)
                param.Value.ZeroParam();
            _params.Clear();
        }

        public string[] GetName() =>
            // If we have no parameters, return a single value array containing the paramName. If we have values, return the names of all the parameters
            _params.Count == 0 ? new[] {_paramName} : _params.Select(p => p.Value.ParamName).ToArray();

        public BinaryBaseParameter(string paramName) : base(paramName, VRCExpressionParameters.ValueType.Bool)  // Technically a bool ¯\_(ツ)_/¯
        {
            _paramName = paramName;
            _negativeParam = new BoolBaseParam(paramName + "Negative");
        }
    }
}
