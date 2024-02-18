using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                IDictionary<string, JToken> parsedJobject = JObject.Parse(json);
                foreach (var element in parsedJobject)
                {
                    var field = _instance.GetType().GetField(element.Key);
                    if (field != null && field.GetCustomAttributes().Select(t => t.GetType())
                            .Contains(typeof(OutsideInitializedAttribute)))
                    {
                        if (
                            _allowedTypes.ContainsKey(field.FieldType))
                            field.SetValue(_instance, Convert.ChangeType(element.Value, field.FieldType));
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
            var fields = _instance.GetType().GetFields();
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.GetCustomAttributes()
                        .FirstOrDefault(t => t.GetType() == typeof(OutsideInitializedAttribute)) == null) continue;
                if (!_allowedTypes.ContainsKey(field.FieldType))
                    Debug.LogException(new TypeIsNotAllowedException(field.FieldType));

                field.SetValue(_instance,
                    _allowedTypes[field.FieldType].Invoke(position, field.Name, field.GetValue(_instance)));

                position.position += new Vector2(0f, EditorGUIUtility.singleLineHeight);
            }

            position.height = 1;
            EditorGUI.DrawRect(position, new Color(0.5f, 0.5f, 0.5f, 1));
            if (EditorGUI.EndChangeCheck()) property.stringValue = JsonConvert.SerializeObject(_instance);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _currentType = Type.GetType(property.FindParentProperty().FindPropertyRelative("TypeRef")
                .FindPropertyRelative("qualifiedName").stringValue);
            if (_currentType != null) _instance = Activator.CreateInstance(_currentType);
            if (_currentType == null || _instance == null) return 0f;
            var count = _instance.GetType().GetFields().Count(t => _allowedTypes.ContainsKey(t.FieldType) &&
                                                                   t.GetCustomAttributes().Select(t => t.GetType())
                                                                       .Contains(
                                                                           typeof(OutsideInitializedAttribute)));
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