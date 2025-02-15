﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Utils;
using com.spacepuppyeditor;

using com.spacepuppy.Statistics.Events;

namespace com.spacepuppyeditor.Statistics.Events
{

    [CustomEditor(typeof(i_CheckToken))]
    public class i_CheckTokenInspector : SPEditor
    {

        private const string PROP_CATEGORY = "_category";
        private const string PROP_TOKEN = "_token";
        private const string PROP_VALUE = "_value";
        private const string PROP_COMPARISON = "_comparison";

        protected override void OnSPInspectorGUI()
        {
            this.serializedObject.Update();

            this.DrawPropertyField(EditorHelper.PROP_SCRIPT);
            this.DrawPropertyField(EditorHelper.PROP_ORDER);
            this.DrawPropertyField(EditorHelper.PROP_ACTIVATEON);
            EditorGUILayout.Space();

            var catprop = this.serializedObject.FindProperty(PROP_CATEGORY);
            var idprop = this.serializedObject.FindProperty(PROP_TOKEN);
            var valueprop = this.serializedObject.FindProperty(PROP_VALUE);
            var compprop = this.serializedObject.FindProperty(PROP_COMPARISON);

            int selection = Mathf.Max(0, StatisticsTokenLedgerCategories.FindIndexOfCategory(catprop.stringValue));
            selection = EditorGUILayout.Popup("Category", selection, StatisticsTokenLedgerCategories.Categories.Select(o => o.Name).ToArray());
            catprop.stringValue = StatisticsTokenLedgerCategories.IndexInRange(selection) ? StatisticsTokenLedgerCategories.Categories[selection].Name : null;

            if (StatisticsTokenLedgerCategories.IndexInRange(selection))
            {
                var category = StatisticsTokenLedgerCategories.Categories[selection];

                selection = category.Entries.IndexOf(idprop.stringValue);
                selection = EditorGUILayout.Popup("Token", selection, category.Entries);
                idprop.stringValue = selection >= 0 ? category.Entries[selection] : null;

                if (category.DataStore == typeof(bool))
                {
                    valueprop.doubleValue = EditorGUILayout.Toggle("True/False", valueprop.doubleValue != 0d) ? 1 : 0;
                    compprop.SetEnumValue(ComparisonOperator.Equal);
                }
                else if (category.DataStore.IsEnum)
                {
                    int eval = (int)valueprop.doubleValue;
                    valueprop.doubleValue = ConvertUtil.ToInt(SPEditorGUILayout.EnumPopup("Value", ConvertUtil.ToEnumOfType(category.DataStore, eval)));
                    compprop.intValue = (int)(EditorGUILayout.Toggle("Not Equal", compprop.intValue == (int)ComparisonOperator.NotEqual) ? ComparisonOperator.NotEqual : ComparisonOperator.Equal);
                }
                else if (category.DataStore == typeof(int))
                {
                    valueprop.doubleValue = EditorGUILayout.IntField("Value", (int)valueprop.doubleValue);
                    SPEditorGUILayout.PropertyField(compprop);
                }
                else if (ConvertUtil.IsNumericType(category.DataStore))
                {
                    valueprop.doubleValue = EditorGUILayout.DoubleField("Value", valueprop.doubleValue);
                    SPEditorGUILayout.PropertyField(compprop);
                }
            }

            EditorGUILayout.Space();
            this.DrawDefaultInspectorExcept(EditorHelper.PROP_SCRIPT, EditorHelper.PROP_ORDER, EditorHelper.PROP_ACTIVATEON, PROP_CATEGORY, PROP_TOKEN, PROP_VALUE, PROP_COMPARISON);

            this.serializedObject.ApplyModifiedProperties();
        }

    }

}