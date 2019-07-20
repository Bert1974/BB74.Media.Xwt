using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace BaseLib
{
    public enum MessageTypes
    {
        Debug,
        Verbose,
        Trace,
        Error
    }
    public static class Log
    {
        public static string ErrorFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "errors.txt");

        public static void Debug(string message)
        {
            Message(MessageTypes.Debug, message);
        }
        public static void Trace(string message)
        {
            Message(MessageTypes.Trace, message);
        }
        public static void Verbose(string message)
        {
            Message(MessageTypes.Verbose, message);
        }
        public static void Error(string message)
        {
            Message(MessageTypes.Error, message);
        }

        private static void Message(MessageTypes type, string message)
        {
            switch (type)
            {
                case MessageTypes.Error:
                    Console.Error.WriteLine($"{type.ToString().ToLower()} {DateTime.Now.ToLongTimeString()} : {message}");
                    break;
                default:
                    Console.WriteLine($"{type.ToString().ToLower()} {DateTime.Now.ToLongTimeString()} : {message}");
                    break;
            }
        }
        public static void LogException(Exception e)
        {
            Error(e.ToString());

            int cnt = 0;
            while (!TryWriteExcepetion(e))
            {
                Thread.Sleep(50);

                if (++cnt > 15) { break; }
            }
        }
        public static void LogException(string message)
        {
            Error(message);

            int cnt = 0;
            while (!TryWriteExcepetion(message))
            {
                Thread.Sleep(50);

                if (++cnt > 15) { break; }
            }
        }
        private static bool TryWriteExcepetion(Exception exception)
        {
            try
            {
                using (TextWriter writer = new StreamWriter(Log.ErrorFile, true, Encoding.Unicode))
                {
                    try
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine(DateTime.Now.ToString());

                        for (Exception e = exception; e != null; e = e.InnerException)
                        {
                            writer.WriteLine(e.Message);
                            writer.WriteLine(e.Source);
                            writer.WriteLine(e.StackTrace);
                            writer.WriteLine();
                        }
                    }
                    catch { }
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }
        private static bool TryWriteExcepetion(string message)
        {
            try
            {
                using (TextWriter writer = new StreamWriter(Log.ErrorFile, true, Encoding.Unicode))
                {
                    try
                    {
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine(message);
                    }
                    catch { }
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
