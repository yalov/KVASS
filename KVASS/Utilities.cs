using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KVASS
{
    public static class Utilities
    {
        public static object GetMemberInfoValue(System.Reflection.MemberInfo member, object sourceObject)
        {
            object newVal;
            if (member is System.Reflection.FieldInfo)
                newVal = ((System.Reflection.FieldInfo)member).GetValue(sourceObject);
            else
                newVal = ((System.Reflection.PropertyInfo)member).GetValue(sourceObject, null);
            return newVal;
        }
    }

    public static class TypeExtensions
    {

        public static T GetPublicValue<T>(this Type type, string name, object instance) where T : class
        {
            return (T)Utilities.GetMemberInfoValue(type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).FirstOrDefault(), instance);
        }

        public static object GetPrivateMemberValue(this Type type, string name, object instance, int index = -1)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            object value = Utilities.GetMemberInfoValue(type.GetMember(name, flags).FirstOrDefault(), instance);
            if (value != null)
            {
                return value;
            }

            Logging.Log($"Could not get value by name '{name}', getting by index '{index}'");
            if (index >= 0)
            {
                List<MemberInfo> members = type.GetMembers(flags).ToList();
                if (members.Count > index)
                {
                    return Utilities.GetMemberInfoValue(members[index], instance);
                }
            }
            throw new Exception($"No members found for name '{name}' at index '{index}' for type '{type}'");
        }
    }
}
