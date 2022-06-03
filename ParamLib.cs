using System.Linq;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC.Playables;
using VRC.SDK3.Avatars.ScriptableObjects;
using MethodInfo = System.Reflection.MethodInfo;

// ReSharper disable Unity.NoNullPropagation        Rider please, we both know this wasn't gonna be pretty...

namespace ParamLib
{
    public static class ParamLib
    {
        private static AvatarPlayableController LocalPlayableController => LocalAnimParamController
            ?.field_Private_AvatarPlayableController_0;

        private static AvatarAnimParamController LocalAnimParamController => VRCPlayer.field_Internal_Static_VRCPlayer_0
            ?.field_Private_AnimatorControllerManager_0?.field_Private_AvatarAnimParamController_0;

        private static readonly MethodInfo PrioritizeMethod = typeof(AvatarPlayableController).GetMethods().Where(info =>
                info.Name.Contains("Method") && !info.Name.Contains("PDM") && info.Name.Contains("Public")
                && info.GetParameters().Length == 1 && info.Name.Contains("Int32") &&
                info.ReturnType == typeof(void))
            .First(method => XrefScanner.XrefScan(method).Any(xref => xref.Type == XrefType.Global && xref.ReadAsObject().ToString().Contains("Ran out of free puppet channels!")));
        
        //Why are we xreffing this? Both the _0 and the PDM work.
        private static readonly MethodInfo SetMethod = typeof(AvatarPlayableController).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m => m.Name.Contains("Boolean_Int32_Single") && !m.Name.Contains("PDM"));

        public static void PrioritizeParameter(int paramIndex)
        {
            if (LocalPlayableController == null) return;

            PrioritizeMethod.Invoke(LocalPlayableController, new object[] { paramIndex });
        }

        public static VRCExpressionParameters.Parameter[] GetParams(VRCPlayer player) => player?.prop_VRCAvatarManager_0
            ?.prop_VRCAvatarDescriptor_0?.expressionParameters
            ?.parameters;

        public static Dictionary<int, AvatarParameter> GetPlayableParameters(VRCPlayer player) => player
            ?.prop_VRCAvatarManager_0?.field_Private_AvatarPlayableController_0
            ?.field_Private_Dictionary_2_Int32_AvatarParameter_0;

        public static bool DoesParamExist(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCPlayer player = null)
        {
            // Always get the parameters from the VRCPlayer
            var parameters = GetParams(player ? player : VRCPlayer.field_Internal_Static_VRCPlayer_0);

            // Separate Length from nulll check, otherwise you'll get a null exception if parameters are null
            return parameters != null && parameters.Any(p => p.name == paramName && p.valueType == paramType);
        }
        
        public static (int?, VRCExpressionParameters.Parameter) FindParam(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCPlayer player = null)
        {
            // Get the PlayableParameters from the VRCPlayer
            var parameters = GetParams(player ? player : VRCPlayer.field_Internal_Static_VRCPlayer_0);
            var playableParams = GetPlayableParameters(player ? player : VRCPlayer.field_Internal_Static_VRCPlayer_0);

            var key = Animator.StringToHash(paramName);
            
            if (playableParams == null || !playableParams.ContainsKey(key)) return (null, null);

            foreach (var t in parameters)
            {
                var param = t;
                if (param.name == null) continue;
                if (param.name == paramName && param.valueType == paramType) return (key, t);
            }

            return (null, null);
        }
        
        public static double? GetParamDefaultValue(VRCExpressionParameters.Parameter param) => param?.defaultValue;

        public static bool SetParameter(int paramIndex, float value)
        {
            if (LocalAnimParamController?.field_Private_AvatarPlayableController_0 == null) return false;

            SetMethod.Invoke(LocalAnimParamController.field_Private_AvatarPlayableController_0, new object[] {paramIndex, value});
            return true;
        }
    }
}
