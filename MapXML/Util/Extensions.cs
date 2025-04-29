using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace MapXML.Utils
{
    public static class Extensions
    {
        internal static object GetCurrentInstance(this IXMLInternalContext context) => context.CurrentInstance
                    ?? throw new InvalidOperationException("Current instance should not be null here!");
        public static IEnumerable<ResultType> ForEach<EnumerableType, ResultType>(
            this IEnumerable<EnumerableType> list,
            Func<EnumerableType, ResultType> function)
        {
            List<ResultType> _result = new List<ResultType>();
            list.ForEach(l => _result.Add(function(l)));
            return _result;
        }

        public static void ForEach<EnumerableType>
            (this IEnumerable<EnumerableType> list, Action<EnumerableType> action)
        {
            foreach (EnumerableType item in list)
            {
                action(item);
            }
        }




        //*****************************************//

        public static Type FieldOrPropertyType(this MemberInfo member)
        {
            if (member is FieldInfo field) return field.FieldType;
            else if (member is PropertyInfo pinfo) return pinfo.PropertyType;
            else throw new ArgumentException($"Member {member.Name} is neither a Property nor a Field");
        }
        public static bool IsFieldOrProperty(this MemberInfo member) => member is FieldInfo || member is PropertyInfo;

        public static object GetValue(this MemberInfo member, object instance)
        {
            if (member is FieldInfo field) return field.GetValue(instance);
            else if (member is PropertyInfo pinfo) return pinfo.GetValue(instance);
            else throw new ArgumentException($"Member {member.Name} is neither a Property nor a Field");

        }
        public static void SetValue(this MemberInfo member, object instance, object data)
        {
            if (member is FieldInfo field) field.SetValue(instance, data);
            else if (member is PropertyInfo pinfo) pinfo.SetValue(instance, data);
            else throw new ArgumentException($"Member {member.Name} is neither a Property nor a Field");

        }
        public static bool CanWrite(this MemberInfo member)
        {
            if (member is FieldInfo field) return true;
            else if (member is PropertyInfo pinfo) return pinfo.CanWrite;
            else return false;

        }
        public static bool TryGetMemberType(Type subjectType, string memberName, out Type? t, out MemberInfo? pinfo)
        {
            pinfo = FindPropertyOrField(subjectType, memberName);
            t = pinfo?.FieldOrPropertyType();
            return t != null;
        }

        public static Type GetMemberType(Type subjectType, string memberName, out MemberInfo? pinfo)
        {
            pinfo = FindPropertyOrField(subjectType, memberName);
            return pinfo?.FieldOrPropertyType() ?? throw new ArgumentException($"Member {memberName} not found");
        }


        public static bool TryGetRuntimeMemberType(object subject, string memberName, out Type? t, out MemberInfo? pinfo)
        {
            Type subjectType = subject.GetType();
            pinfo = FindPropertyOrField(subjectType, memberName);
            var runtimeObject = pinfo?.GetValue(subject);
            t = runtimeObject?.GetType() ?? pinfo?.FieldOrPropertyType();
            return pinfo != null;
        }
        public static Type? GetRuntimeMemberType(object subject, string memberName, out MemberInfo? pinfo)
        {
            Type subjectType = subject.GetType();
            pinfo = FindPropertyOrField(subjectType, memberName);
            if (pinfo == null)
                throw new ArgumentException($"Type {subjectType.Name} does not contain a member named {memberName}.");
            var runtimeObject = pinfo.GetValue(subject);
            return runtimeObject?.GetType();
        }
        public static MemberInfo? FindPropertyOrField(this INotifyPropertyChanged subject, string propertyName)
    => FindPropertyOrField(subject.GetType(), propertyName);

        public static MemberInfo? FindPropertyOrField(this Type subjectType, string propertyName)
        {
            Queue<Type> _queue = new Queue<Type>();
            _queue.Enqueue(subjectType);
            while (_queue.Count > 0)
            {
                Type current = _queue.Dequeue();
                MemberInfo? pinfo = current.GetMember(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
                if (!(pinfo is PropertyInfo || pinfo is FieldInfo))
                    pinfo = null;
                if (pinfo != null) return pinfo;

                if (current.BaseType != null) _queue.Enqueue(current.BaseType);
                foreach (Type IntrType in current.GetInterfaces())
                {
                    _queue.Enqueue(IntrType);
                }
            }
            return null;
        }

        public static bool IsCompatibleWithDelegateType(this MethodInfo method, Type delegateType)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));

            if (typeof(Delegate).IsAssignableFrom(delegateType))
            {
                MethodInfo DelegateMethod = delegateType.GetMethod("Invoke");

                var delegateReturnType = DelegateMethod.ReturnType;
                if (!method.ReturnType.Equals(delegateReturnType))
                    return false;

                var MethodParams = method.GetParameters();
                var DelegParams = DelegateMethod.GetParameters();

                if (MethodParams.Length != DelegParams.Length)
                    return false;

                for (var i = 0; i < MethodParams.Length; i++)
                {
                    if (!MethodParams[i].ParameterType.Equals(DelegParams[i]))
                        return false;
                }

                return true;

            }
            return false;
        }

    }
}
