using System;

namespace TypeDropdownWithParameters.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OutsideInitializedAttribute : Attribute
    {
    }
}