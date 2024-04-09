 using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using TypePickerWithParameters.Runtime;
using UnityEditor;
using UnityEngine;

namespace TypePickerWithParameters.Editor
{
    [CustomPropertyDrawer(typeof(ParamsAttribute), true)]
    public class ParamsDrawer : PropertyDrawer
    {
        private static readonly Dictionary<Type, Func<Rect, string, object, object>> _allowedTypes = new()
        {
            {
                typeof(float), (rect, str, ob) => { return EditorGUI.FloatField(rect, str, (float)ob); }
            },
            {
                typeof(int), (rect, str, ob) => { return EditorGUI.IntField(rect, str, (int)ob); }
            },
            {
                typeof(bool), (rect, str, ob) => { return EditorGUI.Toggle(rect, str, (bool)ob); }
            },
            {
                typeof(string), (rect, str, ob) => { return EditorGUI.TextField(rect, str, (string)ob); }
            }
        };

        private Type _currentType;
        private object _instance;
        private bool _showing;

        private void Initialize(SerializedProperty property)
        {
            var json = property.stringValue;

            if (_currentType == null)
            {
                _instance = null;
                return;
            }

            _instance = Activator.CreateInstance(_currentType);
            if (json.Length > 1)
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                var options = new JsonSerializerOptions()
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                    {
                        Modifiers = { TypePickerJsonExtensions.AddPrivateFieldsWithAttributeModifier }
                    }
                };
                var parsedObject = JsonObject.Parse(json).AsObject();
                foreach (var element in parsedObject)
                {
                    object value;
                    if ((element.Value is JsonValue) == false || element.Value.GetValueKind() == JsonValueKind.Object) continue;
                    value = element.Value.GetValueKind() switch
                    {
                        JsonValueKind.String => element.Value.GetValue<string>(),
                        JsonValueKind.Number => element.Value.GetValue<float>(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var field = _instance.GetType().GetField(element.Key,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && field.IsDefined(typeof(OutsideInitializedAttribute)))
                    {
                        if (
                            _allowedTypes.ContainsKey(field.FieldType))
                            field.SetValue(_instance, value);
                        else
                            Debug.LogException(new TypeIsNotAllowedException(field.FieldType));
                    }
                }
            }
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _currentType = Type.GetType(property.FindParentProperty().FindPropertyRelative("TypeRef")
                .FindPropertyRelative("qualifiedName").stringValue);
            if (_currentType == null) return;
            position.height = EditorGUIUtility.singleLineHeight;
            Initialize(property);

            if (_instance == null) return;
            //Enable if you want to have toggle for showing/now showing parameters. Not ready feature.
            /*_showing = EditorGUI.Toggle(position, "Show parameters", _showing);
            if (!_showing)
            {
                position.position += new Vector2(0, EditorGUIUtility.singleLineHeight);
                position.height = 1;
                EditorGUI.DrawRect(position, new Color(0.5f, 0.5f, 0.5f, 1));
                _height = position.y - startPositionY;
                return;

            }
            position.position += new Vector2(0f, EditorGUIUtility.singleLineHeight);*/
            EditorGUI.BeginChangeCheck();
            var fields = _instance.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var attribute = field.GetCustomAttribute(typeof(OutsideInitializedAttribute));
                if (attribute == null) continue;
                if (!_allowedTypes.ContainsKey(field.FieldType))
                    Debug.LogException(new TypeIsNotAllowedException(field.FieldType));

                var outsideInitializedAttribute = (OutsideInitializedAttribute)attribute;
                var name = field.Name;
                name = name.Replace("_", "");
                var charArray = name.ToCharArray();
                charArray[0] = charArray[0].ToString().ToUpper().ToCharArray()[0];
                name = new string(charArray);
                field.SetValue(_instance,
                    _allowedTypes[field.FieldType].Invoke(position,
                        outsideInitializedAttribute.InitializeAs == ""
                            ? name
                            : outsideInitializedAttribute.InitializeAs, field.GetValue(_instance)));
                position.position += new Vector2(0f, EditorGUIUtility.singleLineHeight);
            }

            position.height = 1;
            EditorGUI.DrawRect(position, new Color(0.5f, 0.5f, 0.5f, 1));
            if (EditorGUI.EndChangeCheck())
            {
                using var ms = new MemoryStream();
                JsonSerializer.Serialize(ms, _instance, new JsonSerializerOptions()
                {
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                    {
                        Modifiers = { TypePickerJsonExtensions.AddPrivateFieldsWithAttributeModifier }
                    }
                });
                property.stringValue = Encoding.UTF8.GetString(ms.GetBuffer());
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _currentType = Type.GetType(property.FindParentProperty().FindPropertyRelative("TypeRef")
                .FindPropertyRelative("qualifiedName").stringValue);
            if (_currentType == null) return 0f;
            var count = _currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Count(t => _allowedTypes.ContainsKey(t.FieldType) &&
                            t.IsDefined(typeof(OutsideInitializedAttribute)));
            return count *
                   EditorGUIUtility.singleLineHeight;
        }

        private class TypeIsNotAllowedException : Exception
        {
            public TypeIsNotAllowedException(Type type) : base(
                $"Field is of type {type} and this type is not allowed. Add type to dictionary or change the field's type")
            {
            }
        }
    }
}