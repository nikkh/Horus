using System;
using System.Collections.Generic;
using System.Text;

namespace Horus.Functions.Models
{
    public static class PreprocessorStatus
    {
        private const string pending = "Pending";
        private const string completed = "Completed";
        public static string Pending { get { return pending; } }
        public static string Completed { get { return completed; } }
    }
}
