using System;

namespace TypePickerWithParameters.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OutsideInitializedAttribute : Attribute
    {
        public string InitializeAs;

        public OutsideInitializedAttribute(string initializeAs = "")
        {
            InitializeAs = initializeAs;
        }
    }
}