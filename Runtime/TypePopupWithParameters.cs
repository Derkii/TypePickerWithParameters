using System;
using Newtonsoft.Json;
using UnityEngine;

namespace TypeRef.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OutsideInitializedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ParamsAttribute : PropertyAttribute
    {
    }

    [Serializable]
    public class TypePickerWithParameters<T>
    {
        public TypeRef<T> TypeRef;
        public T Instance
        {
            get
            {
                if (instance == null) instance = (T)JsonConvert.DeserializeObject(Params, (Type)TypeRef);
                return instance;
            }
        }

        private T instance;
        [Params] public string Params;
    }
}