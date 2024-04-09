using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TypePickerWithParameters.Runtime
{
    [Serializable]
    public class TypePickerWithParameters<T>
    {
        public TypeRef<T> TypeRef;
        [Params] public string Params;

        public async UniTask Initialize()
        {
            if (Instance == null)
            {
                if ((Type)TypeRef == null)
                    Debug.LogException(new Exception("The type is null. Choose the type in the inspector"));
                var readTask = JsonSerializer.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(Params)),
                    (Type)TypeRef, new JsonSerializerOptions()
                    {
                        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                        {
                            Modifiers = { TypePickerJsonExtensions.AddPrivateFieldsWithAttributeModifier }
                        }
                    });
                await readTask;

                Instance = (T)readTask.Result;
            }
        }

        public T Instance { get; private set; }
    }

    public static class TypePickerJsonExtensions
    {
        public static void AddPrivateFieldsWithAttributeModifier(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }

            var fields = typeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(OutsideInitializedAttribute))) continue;
                var propertyInfo = typeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
                propertyInfo.Get = field.GetValue;
                propertyInfo.Set = field.SetValue;
                propertyInfo.AttributeProvider = field;
                typeInfo.Properties.Add(propertyInfo);
            }
        }
    }
}