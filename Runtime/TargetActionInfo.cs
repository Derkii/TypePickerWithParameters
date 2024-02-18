using System;
using TargetActions;

namespace TypeDropdownWithParameters.Runtime
{
    [Serializable]
    public class TargetActionInfo : TypePopupWithTuning<TargetAction>
    {
        public float time;
    }
}