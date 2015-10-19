using System.Diagnostics;

namespace FIM.MARE
{
	public static class Tracer
	{
		const string SwitchName = "MARE";
		const string SourceName = "FIM.MARE";
		static TraceSource Trace = new TraceSource(SourceName, SourceLevels.All);
		static string IndentText = "";

		public static int IndentLevel
		{
			get
			{
				return IndentText.Length;
			}
			set
			{
				IndentText = "";
			}
		}
		public static void Indent()
		{
			IndentText = IndentText + " ";
		}
		public static void Unindent()
		{
			IndentText = IndentText.Trim(' ');
		}
		public static void TraceInformation(string message, params object[] param)
		{
			Trace.TraceInformation(IndentText + message, param);
		}
		public static void TraceWarning(string message, params object[] param)
		{
			Trace.TraceEvent(TraceEventType.Warning, -1, IndentText + message, param);
		}
		public static void TraceError(string message, int id, params object[] param)
		{
			Trace.TraceEvent(TraceEventType.Error, id, IndentText + message, param);
		}
		public static void TraceError(string message, params object[] param)
		{
			TraceError(message, -2, param);
		}
		static Tracer()
		{
			SourceSwitch sw = new SourceSwitch(SwitchName, SwitchName);
			sw.Level = SourceLevels.All;
			Trace.Switch = sw;
		}
	}
}
