using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
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
        
        public static string[] ExcludedParams = {
            "IsLocal",
            "Viseme",
            "Voice",
            "GestureLeft",
            "GestureRight",
            "GestureLeftWeight",
            "GestureRightWeight",
            "AngularY",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "Upright",
            "Grounded",
            "Seated",
            "AFK",
            "Expression1",
            "Expression2",
            "Expression3",
            "Expression4",
            "Expression5",
            "Expression6",
            "Expression7",
            "Expression8",
            "Expression9",
            "Expression10",
            "Expression11",
            "Expression12",
            "Expression13",
            "Expression14",
            "Expression15",
            "Expression16",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "InStation"
        };
        
        private static readonly MethodInfo PrioritizeMethod = typeof(AvatarPlayableController).GetMethods().Where(info =>
                info.Name.Contains("Method") && !info.Name.Contains("PDM") && info.Name.Contains("Public")
                && info.GetParameters().Length == 1 && info.Name.Contains("Int32") &&
                info.ReturnType == typeof(void))
            .First(method => XrefScanner.XrefScan(method).Any(xref => xref.Type == XrefType.Global && xref.ReadAsObject().ToString().Contains("Ran out of free puppet channels!")));

        private static readonly MethodInfo SetMethod = typeof(AvatarPlayableController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m =>
                m.Name.Contains("Method_Private_Boolean_Int32_Single") && m.GetParameters().Length == 2);

        public static void PrioritizeParameter(int paramIndex)
        {
            if (LocalPlayableController == null) return;

            PrioritizeMethod.Invoke(LocalPlayableController, new object[] { paramIndex });
        }

        public static VRCExpressionParameters.Parameter[] GetLocalParams() =>
            GetParams(VRCPlayer.field_Internal_Static_VRCPlayer_0);

        public static VRCExpressionParameters.Parameter[] GetParams(VRCPlayer player) => player?.prop_VRCAvatarManager_0
            ?.prop_VRCAvatarDescriptor_0?.expressionParameters
            ?.parameters;

        public static bool DoesParamExist(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCExpressionParameters.Parameter[] parameters = null)
        {
            // If they're null, then try getting LocalParams
            parameters = parameters ?? GetLocalParams();
            
            // Separate Length from nulll check, otherwise you'll get a null exception if parameters are null
            return parameters != null && parameters.Any(p => p.name == paramName && p.valueType == paramType);
        }
        
        public static (int?, VRCExpressionParameters.Parameter) FindParam(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCExpressionParameters.Parameter[] parameters = null)
        {
            var indexOffset = 0;
            
            // If they're null, then try getting LocalParams
            parameters = parameters ?? GetLocalParams();
            
            if (parameters == null) return (null, null);
            

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.name == null) continue;
                //Fix for VRChat being dumb when someone uses a default param in their avatar
                if (ExcludedParams.Contains(param.name)) indexOffset--;
                if (param.name == paramName && param.valueType == paramType) return (i + indexOffset, parameters[i]);
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
