using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    internal static class HubReflectionHelper
    {
        private static readonly Type[] _excludeInterfaces = new[] { typeof(IDisposable) };

        /// <summary>
        /// 获取所有hub的方法
        /// </summary>
        /// <param name="hubType"></param>
        /// <returns></returns>
        public static IEnumerable<MethodInfo> GetHubMethods(Type hubType)
        {
            var methods = hubType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var allInterfaceMethods = _excludeInterfaces.SelectMany(i => GetInterfaceMethods(hubType, i));

            return methods.Except(allInterfaceMethods).Where(m => IsHubMethod(m));
        }

        private static IEnumerable<MethodInfo> GetInterfaceMethods(Type type, Type iface)
        {
            if (!iface.IsAssignableFrom(type))
            {
                return Enumerable.Empty<MethodInfo>();
            }

            return type.GetInterfaceMap(iface).TargetMethods;
        }

        private static bool IsHubMethod(MethodInfo methodInfo)
        {
            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType!;
            if (typeof(object) == baseDefinition || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseType = baseDefinition.GetTypeInfo().IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            return typeof(Hub) != baseType;
        }
    }
}
