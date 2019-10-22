using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KVASSNS
{
    public static class Utils
    {
        public static object GetMemberInfoValue(System.Reflection.MemberInfo member, object sourceObject)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));

            object newVal;
            if (member is System.Reflection.FieldInfo)
                newVal = ((System.Reflection.FieldInfo)member).GetValue(sourceObject);
            else
                newVal = ((System.Reflection.PropertyInfo)member).GetValue(sourceObject, null);
            return newVal;
        }


        //takes any number of strings and returns them joined together with Linux specific path divider, ie:
        //Paths.joined("follow", "the", "yellow", "brick", "road") -> "follow/the/yellow/brick/road 
        static public string PathJoin(params string[] paths)
        {
            return String.Join("/", paths).Replace("\\", "/");
        }

        static public double UT()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                return HighLogic.CurrentGame.flightState.universalTime;
            }
            else
            {
                return Planetarium.GetUniversalTime();
            }
        }
    }

    public static class TypeExtensions
    {

        public static T GetPublicValue<T>(this Type type, string name, object instance) where T : class
        {
            if (type == null) return null;
            return (T)Utils.GetMemberInfoValue(type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy).FirstOrDefault(), instance);
        }

        public static object GetPrivateMemberValue(this Type type, string name, object instance, int index = -1)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            object value = Utils.GetMemberInfoValue(type.GetMember(name, flags).FirstOrDefault(), instance);
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
                    return Utils.GetMemberInfoValue(members[index], instance);
                }
            }
            throw new Exception($"No members found for name '{name}' at index '{index}' for type '{type}'");
        }
    }
}
