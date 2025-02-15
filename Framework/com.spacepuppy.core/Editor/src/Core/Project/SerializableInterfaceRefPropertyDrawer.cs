﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Dynamic;
using com.spacepuppy.Project;
using com.spacepuppy.Utils;
using com.spacepuppyeditor.Windows;

namespace com.spacepuppyeditor.Core.Project
{

    /// <summary>
    /// Deals with both SerializableInterfaceRef and SelfReducingEntityConfigRef.
    /// </summary>
    [CustomPropertyDrawer(typeof(BaseSerializableInterfaceRef), true)]
    public class SerializableInterfaceRefPropertyDrawer : PropertyDrawer
    {

        public const string PROP_OBJ = "_obj";

        private SelectableComponentPropertyDrawer _componentSelectorDrawer = new SelectableComponentPropertyDrawer()
        {
            RestrictionType = typeof(UnityEngine.Object),
            AllowNonComponents = true,
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var tp = (this.fieldInfo != null) ? this.fieldInfo.FieldType : null;
            var objProp = property.FindPropertyRelative(PROP_OBJ);
            if (tp == null || objProp == null || objProp.propertyType != SerializedPropertyType.ObjectReference)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (objProp.objectReferenceValue == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            else
            {
                return _componentSelectorDrawer.GetPropertyHeight(objProp, label);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var tp = (this.fieldInfo != null) ? this.fieldInfo.FieldType : null;
            var objProp = property.FindPropertyRelative(PROP_OBJ);
            if (tp == null || objProp == null || objProp.propertyType != SerializedPropertyType.ObjectReference)
            {
                this.DrawMalformed(position);
                return;
            }

            var valueType = DynamicUtil.GetReturnType(DynamicUtil.GetMemberFromType(tp, "_value", true));
            if (valueType == null || !(valueType.IsClass || valueType.IsInterface))
            {
                this.DrawMalformed(position);
                return;
            }

            //SelfReducingEntityConfigRef - support
            try
            {
                var interfaceType = typeof(ISelfReducingEntityConfig<>).MakeGenericType(valueType);
                if (interfaceType != null && TypeUtil.IsType(valueType, interfaceType))
                {
                    var childType = typeof(SelfReducingEntityConfigRef<>).MakeGenericType(valueType);
                    if (TypeUtil.IsType(this.fieldInfo.FieldType, childType))
                    {
                        var obj = EditorHelper.GetTargetObjectOfProperty(property);
                        if (obj != null && childType.IsInstanceOfType(obj))
                        {
                            var entity = SPEntity.Pool.GetFromSource(property.serializedObject.targetObject);
                            var source = DynamicUtil.GetValue(obj, "GetSourceType", entity);
                            label.text = string.Format("{0} (Found from: {1})", label.text, source);
                        }
                    }
                }
            }
            catch (System.Exception) { }

            if (objProp.objectReferenceValue == null)
            {
                object val = UnityObjectDropDownWindowSelector.ObjectField(position, label, objProp.objectReferenceValue, valueType, true, true);
                if (val != null && !valueType.IsInstanceOfType(val) && ObjUtil.GetAsFromSource<IProxy>(val) == null)
                {
                    val = null;
                }
                objProp.objectReferenceValue = val as UnityEngine.Object;
            }
            else
            {
                _componentSelectorDrawer.RestrictionType = valueType;
                _componentSelectorDrawer.OnGUI(position, objProp, label);
            }
        }

        private void DrawMalformed(Rect position)
        {
            EditorGUI.LabelField(position, "Malformed SerializedInterfaceRef.");
            Debug.LogError("Malformed SerializedInterfaceRef - make sure you inherit from 'SerializableInterfaceRef'.");
        }


        public static void SetSerializedProperty(SerializedProperty property, UnityEngine.Object obj)
        {
            if (property == null) throw new System.ArgumentNullException(nameof(property));
            var objProp = property.FindPropertyRelative(PROP_OBJ);
            if (objProp != null && objProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                objProp.objectReferenceValue = obj;
            }
        }

        public static UnityEngine.Object GetFromSerializedProperty(SerializedProperty property)
        {
            if (property == null) throw new System.ArgumentNullException(nameof(property));

            return property.FindPropertyRelative(PROP_OBJ)?.objectReferenceValue;
        }

        public static System.Type GetRefTypeFromSerializedProperty(SerializedProperty property)
        {
            if (property == null) throw new System.ArgumentNullException(nameof(property));

            var wrapperType = property.GetTargetType();
            if (TypeUtil.IsType(wrapperType, typeof(SerializableInterfaceRef<>)))
            {
                var valueprop = wrapperType.GetProperty("Value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return valueprop?.PropertyType ?? typeof(UnityEngine.Object);
            }

            return typeof(UnityEngine.Object);
        }

    }

}
