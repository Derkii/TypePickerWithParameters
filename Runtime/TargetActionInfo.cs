using System;
using TargetActions;

namespace TypePickerWithParameters.Runtime
{
    [Serializable]
    public class TargetActionInfo : TypePopupWithTuning<TargetAction>
    {
        public float time;
    }
}