# ParamLib
Library to interface with VRChat Avatar V3 stage parameters via MelonLoader Mod

## Example
A quick example showing how to use ParamLib

The example creates three parameters, each a different type (float, bool, and int). Each parameter explains what it returns in the comments above it, based on the invoked value (`boolCheck`).

```cs
private Action<bool> shouldBeOne_updated = (sbo) => { };
private bool shouldBeOne = true;

private readonly List<IParam> parameters = new List<IParam>()
{
    new FloatParameter((boolCheck) => 
    {
        // Returns 1 if true, 0 if false
        if(boolCheck)
            return 1;
        else
            return 0;
    }, "FloatSBOCheck", false),
    new BoolParameter((boolCheck) =>
    {
        // Returns true if boolCheck is true, false if boolCheck is false
        // (lol this is just an example okay)
        if(boolCheck)
            return true;
        else
            return false;
    }, "BoolSBOCheck"),
    new IntParameter((boolCheck) =>
    {
        // Returns 1 if true, 0 if false
        if(boolCheck)
            return 1;
        else
            return 0;
    }, "IntSBOCheck")
};

// Call this when the user's avatar (the one with the parameter) changes
// While you don't *have* to use VRCPlayer, you can use something else like VRC.Player, get the gameObject container, and GetComponent<VRCPlayer>()
public void OnAvatarChanged(VRCPlayer player)
{
    // Get this user's params
    VRCExpressionParameters.Parameter[] usersParams = ParamLib.GetParams(player);
    bool isLocal = userParams == ParamLib.GetLocalParams();
    foreach(IParam param in parameters)
    {
        // Find all the Parameters (if local)
        if(isLocal)
            param.ResetParam();
        // Check if the user has this param
        if(ParamLib.DoesParamExist(param.name, param.valueType, usersParams))
        {
            // User has Param; do whatever you'd like here
            // For demonstration, we'll just debug this
            MelonLoader.MelonLogger.Msg($"{player.prop_String_1} has parameter {param.name}!");
        }
    }
}

/*
The ParamLib's BaseParam.cs contains all animator conditional types

BoolBaseParam - Drives bool parameter values; does not contain Prioritised.
IntBaseParam - Drives int parameter values; does not contain Prioritised.
FloatBaseParam - Drives float parameter values; Prioritised invokes the Prioritised Method in the AvatarPlayableController type.
*/
public class BoolParameter : ParamLib.BoolBaseParam, IParam
{
    public BoolParameter(Func<bool, bool> getVal, string parameterName) : base(paramName: parameterName)
    {
        shouldBeOne_updated += (sbo) => 
        {
            bool theValue = getVal.Invoke(sbo);
            ParamValue = theValue;
        }
    }

    public string[] GetName() => new[] {ParamName};
    public void ZeroParam() => ZeroParams();
    public void ResetParam() => ResetParams();
}

public class IntParameter : ParamLib.IntBaseParam, IParam
{
    public IntParameter(Func<bool, int> getVal, string parameterName) : base(paramName: parameterName)
    {
        shouldBeOne_updated += (sbo) =>
        {
            int theValue = getVal.Invoke(sbo);
            ParamValue = theValue;
        }
    }

    public string[] GetName() => new[] {ParamName};
    public void ZeroParam() => ZeroParams();
    public void ResetParam() => ResetParams();
}

public class FloatParameter : ParamLib.FloatBaseParam, IParam
{
    public FloatParameter(Func<bool, float> getVal, string parameterName, bool prioritisedFloat) : base(paramName: parameterName, prioritised: prioritisedFloat)
    {
        shouldBeOne_updated += (sbo) =>
        {
            float theValue = getVal.Invoke(sbo);
            ParamValue = theValue;
        }
    }

    public string[] GetName() => new[] {ParamName};
    public void ZeroParam() => ZeroParams();
    public void ResetParam() => ResetParams();
}

// Used for updating parameters
// Whenever shouldBeOne_updated is invoked, parameters will update
public void Update() => shouldBeOne_updated.Invoke(shouldBeOne);

// Used for classification of all Parameter Types
public interface IParam
{
    // Get Parameter name(s)
    string[] GetName();
    // Reset Parameter Index to null
    void ZeroParam();
    // Rescan for parameter
    void ResetParam();
}
```

## Documentation

Local means that the method should only be used to interact with the local client,

Global means that the method can be used on other players' parameters.

### ParamLib.cs

#### public static void PrioritizeParameter(int paramIndex)

(Local) Prioritizes parameter at index on the local avatar. This should only be used for floats.

#### public static VRCExpressionParameters.Parameter[] GetLocalParams()

(Local) Gets all parameters on a local user's current SDK3 avatar.

#### public static VRCExpressionParameters.Parameter[] GetParams(VRCPlayer player)

(Local/Global) Gets all parameters from a user (VRCPlayer type). Can be used to get a local player's parameters, but you should use **GetLocalParams()** for this instead.

#### public static DoesParamExist(string paramName, VRCExpressionParameters.ValueType paramType, VRCExpressionParameters.Parameter[] parameters = null)

(Local/Global) Checks a user's (VRCPlayer type) parameters to see if one matches the name and paramType given.

If `parameters` value is left null, then it will default to **GetLocalParams()**

#### public static (int?, VRCExpressionParameters.Parameter) FindParam(string paramName, VRCExpressionParameters.ValueType paramType, VRCExpressionParameters.Parameter[] parameters = null)

(Local/Global) Gets a parameter that matches the paramName and paramType in the user's parameter's list (`parameters`)

If `parameters` value is left null, then it will default to **GetLocalParams()**

#### public static double? GetParamDefaultValue(VRCExpressionParameters.Parameter param)

(Local/Global) Returns the default value for a parameter

#### public static bool SetParameter(int paramIndex, float value)

(Local) Sets the parameter at the index given for the local user to the value given.

### BaseParam.cs
Everything in BaseParam.cs is **local**.

#### public class BaseParam(string paramName, VRCExpressionParameters.ValueType paramType)

Defines a parameter that can be driven.

`paramName` defines the name of the parameter you'd wish to drive.

`paramType` is the type of parameter you'd wish to drive.

**public void ResetParam()**

Attempts to find the parameter on a local user's avatar. Should be called every time the local user's avatar refreshes.

**public void ZeroParam()**

Sets the parameter to null/no longer drives the parameter. Should be called when you decide to no longer drive the parameter.

**protected double ParamValue**

*get*

Returns the parameter's current value

*set*

Sets the current parameter's value

**public int? ParamIndex**

Gets or sets the current index of the parameter on the local user's avatar.

**public readonly string ParamName**

Gets the name of the current-driven parameter.

**protected VRCExpressionParameters.Parameter ParameterLiteral**

The literal parameter that's found

#### public class BoolBaseParam(string paramName) : BaseParam
: base(paramName, VRCExpressionParameters.ValueType.Bool)

See **BaseParam**

#### public class IntBaseParam(string paramName) : BaseParam
: base(paramName, VRCExpressionParameters.ValueType.Int)

See **BaseParam**

#### public class FloatBaseParam(string paramName, bool prioritised = false)
: base(paramName, VRCExpressionParameters.ValueType.Float)

See **BaseParam**

`prioritized` Defines whether the float parameter should be prioritized or not.

#### public class XYParam(FloatBaseParam x, FloatBaseParam y)

See **FloatBaseParam**

Combines two float parameters to make one x,y class. Not required for x,y stuff in an animator controller, just used to make code neater.
