using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HuntHelper
{
    internal class PluginLog
    {
        public static IPluginLog Logger { get; set; } = null;

        public static void Log(string message, params object[] args) { Logger.Info(message, args); }
        public static void Log(Exception? exception, string messageTemplate, params object[] values) { Logger.Info(messageTemplate, values); }
        public static void Error(string message, params object[] args) { Logger.Error(message, args); }
        public static void Error(Exception? exception, string messageTemplate, params object[] values) { Logger.Error(exception, messageTemplate, values); }
        public static void Warning(string message, params object[] args) { Logger.Warning(message, args); }
        public static void Warning(Exception? exception, string messageTemplate, params object[] values) { Logger.Warning(exception, messageTemplate, values); }
        public static void Debug(string message, params object[] args) { Logger.Debug(message, args); }
        public static void Debug(Exception? exception, string messageTemplate, params object[] values) { Logger.Debug(exception, messageTemplate, values); }
        public static void Verbose(string message, params object[] args) { Logger.Verbose(message, args); }
        public static void Fatal(string message, params object[] args) { Logger.Fatal(message, args); }
        public static void Fatal(Exception? exception, string messageTemplate, params object[] values) { Logger.Fatal(exception, messageTemplate, values); }

    }
}
