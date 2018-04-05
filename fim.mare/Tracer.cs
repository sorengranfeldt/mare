using System;
using System.Diagnostics;

namespace FIM.MARE
{
    public static class Tracer
    {
        const string switchName = "MARE";
        const string sourceName = "FIM.MARE";
        internal static TraceSource trace = new TraceSource(sourceName, SourceLevels.All);

        public static void Enter(string entryPoint)
        {
            TraceInformation("enter {0}", entryPoint);
        }
        public static void Exit(string entryPoint)
        {
            TraceInformation("exit {0}", entryPoint);
        }
        public static void TraceInformation(string message, params object[] param)
        {
            trace.TraceInformation(message, param);
        }
        public static void TraceWarning(string message, int warningCode = 1, params object[] param)
        {
            string msg = string.Format(message, param);
            trace.TraceEvent(TraceEventType.Warning, warningCode, GetMessageFromException(null, message));
            WriteToEventLog(message, EventLogEntryType.Warning, warningCode, 0);
        }
        internal static string GetMessageFromException(Exception ex, string message)
        {
            string msg = string.Format("{0}, {1}", message, ex == null ? "N/A" : ex.GetBaseException().Message);
            return msg;
        }
        internal static void WriteToEventLog(string message, EventLogEntryType entryType, int? eventId, short? category)
        {
            EventLog.WriteEntry(sourceName, message, entryType, eventId.GetValueOrDefault(), category.GetValueOrDefault());
        }
        public static void TraceError(string message, int id, params object[] param)
        {
            trace.TraceEvent(TraceEventType.Error, id, message, param);
            WriteToEventLog(message, EventLogEntryType.Error, id, 0);
        }
        public static void TraceError(string message, Exception ex, int errorCode = 1)
        {
            string msg = GetMessageFromException(ex, message);
            trace.TraceEvent(TraceEventType.Error, ex.HResult, msg);
            WriteToEventLog(msg, EventLogEntryType.Error, errorCode, 0);
        }
        public static void TraceError(string message, params object[] param)
        {
            TraceError(message, 0, param);
        }
        static Tracer()
        {
            SourceSwitch sw = new SourceSwitch(switchName, switchName);
            sw.Level = SourceLevels.All;
            trace.Switch = sw;
        }
    }
}
