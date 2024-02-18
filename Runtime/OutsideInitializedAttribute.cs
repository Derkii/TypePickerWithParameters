using System;

namespace TypePickerWithParameters.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OutsideInitializedAttribute : Attribute
    {
    }
}