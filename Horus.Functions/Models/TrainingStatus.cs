using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public static class TrainingStatus
    {
        private const string ready = "ready";
        private const string invalid = "invalid";
        public static string Ready { get { return ready; } }
        public static string Invalid { get { return invalid; } }
    }
}
