using Dalamud.Plugin.Services;
using Serilog.Events;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ServiceCarePackage.Services.Logs
{
    public class MyLog : ILog
    {
        private IPluginLog pluginLog;

        public MyLog(IPluginLog pluginLog)
        {
            this.pluginLog = pluginLog;
        }

        public void Debug(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Debug(messageTemplate, values);
        }

        public void Debug(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog?.Debug(exception, messageTemplate, values);
        }

        public void Error(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Error(messageTemplate, values);
        }

        public void Error(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Error(exception, messageTemplate, values);
        }

        public void Fatal(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Fatal(messageTemplate, values);
        }

        public void Fatal(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Fatal(exception, messageTemplate, values);
        }

        public void Info(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Info(messageTemplate, values);
        }

        public void Info(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Info(exception, messageTemplate, values);
        }

        public void Information(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Information(messageTemplate, values);
        }

        public void Information(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Information(exception, messageTemplate, values);
        }

        public void Verbose(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Verbose(messageTemplate, values);
        }

        public void Verbose(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Verbose(exception, messageTemplate, values);
        }

        public void Warning(string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog?.Warning(messageTemplate, values);
        }

        public void Warning(Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog?.Warning(exception, messageTemplate, values);
        }

        public void Write(LogEventLevel level, Exception? exception, string messageTemplate, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "", params object[] values)
        {
            messageTemplate = MessagePrefix(messageTemplate, file, line, member);
            pluginLog.Write(level, exception, messageTemplate, values);
        }

        private string MessagePrefix(string message, string file, int line, string member)
        {
            return $"[{Path.GetFileName(file)}:{line}]: {message}";
        }
    }
}
