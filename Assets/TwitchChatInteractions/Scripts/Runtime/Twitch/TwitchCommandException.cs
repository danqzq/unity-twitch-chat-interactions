using System;

namespace TwitchIntegration
{
    internal class TwitchCommandException : Exception
    {
        internal TwitchCommandException(string message) : base(message) { }
    }
}