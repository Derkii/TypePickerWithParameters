using System;
using UnityEngine;

namespace TypePickerWithParameters.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : PropertyAttribute
    {
    }
}