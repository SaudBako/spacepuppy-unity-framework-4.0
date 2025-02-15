using UnityEngine;
using UnityEditor;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Collections;
using com.spacepuppy.Dynamic;
using com.spacepuppy.Tween;
using com.spacepuppy.Tween.Events;
using com.spacepuppy.Utils;

using com.spacepuppyeditor.Core;
using com.spacepuppyeditor.Internal;

namespace com.spacepuppyeditor.Tween.Events
{

    [CustomEditor(typeof(i_Tween))]
    public class i_TweenInspector : SPEditor
    {
        public const string PROP_ORDER = "_order";
        public const string PROP_ACTIVATEON = "_activateOn";

        public const string PROP_TIMESUPPLIER = "_timeSupplier";
        public const string PROP_WRAPMODE = "_wrapMode";
        public const string PROP_WRAPCOUNT = "_wrapCount";
        public const string PROP_TARGET = "_target";
        public const string PROP_TWEENDATA = "_data";
        public const string PROP_ONCOMPLETE = "_onComplete";
        public const string PROP_TWEENTOKEN = "_tweenToken";

        private const string PROP_DATA_MODE = "Mode";
        private const string PROP_DATA_MEMBER = "MemberName";
        private const string PROP_DATA_EASE = "Ease";
        private const string PROP_DATA_VALUES = "ValueS";
        private const string PROP_DATA_VALUEE = "ValueE";
        private const string PROP_DATA_DUR = "Duration";
        private const string PROP_DATA_OPTION = "Option";

        private SPReorderableList _dataList;
        private SerializedProperty _targetProp;
        private VariantReferencePropertyDrawer _variantDrawer = new VariantReferencePropertyDrawer();

        protected override void OnEnable()
        {
            base.OnEnable();

            _dataList = new SPReorderableList(this.serializedObject, this.serializedObject.FindProperty(PROP_TWEENDATA));
            _dataList.drawHeaderCallback = _dataList_DrawHeader;
            _dataList.drawElementCallback = _dataList_DrawElement;
            _dataList.elementHeight = EditorGUIUtility.singleLineHeight * 7f + 7f;

        }

        protected override void OnSPInspectorGUI()
        {
            this.serializedObject.Update();

            _targetProp = this.serializedObject.FindProperty(PROP_TARGET);

            this.DrawPropertyField(EditorHelper.PROP_SCRIPT);
            this.DrawPropertyField(PROP_ORDER);
            this.DrawPropertyField(PROP_ACTIVATEON);
            this.DrawPropertyField(PROP_TIMESUPPLIER);
            SPEditorGUILayout.PropertyField(_targetProp);
            this.DrawPropertyField(PROP_WRAPMODE);
            if (this.serializedObject.FindProperty(PROP_WRAPMODE).GetEnumValue<TweenWrapMode>() != TweenWrapMode.Once)
            {
                this.DrawPropertyField(PROP_WRAPCOUNT);
            }
            this.DrawPropertyField(PROP_TWEENTOKEN);
            _dataList.DoLayoutList();
            this.DrawPropertyField(PROP_ONCOMPLETE);


            this.DrawDefaultInspectorExcept(EditorHelper.PROP_SCRIPT, PROP_ORDER, PROP_ACTIVATEON, PROP_WRAPMODE, PROP_WRAPCOUNT, PROP_TARGET, PROP_TIMESUPPLIER, PROP_TWEENDATA, PROP_ONCOMPLETE, PROP_TWEENTOKEN);

            this.serializedObject.ApplyModifiedProperties();
        }



        #region ReorderableList Handlers

        private void _dataList_DrawHeader(Rect area)
        {
            EditorGUI.LabelField(area, "Tween Data");
        }

        private void _dataList_DrawElement(Rect area, int index, bool isActive, bool isFocused)
        {
            Rect position;
            var el = _dataList.serializedProperty.GetArrayElementAtIndex(index);
            var mtp = el.GetManagedReferenceType();
            if(mtp == null)
            {
                el.managedReferenceValue = new i_Tween.GenericTweenData();
                GUI.changed = true;
                return;
            }
            else if(mtp != typeof(i_Tween.GenericTweenData))
            {
                EditorGUI.LabelField(area, "Unsupported ITweenData Type '" + mtp.Name + "' in editor.");
                return;
            }

            position = CalcNextRect(ref area);
            SPEditorGUI.PropertyField(position, el.FindPropertyRelative(PROP_DATA_MODE));

            //TODO - member
            position = CalcNextRect(ref area);
            var memberProp = el.FindPropertyRelative(PROP_DATA_MEMBER);
            System.Type targType = com.spacepuppyeditor.Core.Events.TriggerableTargetObjectPropertyDrawer.GetTargetType(_targetProp);
            object targObj = com.spacepuppyeditor.Core.Events.TriggerableTargetObjectPropertyDrawer.GetTarget(_targetProp, targType);
            System.Type propType;
            memberProp.stringValue = i_TweenValueInspector.ReflectedPropertyAndCustomTweenAccessorField(position,
                                                                                                        EditorHelper.TempContent("Property", "The property on the target to set."),
                                                                                                        targObj,
                                                                                                        memberProp.stringValue,
                                                                                                        com.spacepuppy.Dynamic.DynamicMemberAccess.ReadWrite,
                                                                                                        out propType);
            var curveGenerator = SPTween.CurveFactory.LookupTweenCurveGenerator(targObj?.GetType(), memberProp.stringValue, propType);

            position = CalcNextRect(ref area);
            SPEditorGUI.PropertyField(position, el.FindPropertyRelative(PROP_DATA_EASE));

            position = CalcNextRect(ref area);
            var propOption = el.FindPropertyRelative(PROP_DATA_OPTION);
            this.DrawOption(position, curveGenerator, propOption);

            position = CalcNextRect(ref area);
            SPEditorGUI.PropertyField(position, el.FindPropertyRelative(PROP_DATA_DUR));

            propType = curveGenerator?.GetExpectedMemberType(propOption.intValue) ?? propType;
            if (propType != null)
            {
                switch (el.FindPropertyRelative(PROP_DATA_MODE).GetEnumValue<TweenHash.AnimMode>())
                {
                    case TweenHash.AnimMode.To:
                        {
                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("To Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUES));

                            position = CalcNextRect(ref area);
                        }
                        break;
                    case TweenHash.AnimMode.From:
                        {
                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("From Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUES));
                        }
                        break;
                    case TweenHash.AnimMode.By:
                        {
                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("By Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUES));
                        }
                        break;
                    case TweenHash.AnimMode.FromTo:
                        {
                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("Start Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUES));

                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("End Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUEE));
                        }
                        break;
                    case TweenHash.AnimMode.RedirectTo:
                        {
                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("Start Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUES));

                            position = CalcNextRect(ref area);
                            this.DrawVariant(position, EditorHelper.TempContent("End Value"), propType, el.FindPropertyRelative(PROP_DATA_VALUEE));
                        }
                        break;
                }
            }


        }

        private void DrawVariant(Rect position, GUIContent label, System.Type propType, SerializedProperty valueProp)
        {
            if (com.spacepuppy.Dynamic.DynamicUtil.TypeIsVariantSupported(propType))
            {
                //draw the default variant as the method accepts anything
                _variantDrawer.RestrictVariantType = false;
                _variantDrawer.ForcedObjectType = null;
                _variantDrawer.OnGUI(position, valueProp, label);
            }
            else
            {
                _variantDrawer.RestrictVariantType = true;
                _variantDrawer.TypeRestrictedTo = propType;
                _variantDrawer.ForcedObjectType = (TypeUtil.IsType(propType, typeof(Component))) ? propType : null;
                _variantDrawer.OnGUI(position, valueProp, label);
            }
        }


        private static Rect CalcNextRect(ref Rect area)
        {
            var pos = new Rect(area.xMin, area.yMin + 1f, area.width, EditorGUIUtility.singleLineHeight);
            area = new Rect(pos.xMin, pos.yMax, area.width, area.height - EditorGUIUtility.singleLineHeight + 1f);
            return pos;
        }

        private void DrawOption(Rect position, ITweenCurveGenerator generator, SerializedProperty optionProp)
        {
            var etp = generator?.GetOptionEnumType();
            if (etp != null)
            {
                System.Enum evalue = null;
                if (!System.Enum.IsDefined(etp, optionProp.intValue))
                {
                    var arr = System.Enum.GetValues(etp) as System.Enum[];
                    evalue = arr?.FirstOrDefault();
                }
                else
                {
                    evalue = System.Enum.ToObject(etp, optionProp.intValue) as System.Enum;
                }

                if (evalue != null)
                {
                    evalue = EditorGUI.EnumPopup(position, "Option", evalue);
                    optionProp.intValue = ConvertUtil.ToInt(evalue);
                    return;
                }
            }

            optionProp.intValue = 0;
            EditorGUI.LabelField(position, "Option", "(no option available)");
        }

        #endregion


        #region Custom Reflected PropertyField

        public static string ReflectedPropertyAndCustomTweenAccessorField(Rect position, GUIContent label, object targObj, string selectedMemberName, DynamicMemberAccess access, out System.Type propType)
        {
            if (targObj != null)
            {
                //var members = DynamicUtil.GetEasilySerializedMembers(targObj, System.Reflection.MemberTypes.Field | System.Reflection.MemberTypes.Property, access).ToArray();
                var targTp = targObj.GetType();
                var members = DynamicUtil.GetEasilySerializedMembersFromType(targTp, System.Reflection.MemberTypes.Field | System.Reflection.MemberTypes.Property, access).ToArray();
                var accessors = SPTween.CurveFactory.AccessorFactory.GetCustomAccessorIds(targTp, (d) => VariantReference.AcceptableSerializableType(d.Accessor.GetMemberType()));
                System.Array.Sort(accessors);

                using (var entries = TempCollection.GetList<GUIContent>(members.Length))
                {
                    int index = -1;
                    for (int i = 0; i < members.Length; i++)
                    {
                        var m = members[i];
                        if ((DynamicUtil.GetMemberAccessLevel(m) & DynamicMemberAccess.Write) != 0)
                            entries.Add(EditorHelper.TempContent(string.Format("{0} ({1}) -> {2}", m.Name, DynamicUtil.GetReturnType(m).Name, DynamicUtil.GetValueWithMember(m, targObj))));
                        else
                            entries.Add(EditorHelper.TempContent(string.Format("{0} (readonly - {1}) -> {2}", m.Name, DynamicUtil.GetReturnType(m).Name, DynamicUtil.GetValueWithMember(m, targObj))));

                        if (index < 0 && m.Name == selectedMemberName)
                        {
                            //index = i;
                            index = entries.Count - 1;
                        }
                    }

                    for (int i = 0; i < accessors.Length; i++)
                    {
                        entries.Add(EditorHelper.TempContent(accessors[i]));
                        if (index < 0 && accessors[i] == selectedMemberName)
                        {
                            index = entries.Count - 1;
                        }
                    }


                    index = EditorGUI.Popup(position, label, index, entries.ToArray());
                    //selectedMember = (index >= 0) ? members[index] : null;
                    //return (selectedMember != null) ? selectedMember.Name : null;

                    if (index < 0)
                    {
                        propType = null;
                        return null;
                    }
                    else if (index < members.Length)
                    {
                        propType = DynamicUtil.GetReturnType(members[index]);
                        return members[index].Name;
                    }
                    else
                    {
                        var nm = accessors[index - members.Length];
                        TweenCurveFactory.SpecialNameAccessorInfo info;
                        if (SPTween.CurveFactory.AccessorFactory.TryGetMemberAccessorInfoByType(targTp, nm, out info))
                        {
                            propType = info.Accessor.GetMemberType();
                            if (VariantReference.AcceptableSerializableType(propType))
                            {
                                return nm;
                            }
                        }
                    }

                    propType = null;
                    return null;
                }
            }
            else
            {
                propType = null;
                EditorGUI.Popup(position, label, -1, new GUIContent[0]);
                return null;
            }
        }

        #endregion

    }

}
