﻿namespace com.spacepuppy
{

    public enum VariantType : sbyte
    {
        Object = -1,
        Null = 0,
        String = 1,
        Boolean = 2,
        Integer = 3,
        Float = 4,
        Double = 5,
        Vector2 = 6,
        Vector3 = 7,
        Vector4 = 8,
        Quaternion = 9,
        Color = 10,
        DateTime = 11,
        GameObject = 12,
        Component = 13,
        LayerMask = 14,
        Rect = 15,
        Numeric = 16,
        Ref = 17,
    }

    public enum QuitState
    {
        None,
        BeforeQuit,
        Quit
    }

    public enum EntityRelativity
    {
        Entity = 0,
        Self = 1,
        SelfAndChildren = 2
    }

    /// <summary>
    /// Search parameter type
    /// </summary>
    public enum SearchBy
    {
        Nothing = 0,
        Tag = 1,
        Name = 2,
        Type = 3
    }

    /// <summary>
    /// Describe an axis in cartesian coordinates, Useful for components that need to serialize which axis to use in some fashion.
    /// </summary>
    public enum CartesianAxis
    {
        Zneg = -3,
        Yneg = -2,
        Xneg = -1,
        X = 0,
        Y = 1,
        Z = 2
    }

    [System.Flags()]
    public enum ComparisonOperator
    {
        NotEqual = 0,
        LessThan = 1,
        GreaterThan = 2,
        NotEqualAlt = 3,
        Equal = 4,
        [System.ComponentModel.Description("Less Than Or Equal")]
        LessThanEqual = 5,
        [System.ComponentModel.Description("Greater Than Or Equal")]
        GreatThanEqual = 6,
        Always = 7
    }

    /// <summary>
    /// Enum used by InfoboxAttribute to define which message box type to display as.
    /// </summary>
    public enum InfoBoxMessageType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public enum AudioInterruptMode
    {
        StopIfPlaying = 0,
        DoNotPlayIfPlaying = 1,
        PlayOverExisting = 2
    }

    /// <summary>
    /// Used with the TypeReference class to define how the inspector should show the types in the drop down.
    /// </summary>
    public enum TypeDropDownListingStyle
    {
        Flat = 0,
        Namespace = 1,
        ComponentMenu = 2
    }

}