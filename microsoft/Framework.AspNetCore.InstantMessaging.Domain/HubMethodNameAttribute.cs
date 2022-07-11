using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HubMethodNameAttribute : Attribute
    {
        public string Name { get; }

        public HubMethodNameAttribute(string name)
        {
            Name = name;
        }
    }
}
