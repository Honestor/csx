﻿using System;
using System.Runtime.Serialization;

namespace Framework.Core
{
    public class FrameworkException : Exception
    {
        public FrameworkException()
        {

        }

        public FrameworkException(string message) : base(message)
        {

        }

        public FrameworkException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public FrameworkException(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context)
        {

        }
    }
}
