using System;
using UnityEngine;

namespace TypeDropdownWithParameters.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : PropertyAttribute
    {
    }
}