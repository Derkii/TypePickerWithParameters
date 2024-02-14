using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TypeRef.Runtime;
using UnityEditor;
using UnityEngine;

namespace TypeRef.Editor
{
    [CustomPropertyDrawer(typeof(ParamsAttribute), true)]
    public class ParamsDrawer : PropertyDrawer
    {
        private static readonly Type[] _allowedTypes = { typeof(float), typeof(string), typeof(bool), typeof(int) };
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
                IDictionary<string, JToken> Jsondata = JObject.Parse(json);
                foreach (var element in Jsondata)
                {
                    var field = _instance.GetType().GetField(element.Key);
                    if (field != null && field.GetCustomAttributes().Select(t => t.GetType())
                            .Contains(typeof(OutsideInitializedAttribute)) &&
                        _allowedTypes.Contains(field.FieldType))
                        field.SetValue(_instance, Convert.ChangeType(element.Value, field.FieldType));
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
                if (field.FieldType == typeof(float))
                {
                    if (field.GetCustomAttributes()
                            .FirstOrDefault(t => t.GetType() == typeof(OutsideInitializedAttribute)) == null) continue;
                    field.SetValue(_instance,
                        EditorGUI.FloatField(position, field.Name, (float)field.GetValue(_instance)));
                    position.position += new Vector2(0f, EditorGUIUtility.singleLineHeight);
                }
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
            var count = _instance.GetType().GetFields().Count(t => _allowedTypes.Contains(t.FieldType) &&
                                                                   t.GetCustomAttributes().Select(t => t.GetType())
                                                                       .Contains(
                                                                           typeof(OutsideInitializedAttribute)));
            return count *
                   EditorGUIUtility.singleLineHeight;
        }
    }
}