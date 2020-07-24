using System;
using System.Runtime.Serialization;

namespace Horus.Functions
{
    [Serializable]
    internal class HorusTerminalException : Exception
    {
        private Exception ex;

        public HorusTerminalException()
        {
        }

        public HorusTerminalException(Exception ex)
        {
            this.ex = ex;
        }

        public HorusTerminalException(string message) : base(message)
        {
        }

        public HorusTerminalException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected HorusTerminalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}