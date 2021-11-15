using System.Linq;
using UnhollowerRuntimeLib.XrefScans;
using VRC.SDK3.Avatars.Components;
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
            .First(method => XrefScanner.UsedBy(method).Any(xrefs =>
                XrefScanner.UsedBy(xrefs.TryResolve()).Any(i => i.TryResolve().Name == "OnEnable")));

        private static readonly MethodInfo SetMethod = typeof(AvatarPlayableController).GetMethods().First(m =>
            m.Name.Contains("Boolean_Int32_Single") &&
            XrefScanner.UsedBy(m).All(inst => inst.Type == XrefType.Method && inst.TryResolve()?.DeclaringType == typeof(AvatarPlayableController)));

        public static void PrioritizeParameter(int paramIndex)
        {
            if (LocalPlayableController == null) return;

            PrioritizeMethod.Invoke(LocalPlayableController, new object[] { paramIndex });
        }
        
        public static VRCExpressionParameters.Parameter[] GetLocalParams() => VRCPlayer.field_Internal_Static_VRCPlayer_0
            ?.prop_VRCAvatarManager_0?.prop_VRCAvatarDescriptor_0?.expressionParameters
            ?.parameters;

        public static VRCExpressionParameters.Parameter[] GetParams(VRCPlayer player)
        {
            // Get the Avatar
            GameObject _avatar = player.transform.Find("ForwardDirection").Find("Avatar").gameObject;
            // Get the Avatar Descriptor / easy method ;)
            VRCAvatarDescriptor _descriptor = _avatar.GetComponent<VRCAvatarDescriptor>();
            // Get the Expression Parameters
            VRCExpressionParameters _parameters = _descriptor.expressionParameters;
            // Return the list
            return _parameters.parameters;
        }

        public bool DoesParamExist(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCExpressionParameters.Parameter[] parameters = new []{})
        {
            // If they're the default value, then assume Local Params
            if(parameters == parameters == new []{})
                parameters = GetLocalParams();
            // If they're null, then just return null
            if (parameters == null)
                return (null, null);
            // Separate Length from nulll check, otherwise you'll get a null exception if parameters are null
            if (parameters.Length == 0)
                return (null, null);

            bool exists = false;
            for (int i = 0; i < parameters.Length; i++)
                if (!exists)
                {
                    exists = parameters[i].name == paramName && parameters[i].valueType == paramType;
                }

            return exists;
        }
        
        public static (int?, VRCExpressionParameters.Parameter) FindParam(string paramName, VRCExpressionParameters.ValueType paramType,
            VRCExpressionParameters.Parameter[] parameters = new []{})
        {
            // If they're the default value, then assume Local Params
            if(parameters == parameters == new []{})
                parameters = GetLocalParams();
            // If they're null, then just return null
            if (parameters == null)
                return (null, null);
            // Separate Length from nulll check, otherwise you'll get a null exception if parameters are null
            if (parameters.Length == 0)
                return (null, null);

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.name == null) continue;
                if (param.name == paramName && param.valueType == paramType) return (i, parameters[i]);
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
