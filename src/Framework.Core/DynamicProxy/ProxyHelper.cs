﻿using System;
using System.Linq;
using System.Reflection;

namespace Framework.Core.DynamicProxy
{
    public static class ProxyHelper
    {
        private const string ProxyNamespace = "Castle.Proxies";

        public static object UnProxy(object obj)
        {
            if (obj.GetType().Namespace != ProxyNamespace)
            {
                return obj;
            }

            var targetField = obj.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name == "__target");

            if (targetField == null)
            {
                return obj;
            }

            return targetField.GetValue(obj);
        }

        public static Type GetUnProxiedType(object obj)
        {
            return UnProxy(obj).GetType();
        }
    }
}
