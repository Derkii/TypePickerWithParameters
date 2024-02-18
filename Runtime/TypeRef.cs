using System;
using UnityEngine;

namespace TypePickerWithParameters.Runtime
{
    [Serializable]
    public class TypeRef<T> : ISerializationCallbackReceiver
    {
        [SerializeField] private string qualifiedName;

#if UNITY_EDITOR
        [SerializeField] private string baseTypeName;
#endif
        private Type storedType;

        public TypeRef(Type typeToStore)
        {
            storedType = typeToStore;
        }

        public TypeRef()
        {
            storedType = typeof(T);
        }

        public void OnBeforeSerialize()
        {
            if (storedType != null) qualifiedName = storedType.AssemblyQualifiedName;

#if UNITY_EDITOR
            baseTypeName = typeof(T).AssemblyQualifiedName;
#endif
        }

        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(qualifiedName) || qualifiedName == "null")
            {
                storedType = null;
                return;
            }

            storedType = Type.GetType(qualifiedName);
        }

        public override string ToString()
        {
            if (storedType == null) return string.Empty;
            return storedType.Name;
        }

        public static implicit operator Type(TypeRef<T> t)
        {
            return t.storedType;
        }

        public static implicit operator TypeRef<T>(Type t)
        {
            return new TypeRef<T>(t);
        }
    }
}