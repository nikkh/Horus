using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public static class RecognizerStatus
    {
        private const string succeeded = "succeeded";
        private const string failed = "failed";
        private const string notStarted = "notStarted";
        private const string running = "running";
        public static string Succeeded { get { return succeeded; } }
        public static string Failed { get { return failed; } }
        public static string NotStarted { get { return notStarted; } }
        public static string Running { get { return running; } }


    }
}
