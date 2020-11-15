using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KVASSNS
{
    public static class Utils
    {
        /// <summary>
        /// Get a precision format for comparing string with the least digits.
        /// Example: value: 12.04, addends: 3.01, 9.02
        /// returns "F2";
        /// </summary>
        /// <param name="value"></param>
        /// <param name="addends"></param>
        /// <returns></returns>
        public static string GetComparingFormat(double value, params double[] addends)
        {
            if (addends.Length == 0) return "";
            const double Eps = 1e-10;

            double sum = addends.Sum();
            double diff = sum - value;
            double diff_abs = Math.Abs(diff);

            if (diff_abs < Eps) return "";

            int i = 0;
            const int maxFracDigits = 8;

            for (double diff_rounded_abs = 0; i < maxFracDigits; i++)
            {
                double sum_rounded = addends.Select(z => Math.Round(z, i)).Sum();
                double value_rounded = Math.Round(value, i);
                double diff_rounded = sum_rounded - value_rounded;
                diff_rounded_abs = Math.Abs(diff_rounded);

                if (diff_rounded_abs > Eps
                        && Math.Sign(diff_rounded) == Math.Sign(diff))
                    return "F" + i;
            }

            return "";
        }

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

        /// <summary>
        /// Get Universal Time on any scene
        /// </summary>
        /// <returns></returns>
        static public double UT()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return HighLogic.CurrentGame.flightState.universalTime;
            else
                return Planetarium.GetUniversalTime();
        }

        static public string GetVesselName()
        {
            if (HighLogic.LoadedSceneIsEditor) 
                return EditorLogic.fetch.ship.shipName.Trim();
            else 
                return FlightGlobals.ActiveVessel.GetDisplayName().Trim();
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
