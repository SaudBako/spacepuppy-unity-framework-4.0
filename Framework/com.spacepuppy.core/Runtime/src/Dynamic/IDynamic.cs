﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Linq;

using com.spacepuppy.Utils;

namespace com.spacepuppy.Dynamic
{

    public interface IDynamic
    {
        bool SetValue(string sMemberName, object value, params object[] index);
        bool TryGetValue(string sMemberName, out object result, params object[] args);
        object InvokeMethod(string sMemberName, params object[] args);

        bool HasMember(string sMemberName, bool includeNonPublic);
        IEnumerable<string> GetMemberNames(bool includeNonPublic);
        IEnumerable<MemberInfo> GetMembers(bool includeNonPublic);
        MemberInfo GetMember(string sMemberName, bool includeNonPublic);

    }

    [System.Flags()]
    public enum DynamicMemberAccess
    {
        Inaccessible = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }

    public static class DynamicUtil
    {

        private static IDynamicAccessorImp _accessor = StandardDynamicAccessorImp.Default;
        public static IDynamicAccessorImp Accessor
        {
            get { return _accessor; }
            set { _accessor = value ?? StandardDynamicAccessorImp.Default; }
        }

        #region IDynamic Methods

        public static bool SetValue(this object obj, string sMemberName, object value)
        {
            if (obj is IDynamic d)
            {
                try
                {
                    return d.SetValue(sMemberName, value, (object[])null);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return _accessor.SetValue(obj, sMemberName, value);
            }
        }

        public static bool SetValue(this object obj, string sMemberName, object value, params object[] index)
        {
            if (obj is IDynamic d)
            {
                try
                {
                    return d.SetValue(sMemberName, value, index);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return _accessor.SetValue(obj, sMemberName, value, index);
            }
        }

        public static bool SetValue<T>(this object obj, string sMemberName, T value)
        {
            if (obj is IDynamic d)
            {
                return d.SetValue(sMemberName, value);
            }
            else
            {
                return _accessor.SetValue<T>(obj, sMemberName, value);
            }
        }

        public static bool SetValue(this object obj, MemberInfo member, object value)
        {
            return _accessor.SetValue(obj, member, value);
        }

        public static bool SetValue(this object obj, MemberInfo member, object value, params object[] index)
        {
            return _accessor.SetValue(obj, member, value, index);
        }

        public static bool SetValue<T>(this object obj, MemberInfo member, T value)
        {
            return _accessor.SetValue<T>(obj, member, value);
        }

        public static object GetValue(this IDynamic obj, string sMemberName, params object[] args)
        {
            object result;
            obj.TryGetValue(sMemberName, out result, args);
            return result;
        }

        public static object GetValue(this object obj, string sMemberName, params object[] args)
        {
            object result;
            if (obj is IDynamic d)
            {
                d.TryGetValue(sMemberName, out result, args);
            }
            else
            {
                _accessor.TryGetValue(obj, sMemberName, out result, args);
            }
            return result;
        }

        public static T GetValue<T>(this object obj, string sMemberName, params object[] args)
        {
            if (obj is IDynamic d)
            {
                return ConvertUtil.Coerce<T>(d.GetValue(sMemberName, args));
            }
            else
            {
                T result;
                _accessor.TryGetValue<T>(obj, sMemberName, out result, args);
                return result;
            }
        }

        public static object GetValue(this object obj, MemberInfo member, params object[] args)
        {
            object result;
            _accessor.TryGetValue(obj, member, out result, args);
            return result;
        }

        public static T GetValue<T>(this object obj, MemberInfo member, params object[] args)
        {
            T result;
            _accessor.TryGetValue<T>(obj, member, out result, args);
            return result;
        }

        public static bool TryGetValue(this object obj, string sMemberName, out object result, params object[] args)
        {
            if (obj is IDynamic d)
            {
                return d.TryGetValue(sMemberName, out result, args);
            }
            else
            {
                return _accessor.TryGetValue(obj, sMemberName, out result, args);
            }
        }

        public static bool TryGetValue<T>(this object obj, string sMemberName, out T result, params object[] args)
        {
            if (obj is IDynamic d)
            {
                object temp;
                if (d.TryGetValue(sMemberName, out temp, args))
                {
                    result = ConvertUtil.Coerce<T>(temp);
                    return true;
                }
                else
                {
                    result = default(T);
                    return false;
                }
            }
            else
            {
                return _accessor.TryGetValue<T>(obj, sMemberName, out result, args);
            }
        }

        public static bool TryGetValue(this object obj, MemberInfo member, out object result, params object[] args)
        {
            return _accessor.TryGetValue(obj, member, out result, args);
        }

        public static bool TryGetValue<T>(this object obj, MemberInfo member, out T result, params object[] args)
        {
            return _accessor.TryGetValue<T>(obj, member, out result, args);
        }

        public static object InvokeMethod(object obj, string name, params object[] args)
        {
            if (obj is IDynamic d)
            {
                return d.InvokeMethod(name, args);
            }
            else
            {
                return _accessor.InvokeMethod(obj, name, args);
            }
        }

        public static bool HasMember(object obj, string name, bool includeNonPublic)
        {
            if (obj == null) return false;

            if (obj is IDynamic)
            {
                return (obj as IDynamic).HasMember(name, includeNonPublic);
            }
            else
            {
                return TypeHasMember(obj.GetType(), name, includeNonPublic);
            }
        }

        public static IEnumerable<MemberInfo> GetMembers(object obj, bool includeNonPublic)
        {
            if (obj == null) return Enumerable.Empty<MemberInfo>();

            if (obj is IDynamic)
            {
                return (obj as IDynamic).GetMembers(includeNonPublic);
            }
            else
            {
                return GetMembersFromType(obj.GetType(), includeNonPublic);
            }
        }

        public static IEnumerable<MemberInfo> GetMembers(object obj, bool includeNonPublic, MemberTypes mask)
        {
            if (obj == null) return Enumerable.Empty<MemberInfo>();

            if (obj is IDynamic)
            {
                return FilterMembers((obj as IDynamic).GetMembers(includeNonPublic), mask);
            }
            else
            {
                return GetMembersFromType(obj.GetType(), includeNonPublic, mask);
            }
        }

        public static IEnumerable<string> GetMemberNames(object obj, bool includeNonPublic)
        {
            if (obj == null) return Enumerable.Empty<string>();

            if (obj is IDynamic)
            {
                return (obj as IDynamic).GetMemberNames(includeNonPublic);
            }
            else
            {
                return GetMemberNamesFromType(obj.GetType(), includeNonPublic);
            }
        }

        public static IEnumerable<string> GetMemberNames(object obj, bool includeNonPublic, MemberTypes mask)
        {
            if (obj == null) return Enumerable.Empty<string>();

            if (obj is IDynamic)
            {
                return (from m in (obj as IDynamic).GetMembers(includeNonPublic) where (m.MemberType & mask) != 0 select m.Name);
            }
            else
            {
                return GetMemberNamesFromType(obj.GetType(), includeNonPublic, mask);
            }
        }

        public static MemberInfo GetMember(object obj, string sMemberName, bool includeNonPublic)
        {
            if (obj == null) return null;

            if (obj is IDynamic)
            {
                return (obj as IDynamic).GetMember(sMemberName, includeNonPublic);
            }
            else
            {
                return GetMemberFromType(obj.GetType(), sMemberName, includeNonPublic);
            }
        }


        public static object GetValueRecursively(this object obj, string sprop)
        {
            if (sprop.Contains('.'))
                obj = DynamicUtil.ReduceSubObject(obj, sprop, out sprop);

            return GetValue(obj, sprop);
        }

        #endregion

        #region Direct Reflection

        public static bool SetValueDirect(object obj, string sprop, object value)
        {
            return SetValueDirect(obj, sprop, value, (object[])null);
        }

        public static bool SetValueDirect(object obj, string sprop, object value, params object[] index)
        {
            if (string.IsNullOrEmpty(sprop)) return false;

            //if (sprop.Contains('.'))
            //    obj = DynamicUtil.ReduceSubObject(obj, sprop, out sprop);
            if (obj == null) return false;

            try
            {
                var vtp = (value != null) ? value.GetType() : null;
                var member = GetValueSetterMemberFromType(obj.GetType(), sprop, vtp, true);
                if (member != null)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Field:
                            (member as FieldInfo).SetValue(obj, value);
                            return true;
                        case MemberTypes.Property:
                            (member as PropertyInfo).SetValue(obj, value, index);
                            return true;
                        case MemberTypes.Method:
                            var arr = ArrayUtil.Temp(value);
                            (member as MethodInfo).Invoke(obj, arr);
                            ArrayUtil.ReleaseTemp(arr);
                            return true;
                    }
                }

                if (vtp != null)
                {
                    member = GetValueSetterMemberFromType(obj.GetType(), sprop, null, true);
                    if (member != null)
                    {
                        var rtp = GetReturnType(member);
                        object cobj = null;
                        if (ConvertUtil.TryToPrim(value, rtp, out cobj))
                            value = cobj;

                        switch (member.MemberType)
                        {
                            case MemberTypes.Field:
                                (member as FieldInfo).SetValue(obj, value);
                                return true;
                            case MemberTypes.Property:
                                (member as PropertyInfo).SetValue(obj, value, index);
                                return true;
                            case MemberTypes.Method:
                                var arr = ArrayUtil.Temp(value);
                                (member as MethodInfo).Invoke(obj, arr);
                                ArrayUtil.ReleaseTemp(arr);
                                return true;
                        }
                    }
                }
            }
            catch
            {

            }

            return false;
        }

        public static bool SetValueDirect(object obj, MemberInfo member, object value)
        {
            return SetValueDirect(obj, member, value, (object[])null);
        }

        public static bool SetValueDirect(object obj, MemberInfo member, object value, params object[] index)
        {
            if (obj == null) return false;

            if (member == null) return false;

            try
            {
                switch (member.MemberType)
                {
                    case MemberTypes.Field:
                        (member as FieldInfo).SetValue(obj, value);
                        return true;
                    case MemberTypes.Property:
                        if ((member as PropertyInfo).CanWrite)
                        {
                            (member as PropertyInfo).SetValue(obj, value, index);
                            return true;
                        }
                        else
                            return false;
                    case MemberTypes.Method:
                        var arr = ArrayUtil.Temp(value);
                        (member as MethodInfo).Invoke(obj, arr);
                        ArrayUtil.ReleaseTemp(arr);
                        return true;
                }
            }
            catch
            {

            }

            return false;
        }

        public static object GetValueDirect(object obj, string sprop, params object[] args)
        {
            if (string.IsNullOrEmpty(sprop)) return null;

            //if (sprop != null && sprop.Contains('.')) obj = DynamicUtil.ReduceSubObject(obj, sprop, out sprop);
            if (obj == null) return null;

            try
            {
                var tp = obj.GetType();
                foreach (var member in GetMembersFromType(tp, sprop, true))
                {
                    switch (member.MemberType)
                    {
                        case System.Reflection.MemberTypes.Field:
                            var field = member as System.Reflection.FieldInfo;
                            return field.GetValue(obj);

                        case System.Reflection.MemberTypes.Property:
                            {
                                var prop = member as System.Reflection.PropertyInfo;
                                var paramInfos = prop.GetIndexParameters();
                                if (prop.CanRead && DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                                {
                                    return prop.GetValue(obj, args);
                                }
                                break;
                            }
                        case System.Reflection.MemberTypes.Method:
                            {
                                var meth = member as System.Reflection.MethodInfo;
                                var paramInfos = meth.GetParameters();
                                if (DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                                {
                                    return meth.Invoke(obj, args);
                                }
                                break;
                            }
                    }
                }

                //unstrict
                foreach (var member in GetMembersFromType(tp, sprop, true))
                {
                    switch (member.MemberType)
                    {
                        case System.Reflection.MemberTypes.Field:
                            var field = member as System.Reflection.FieldInfo;
                            return field.GetValue(obj);

                        case System.Reflection.MemberTypes.Property:
                            {
                                var prop = member as System.Reflection.PropertyInfo;
                                var paramInfos = prop.GetIndexParameters();
                                if (prop.CanRead && DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                                {
                                    return prop.GetValue(obj, args);
                                }
                                break;
                            }
                        case System.Reflection.MemberTypes.Method:
                            {
                                var meth = member as System.Reflection.MethodInfo;
                                var paramInfos = meth.GetParameters();
                                if (DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                                {
                                    return meth.Invoke(obj, args);
                                }
                                break;
                            }
                    }
                }
            }
            catch
            {

            }

            return null;
        }

        public static object GetValueDirect(this object obj, MemberInfo member, params object[] args)
        {
            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Field:
                    var field = member as System.Reflection.FieldInfo;
                    return field.GetValue(obj);

                case System.Reflection.MemberTypes.Property:
                    {
                        var prop = member as System.Reflection.PropertyInfo;
                        var paramInfos = prop.GetIndexParameters();
                        if (prop.CanRead && DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                        {
                            return prop.GetValue(obj, args);
                        }
                        break;
                    }
                case System.Reflection.MemberTypes.Method:
                    {
                        var meth = member as System.Reflection.MethodInfo;
                        var paramInfos = meth.GetParameters();
                        if (DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                        {
                            return meth.Invoke(obj, args);
                        }
                        break;
                    }
            }

            //unstrict
            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Field:
                    var field = member as System.Reflection.FieldInfo;
                    return field.GetValue(obj);

                case System.Reflection.MemberTypes.Property:
                    {
                        var prop = member as System.Reflection.PropertyInfo;
                        var paramInfos = prop.GetIndexParameters();
                        if (prop.CanRead && DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                        {
                            return prop.GetValue(obj, args);
                        }
                        break;
                    }
                case System.Reflection.MemberTypes.Method:
                    {
                        var meth = member as System.Reflection.MethodInfo;
                        var paramInfos = meth.GetParameters();
                        if (DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                        {
                            return meth.Invoke(obj, args);
                        }
                        break;
                    }
            }

            return null;
        }

        public static bool TryGetValueDirect(object obj, string sprop, out object result, params object[] args)
        {
            result = null;
            if (string.IsNullOrEmpty(sprop)) return false;

            //if (sprop != null && sprop.Contains('.')) obj = DynamicUtil.ReduceSubObject(obj, sprop, out sprop);
            if (obj == null) return false;

            try
            {
                var tp = obj.GetType();
                foreach (var member in GetMembersFromType(tp, sprop, true))
                {
                    switch (member.MemberType)
                    {
                        case System.Reflection.MemberTypes.Field:
                            var field = member as System.Reflection.FieldInfo;
                            result = field.GetValue(obj);
                            return true;
                        case System.Reflection.MemberTypes.Property:
                            {
                                var prop = member as System.Reflection.PropertyInfo;
                                var paramInfos = prop.GetIndexParameters();
                                if (prop.CanRead && DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                                {
                                    result = prop.GetValue(obj, args);
                                    return true;
                                }
                                break;
                            }
                        case System.Reflection.MemberTypes.Method:
                            {
                                var meth = member as System.Reflection.MethodInfo;
                                var paramInfos = meth.GetParameters();
                                if (DynamicUtil.ParameterSignatureMatches(args, paramInfos, false))
                                {
                                    result = meth.Invoke(obj, args);
                                    return true;
                                }
                                break;
                            }
                    }
                }

                //unstrict
                foreach (var member in GetMembersFromType(tp, sprop, true))
                {
                    switch (member.MemberType)
                    {
                        case System.Reflection.MemberTypes.Field:
                            var field = member as System.Reflection.FieldInfo;
                            result = field.GetValue(obj);
                            return true;
                        case System.Reflection.MemberTypes.Property:
                            {
                                var prop = member as System.Reflection.PropertyInfo;
                                var paramInfos = prop.GetIndexParameters();
                                if (prop.CanRead && DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                                {
                                    result = prop.GetValue(obj, args);
                                    return true;
                                }
                                break;
                            }
                        case System.Reflection.MemberTypes.Method:
                            {
                                var meth = member as System.Reflection.MethodInfo;
                                var paramInfos = meth.GetParameters();
                                if (DynamicUtil.ParameterSignatureMatchesNumericallyUnstrict(args, paramInfos, false, true))
                                {
                                    result = meth.Invoke(obj, args);
                                    return true;
                                }
                                break;
                            }
                    }
                }
            }
            catch
            {

            }

            return false;
        }

        public static object InvokeMethodDirect(object obj, string name, params object[] args)
        {
            const BindingFlags BINDING = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                         BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;
            if (string.IsNullOrEmpty(name)) return null;
            //if (name.Contains('.'))
            //    obj = DynamicUtil.ReduceSubObject(obj, name, out name);
            if (obj == null) return false;

            var tp = obj.GetType();
            try
            {
                //return tp.InvokeMember(name, BINDING, System.Type.DefaultBinder, obj, args);

                var methods = DynamicUtil.GetMembersFromType(obj.GetType(), true, MemberTypes.Method).OfType<MethodBase>().Where(m => m.Name == name).ToArray();
                object state;
                var method = DynamicBinder.Default.BindToMethod(BINDING, methods, ref args, null, CultureInfo.CurrentCulture, null, out state);
                if (method != null)
                {
                    return method.Invoke(obj, args);
                }
            }
            catch
            {
            }

            return null;
        }

        public static bool HasMemberDirect(object obj, string name, bool includeNonPublic)
        {
            if (obj == null) return false;
            if (string.IsNullOrEmpty(name)) return false;

            return TypeHasMember(obj.GetType(), name, includeNonPublic);
        }

        public static IEnumerable<MemberInfo> GetMembersDirect(object obj, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            if (obj == null) return Enumerable.Empty<MemberInfo>();

            return GetMembersFromType(obj.GetType(), includeNonPublic, mask);
        }

        public static IEnumerable<MemberInfo> GetMembersDirect(object obj, string name, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            if (obj == null) return Enumerable.Empty<MemberInfo>();

            return GetMembersFromType(obj.GetType(), name, includeNonPublic, mask);
        }

        public static IEnumerable<string> GetMemberNamesDirect(object obj, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            if (obj == null) return Enumerable.Empty<string>();

            return GetMemberNamesFromType(obj.GetType(), includeNonPublic, mask);
        }

        public static MemberInfo GetMemberDirect(object obj, string sMemberName, bool includeNonPublic)
        {
            if (obj == null) return null;

            return GetMemberFromType(obj.GetType(), sMemberName, includeNonPublic);
        }




        public static bool TypeHasMember(System.Type tp, string name, bool includeNonPublic)
        {
            const BindingFlags BINDING = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            //if (name.Contains('.'))
            //    tp = DynamicUtil.ReduceSubType(tp, name, includeNonPublic, out name);
            if (tp == null) return false;

            var member = tp.GetMember(name, BINDING);
            if (member != null && member.Length > 0) return true;


            if (tp.IsInterface)
            {
                foreach (var itp in tp.GetInterfaces())
                {
                    member = tp.GetMember(name, BINDING);
                    if (member != null && member.Length > 0) return true;
                }
            }
            else if (includeNonPublic)
            {
                while (tp != null)
                {
                    member = tp.GetMember(name, PRIV_BINDING);
                    if (member != null && member.Length > 0) return true;
                    tp = tp.BaseType;
                }
            }
            return false;
        }

        public static IEnumerable<MemberInfo> EnumerateAllMembers(System.Type tp, BindingFlags binding)
        {
            foreach (var m in tp.GetMembers(binding & ~BindingFlags.NonPublic))
            {
                yield return m;
            }

            if (tp.IsInterface)
            {
                foreach (var itp in tp.GetInterfaces())
                {
                    foreach (var m in itp.GetMembers(binding & ~BindingFlags.NonPublic))
                    {
                        yield return m;
                    }
                }
            }
            else if ((binding & BindingFlags.NonPublic) != 0)
            {
                binding = binding & ~BindingFlags.Public;

                while (tp != null)
                {
                    foreach (var m in tp.GetMembers(binding))
                    {
                        yield return m;
                    }
                    tp = tp.BaseType;
                }
            }
        }

        public static IEnumerable<MemberInfo> GetMembersFromType(System.Type tp, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            const BindingFlags BINDING = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) yield break;

            var arr = tp.GetMembers(BINDING);
            foreach (var m in arr)
            {
                if ((m.MemberType & mask) != 0)
                {
                    yield return m;
                }
            }


            if (tp.IsInterface)
            {
                foreach (var itp in tp.GetInterfaces())
                {
                    foreach (var m in itp.GetMembers(BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m;
                        }
                    }
                }
            }
            else if (includeNonPublic)
            {
                while (tp != null)
                {
                    foreach (var m in tp.GetMembers(PRIV_BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m;
                        }
                    }
                    tp = tp.BaseType;
                }
            }
        }

        public static IEnumerable<MemberInfo> GetMembersFromType(System.Type tp, string name, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            const BindingFlags BINDING = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) yield break;

            foreach (var m in tp.GetMember(name, BINDING))
            {
                if ((m.MemberType & mask) != 0)
                {
                    yield return m;
                }
            }


            if (tp.IsInterface)
            {
                foreach (var itp in tp.GetInterfaces())
                {
                    foreach (var m in itp.GetMembers(BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m;
                        }
                    }
                }
            }
            else if (includeNonPublic)
            {
                while (tp != null)
                {
                    foreach (var m in tp.GetMember(name, PRIV_BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m;
                        }
                    }
                    tp = tp.BaseType;
                }
            }
        }

        public static IEnumerable<string> GetMemberNamesFromType(System.Type tp, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            const BindingFlags BINDING = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) yield break;

            foreach (var m in tp.GetMembers(BINDING))
            {
                if ((m.MemberType & mask) != 0)
                {
                    yield return m.Name;
                }
            }

            if (tp.IsInterface)
            {
                foreach (var itp in tp.GetInterfaces())
                {
                    foreach (var m in itp.GetMembers(BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m.Name;
                        }
                    }
                }
            }
            else if (includeNonPublic)
            {
                while (tp != null)
                {
                    foreach (var m in tp.GetMembers(PRIV_BINDING))
                    {
                        if ((m.MemberType & mask) != 0)
                        {
                            yield return m.Name;
                        }
                    }
                    tp = tp.BaseType;
                }
            }
        }



        public static MemberInfo GetMemberFromType(Type tp, string sMemberName, bool includeNonPublic, MemberTypes mask = MemberTypes.Field | MemberTypes.Property | MemberTypes.Method)
        {
            const BindingFlags BINDING_PUBLIC = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) throw new ArgumentNullException("tp");

            //if (sMemberName.Contains('.'))
            //{
            //    tp = DynamicUtil.ReduceSubType(tp, sMemberName, includeNonPublic, out sMemberName);
            //    if (tp == null) return null;
            //}

            try
            {
                MemberInfo[] members;

                members = tp.GetMember(sMemberName, BINDING_PUBLIC);
                foreach (var member in members)
                {
                    if ((member.MemberType & mask) != 0) return member;
                }

                if (tp.IsInterface)
                {
                    foreach (var itp in tp.GetInterfaces())
                    {
                        members = tp.GetMember(sMemberName, BINDING_PUBLIC);
                        foreach (var member in members)
                        {
                            if ((member.MemberType & mask) != 0) return member;
                        }
                    }
                }
                else if (includeNonPublic)
                {
                    while (tp != null)
                    {
                        members = tp.GetMember(sMemberName, PRIV_BINDING);
                        tp = tp.BaseType;
                        if (members == null || members.Length == 0) continue;

                        foreach (var member in members)
                        {
                            if ((member.MemberType & mask) != 0) return member;
                        }
                    }
                }
            }
            catch
            {

            }
            return null;
        }

        public static MemberInfo GetValueMemberFromType(Type tp, string sprop, bool includeNonPublic)
        {
            const BindingFlags BINDING_PUBLIC = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) throw new ArgumentNullException("tp");

            //if (sprop.Contains('.'))
            //{
            //    tp = DynamicUtil.ReduceSubType(tp, sprop, includeNonPublic, out sprop);
            //    if (tp == null) return null;
            //}

            try
            {
                MemberInfo[] members;

                members = tp.GetMember(sprop, BINDING_PUBLIC);
                foreach (var member in members)
                {
                    if (IsValidValueMember(member)) return member;
                }

                if (tp.IsInterface)
                {
                    foreach (var itp in tp.GetInterfaces())
                    {
                        members = tp.GetMember(sprop, BINDING_PUBLIC);
                        foreach (var member in members)
                        {
                            if (IsValidValueMember(member)) return member;
                        }
                    }
                }
                else if (includeNonPublic)
                {
                    while (tp != null)
                    {
                        members = tp.GetMember(sprop, PRIV_BINDING);
                        tp = tp.BaseType;
                        if (members == null || members.Length == 0) continue;

                        foreach (var member in members)
                        {
                            if (IsValidValueMember(member)) return member;
                        }
                    }
                }
            }
            catch
            {

            }
            return null;
        }

        public static MemberInfo GetValueSetterMemberFromType(Type tp, string sprop, Type valueType, bool includeNonPublic)
        {
            const BindingFlags BINDING_PUBLIC = BindingFlags.Public | BindingFlags.Instance;
            const BindingFlags PRIV_BINDING = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if (tp == null) throw new ArgumentNullException("tp");

            //if (sprop.Contains('.'))
            //{
            //    tp = DynamicUtil.ReduceSubType(tp, sprop, includeNonPublic, out sprop);
            //    if (tp == null) return null;
            //}

            try
            {
                MemberInfo[] members;

                //first strict test
                members = tp.GetMember(sprop, BINDING_PUBLIC);
                foreach (var member in members)
                {
                    if (IsValidValueSetterMember(member, valueType)) return member;
                }

                if (tp.IsInterface)
                {
                    foreach (var itp in tp.GetInterfaces())
                    {
                        members = tp.GetMember(sprop, BINDING_PUBLIC);
                        foreach (var member in members)
                        {
                            if (IsValidValueSetterMember(member, valueType)) return member;
                        }
                    }
                }
                else if (includeNonPublic)
                {
                    while (tp != null)
                    {
                        members = tp.GetMember(sprop, PRIV_BINDING);
                        tp = tp.BaseType;
                        if (members == null || members.Length == 0) continue;

                        foreach (var member in members)
                        {
                            if (IsValidValueSetterMember(member, valueType)) return member;
                        }
                    }
                }
            }
            catch
            {

            }

            return null;
        }



        [System.Obsolete("Poorly named method and return type. Use GetDynamicParameterInfo instead.")]
        public static System.Type[] GetParameters(MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return new System.Type[] { (info as FieldInfo).FieldType };
                case MemberTypes.Property:
                    return new System.Type[] { (info as PropertyInfo).PropertyType };
                case MemberTypes.Method:
                    {
                        var paramInfos = (info as MethodBase).GetParameters();
                        Type[] arr = new Type[paramInfos.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = paramInfos[i].ParameterType;
                        }
                        return arr;
                    }
                default:
                    return ArrayUtil.Empty<System.Type>();
            }
        }

        public static DynamicParameterInfo[] GetDynamicParameterInfo(MemberInfo info)
        {
            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return new DynamicParameterInfo[] { new DynamicParameterInfo(info, info.Name, (info as FieldInfo).FieldType) };
                case MemberTypes.Property:
                    return new DynamicParameterInfo[] { new DynamicParameterInfo(info, info.Name, (info as PropertyInfo).PropertyType) };
                case MemberTypes.Method:
                    {
                        var paramInfos = (info as MethodBase).GetParameters();
                        DynamicParameterInfo[] arr = new DynamicParameterInfo[paramInfos.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = new DynamicParameterInfo(paramInfos[i]);
                        }
                        return arr;
                    }
                default:
                    return ArrayUtil.Empty<DynamicParameterInfo>();
            }
        }


        public static Type GetReturnType(MemberInfo info)
        {
            if (info == null) return null;

            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (info as PropertyInfo).PropertyType;
                case MemberTypes.Method:
                    return (info as MethodInfo).ReturnType;
            }
            return null;
        }

        /// <summary>
        /// If the member is writeable, returns the the type it expects.
        /// Field - type of the field
        /// Property - type of the property
        /// Method - type of the first parameter, if any, otherwise null.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static Type GetInputType(MemberInfo info)
        {
            if (info == null) return null;

            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return (info as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (info as PropertyInfo).PropertyType;
                case MemberTypes.Method:
                    {
                        var meth = info as MethodInfo;
                        if (meth == null) return null;
                        var arr = meth.GetParameters();
                        if (arr.Length == 0) return null;
                        return arr[0].ParameterType;
                    }
            }
            return null;
        }

        public static DynamicMemberAccess GetMemberAccessLevel(MemberInfo info)
        {
            if (info == null) return DynamicMemberAccess.Inaccessible;

            switch (info.MemberType)
            {
                case MemberTypes.Field:
                    return DynamicMemberAccess.ReadWrite;
                case MemberTypes.Property:
                    var pinfo = info as PropertyInfo;
                    if (pinfo.CanRead)
                        return (pinfo.CanWrite) ? DynamicMemberAccess.ReadWrite : DynamicMemberAccess.Read;
                    else
                        return (pinfo.CanWrite) ? DynamicMemberAccess.Write : DynamicMemberAccess.Inaccessible;
                case MemberTypes.Method:
                    var minfo = info as MethodInfo;
                    if (minfo.ReturnType != typeof(void))
                        return DynamicMemberAccess.ReadWrite;
                    else
                        return DynamicMemberAccess.Write;
                default:
                    return DynamicMemberAccess.Inaccessible;
            }
        }

        public static object GetValueWithMember(MemberInfo info, object targObj)
        {
            if (info == null) return null;

            try
            {
                switch (info.MemberType)
                {
                    case MemberTypes.Field:
                        return (info as FieldInfo).GetValue(targObj);
                    case MemberTypes.Property:
                        return (info as PropertyInfo).GetValue(targObj, null);
                    case MemberTypes.Method:
                        return (info as MethodInfo).Invoke(targObj, null);
                }
            }
            catch
            {

            }

            return null;
        }

        public static object GetValueWithMember(MemberInfo info, object targObj, bool ignoreMethod)
        {
            if (info == null || targObj == null) return null;
            if (!info.DeclaringType.IsInstanceOfType(targObj)) return null;

            try
            {
                switch (info.MemberType)
                {
                    case MemberTypes.Field:
                        return (info as FieldInfo).GetValue(targObj);
                    case MemberTypes.Property:
                        return (info as PropertyInfo).GetValue(targObj, null);
                    case MemberTypes.Method:
                        if (ignoreMethod)
                            return null;
                        else
                            return (info as MethodInfo).Invoke(targObj, null);
                }
            }
            catch
            {

            }

            return null;
        }


        /// <summary>
        /// Returns members that return/accept types that are considered easily serializable. 
        /// Easily serialized types are those that can be referenced by a VariantReference.
        /// </summary>
        /// <param name="obj">Object to find member of</param>
        /// <param name="mask">MemberType mask</param>
        /// <param name="access">Access mask</param>
        /// <returns></returns>
        public static IEnumerable<System.Reflection.MemberInfo> GetEasilySerializedMembers(object obj, MemberTypes mask = MemberTypes.All, DynamicMemberAccess access = DynamicMemberAccess.ReadWrite, bool ignoreObsoleteMembers = true)
        {
            if (obj == null) yield break;

            bool bRead = (access & DynamicMemberAccess.Read) != 0;
            bool bWrite = (access & DynamicMemberAccess.Write) != 0;
            var members = com.spacepuppy.Dynamic.DynamicUtil.GetMembers(obj, false, mask);
            foreach (var mi in members)
            {
                if ((mi.MemberType & mask) == 0) continue;
                if (ignoreObsoleteMembers && mi.IsObsolete()) continue;

                //if ((mi.DeclaringType.IsAssignableFrom(typeof(UnityEngine.MonoBehaviour)) ||
                //     mi.DeclaringType.IsAssignableFrom(typeof(SPComponent))) && mi.Name != "enabled") continue;
                if (mi.DeclaringType.IsAssignableFrom(typeof(UnityEngine.MonoBehaviour)) && mi.Name != "enabled") continue;

                switch (mi.MemberType)
                {
                    case System.Reflection.MemberTypes.Method:
                        {
                            var m = mi as System.Reflection.MethodInfo;
                            if (m.IsSpecialName) continue;
                            if (m.IsGenericMethod) continue;

                            var parr = m.GetParameters();
                            if (parr.Length == 0)
                            {
                                yield return m;
                            }
                            else
                            {
                                bool pass = true;
                                foreach (var p in parr)
                                {
                                    if (!(VariantReference.AcceptableSerializableType(p.ParameterType) || p.ParameterType == typeof(object)))
                                    {
                                        pass = false;
                                        break;
                                    }
                                }
                                if (pass) yield return m;
                            }
                        }
                        break;
                    case System.Reflection.MemberTypes.Field:
                        {
                            var f = mi as System.Reflection.FieldInfo;
                            if (f.IsSpecialName) continue;

                            if (VariantReference.AcceptableSerializableType(f.FieldType)) yield return f;
                        }
                        break;
                    case System.Reflection.MemberTypes.Property:
                        {
                            var p = mi as System.Reflection.PropertyInfo;
                            if (p.IsSpecialName) continue;
                            if (!p.CanRead && bRead) continue;
                            if (!p.CanWrite && bWrite) continue;
                            if (p.GetIndexParameters().Length > 0) continue; //indexed properties are not allowed

                            if (VariantReference.AcceptableSerializableType(p.PropertyType)) yield return p;
                        }
                        break;
                }

            }
        }

        public static IEnumerable<System.Reflection.MemberInfo> GetEasilySerializedMembersFromType(System.Type tp, MemberTypes mask = MemberTypes.All, DynamicMemberAccess access = DynamicMemberAccess.ReadWrite, bool ignoreObsoleteMembers = true)
        {
            if (tp == null) yield break;

            bool bRead = (access & DynamicMemberAccess.Read) != 0;
            bool bWrite = (access & DynamicMemberAccess.Write) != 0;
            var members = com.spacepuppy.Dynamic.DynamicUtil.GetMembersFromType(tp, false, mask);
            foreach (var mi in members)
            {
                if ((mi.MemberType & mask) == 0) continue;
                if (ignoreObsoleteMembers && mi.IsObsolete()) continue;

                //if ((mi.DeclaringType.IsAssignableFrom(typeof(UnityEngine.MonoBehaviour)) ||
                //     mi.DeclaringType.IsAssignableFrom(typeof(SPComponent))) && mi.Name != "enabled") continue;
                if (mi.DeclaringType.IsAssignableFrom(typeof(UnityEngine.MonoBehaviour)) && mi.Name != "enabled") continue;

                switch (mi.MemberType)
                {
                    case System.Reflection.MemberTypes.Method:
                        {
                            var m = mi as System.Reflection.MethodInfo;
                            if (m.IsSpecialName) continue;
                            if (m.IsGenericMethod) continue;

                            var parr = m.GetParameters();
                            if (parr.Length == 0)
                            {
                                yield return m;
                            }
                            else
                            {
                                bool pass = true;
                                foreach (var p in parr)
                                {
                                    if (!(VariantReference.AcceptableSerializableType(p.ParameterType) || p.ParameterType == typeof(object)))
                                    {
                                        pass = false;
                                        break;
                                    }
                                }
                                if (pass) yield return m;
                            }
                        }
                        break;
                    case System.Reflection.MemberTypes.Field:
                        {
                            var f = mi as System.Reflection.FieldInfo;
                            if (f.IsSpecialName) continue;

                            if (VariantReference.AcceptableSerializableType(f.FieldType)) yield return f;
                        }
                        break;
                    case System.Reflection.MemberTypes.Property:
                        {
                            var p = mi as System.Reflection.PropertyInfo;
                            if (p.IsSpecialName) continue;
                            if (!p.CanRead && bRead) continue;
                            if (!p.CanWrite && bWrite) continue;
                            if (p.GetIndexParameters().Length > 0) continue; //indexed properties are not allowed

                            if (VariantReference.AcceptableSerializableType(p.PropertyType)) yield return p;
                        }
                        break;
                }

            }
        }

        /// <summary>
        /// Returns true if the type is either System.Object or Variant.
        /// </summary>
        /// <param name="tp"></param>
        /// <returns></returns>
        public static bool TypeIsVariantSupported(System.Type tp)
        {
            return tp == typeof(object) || tp == typeof(Variant);
        }

        #endregion

        #region Some Minor Helpers

        private static object ReduceSubObject(object obj, string sprop, out string lastProp)
        {
            if (obj == null)
            {
                lastProp = null;
                return null;
            }

            var arr = sprop.Split('.');
            lastProp = arr[arr.Length - 1];
            for (int i = 0; i < arr.Length - 1; i++)
            {
                obj = DynamicUtil.GetValue(obj, arr[i]);
                if (obj == null) return null;
            }

            return obj;
        }

        private static System.Type ReduceSubType(System.Type tp, string sprop, bool includeNonPublic, out string lastProp)
        {
            if (tp == null)
            {
                lastProp = null;
                return null;
            }

            var arr = sprop.Split('.');
            lastProp = arr[arr.Length - 1];
            for (int i = 0; i < arr.Length - 1; i++)
            {
                var member = DynamicUtil.GetValueMemberFromType(tp, arr[i], includeNonPublic);
                if (member == null) return null;

                tp = GetReturnType(member);
                if (tp == null) return null;
            }

            return tp;
        }

        private static bool ParameterSignatureMatches(object[] args, ParameterInfo[] paramInfos, bool allowOptional)
        {
            if (args == null) args = ArrayUtil.Empty<object>();
            if (paramInfos == null) ArrayUtil.Empty<ParameterInfo>();

            if (args.Length == 0 && paramInfos.Length == 0) return true;
            if (args.Length > paramInfos.Length) return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    continue;
                }
                else if (paramInfos[i].ParameterType.IsInstanceOfType(args[i]))
                {
                    continue;
                }

                return false;
            }

            return paramInfos.Length == args.Length || (allowOptional && paramInfos[args.Length].IsOptional);
        }

        private static bool ParameterSignatureMatchesNumericallyUnstrict(object[] args, ParameterInfo[] paramInfos, bool allowOptional, bool convertArgsOnSuccess)
        {
            if (args == null) args = ArrayUtil.Empty<object>();
            if (paramInfos == null) ArrayUtil.Empty<ParameterInfo>();

            if (args.Length == 0 && paramInfos.Length == 0) return true;
            if (args.Length > paramInfos.Length) return false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == null)
                {
                    continue;
                }
                else if (paramInfos[i].ParameterType.IsInstanceOfType(args[i]))
                {
                    continue;
                }
                else if (ConvertUtil.IsNumericType(args[i].GetType()) && ConvertUtil.IsNumericType(paramInfos[i].ParameterType))
                {
                    continue;
                }

                return false;
            }

            if (paramInfos.Length == args.Length || (allowOptional && paramInfos[args.Length].IsOptional))
            {
                if (convertArgsOnSuccess)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == null)
                        {
                            continue;
                        }
                        else if (paramInfos[i].ParameterType.IsInstanceOfType(args[i]))
                        {
                            continue;
                        }
                        else if (ConvertUtil.IsNumericType(args[i].GetType()) && ConvertUtil.IsNumericType(paramInfos[i].ParameterType))
                        {
                            args[i] = ConvertUtil.ToPrim(args[i], paramInfos[i].ParameterType);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static IEnumerable<MemberInfo> FilterMembers(IEnumerable<MemberInfo> members, MemberTypes mask)
        {
            foreach (var m in members)
            {
                if ((m.MemberType & mask) != 0) yield return m;
            }
        }

        private static bool IsValidValueMember(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Field:
                    return true;

                case System.Reflection.MemberTypes.Property:
                    {
                        var prop = member as System.Reflection.PropertyInfo;
                        if (prop.CanRead && prop.GetIndexParameters().Length == 0) return true;
                        break;
                    }
                case System.Reflection.MemberTypes.Method:
                    {
                        var meth = member as System.Reflection.MethodInfo;
                        if (meth.GetParameters().Length == 0) return true;
                        break;
                    }
            }
            return false;
        }

        private static bool IsValidValueSetterMember(MemberInfo member, System.Type valueType)
        {
            switch (member.MemberType)
            {
                case System.Reflection.MemberTypes.Field:
                    var field = member as System.Reflection.FieldInfo;
                    if ((valueType == null && !field.FieldType.IsValueType) ||
                        field.FieldType.IsAssignableFrom(valueType) ||
                        (ConvertUtil.IsNumericType(field.FieldType) && ConvertUtil.IsNumericType(valueType)))
                    {
                        return true;
                    }

                    break;
                case System.Reflection.MemberTypes.Property:
                    var prop = member as System.Reflection.PropertyInfo;
                    if (prop.CanWrite && ((valueType == null && !prop.PropertyType.IsValueType) ||
                                          prop.PropertyType.IsAssignableFrom(valueType) ||
                                          (ConvertUtil.IsNumericType(prop.PropertyType) && ConvertUtil.IsNumericType(valueType))) &&
                        prop.GetIndexParameters().Length == 0)
                    {
                        return true;
                    }
                    break;
                case System.Reflection.MemberTypes.Method:
                    {
                        var meth = member as System.Reflection.MethodInfo;
                        var paramInfos = meth.GetParameters();
                        if (paramInfos.Length != 1) return false;
                        if ((valueType == null && !paramInfos[0].ParameterType.IsValueType) ||
                            paramInfos[0].ParameterType.IsAssignableFrom(valueType) ||
                            (ConvertUtil.IsNumericType(paramInfos[0].ParameterType) && ConvertUtil.IsNumericType(valueType)))
                        {
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        #endregion

        #region Special Types

        private class DynamicBinder : Binder
        {

            public static readonly DynamicBinder Default = new DynamicBinder();


            public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
            {
                return Type.DefaultBinder.BindToField(bindingAttr, match, value, culture);
            }

            public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
            {
                return Type.DefaultBinder.SelectProperty(bindingAttr, match, returnType, indexes, modifiers);
            }

            public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
            {
                if (args == null) args = ArrayUtil.Empty<object>();

                state = null;
                foreach (var m in match)
                {
                    var pinfos = m.GetParameters();
                    if (ParameterSignatureMatches(args, pinfos, true))
                    {
                        if (args.Length != pinfos.Length)
                        {
                            Array.Resize(ref args, pinfos.Length);
                        }
                        return m;
                    }
                }

                return Type.DefaultBinder.BindToMethod(bindingAttr, match, ref args, modifiers, culture, names, out state);
            }

            public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
            {
                return Type.DefaultBinder.SelectMethod(bindingAttr, match, types, modifiers);
            }

            public override object ChangeType(object value, Type type, CultureInfo culture)
            {
                return Type.DefaultBinder.ChangeType(value, type, culture);
            }

            public override void ReorderArgumentArray(ref object[] args, object state)
            {
                Type.DefaultBinder.ReorderArgumentArray(ref args, state);
            }

        }

        #endregion

    }

}
