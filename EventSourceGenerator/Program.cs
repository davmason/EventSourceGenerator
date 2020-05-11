using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;

namespace EventSourceGenerator
{
    class EventSourceLayout
    {
        public string Name { get; internal set; }
        public List<EventLayout> EventLayouts { get; internal set; }
        public bool IsSelfDescribing { get; internal set; }
    }

    class EventArgumentType
    {
        public string Type { get; internal set; }
        public bool IsArray { get; internal set; }
        public string ArrayType { get; internal set; }

        public override string ToString()
        {
            return Type;
        }
    }

    class EventArgument
    {
        public string Name { get; internal set; }
        public EventArgumentType Type { get; internal set; }
    }

    class EventLayout
    {
        public string Name { get; internal set; }
        public int Level { get; internal set; }
        public int Keywords { get; internal set; }
        public int Opcode { get; internal set; }
        public int Version { get; internal set; }
        public List<EventArgument> Arguments { get; internal set; }
        public List<string> ArgumentValues { get; internal set; }
        public int ID { get; internal set; }
    }

    class Program
    {
        static Random s_rand = new Random();

#if true
        static readonly int s_numberOfEventSources = 20;
        static readonly int s_maxNumberOfEvents = 200;
        static readonly int s_maxNumberOfEventArguments = 20;
        static readonly int s_maxArrayElements = 1024;
#else
        static readonly int s_numberOfEventSources = 20;
        static readonly int s_maxNumberOfEvents = 10;
        static readonly int s_maxNumberOfEventArguments = 10;
        static readonly int s_maxArrayElements = 5;
#endif

        static StreamWriter s_output;

        static void WriteLine(string line)
        {
            s_output.WriteLine(line);
        }

        static void Write(string line)
        {
            s_output.Write(line);
        }

        static void Main(string[] args)
        {
            string outputPath = @"C:\work\eventsource_fuzzer_test\Program.cs";
            using (s_output = new StreamWriter(new FileStream(outputPath, FileMode.Create)))
            {
                WritePreEventSourceFileContents();

                List<string> eventSourceNames = Enumerable.Range(0, s_numberOfEventSources).Select(i => "TestEventSource" + i.ToString()).ToList();
                Dictionary<string, EventSourceLayout> eventSourceLayouts = new Dictionary<string, EventSourceLayout>();
                foreach (string name in eventSourceNames)
                {
                    Console.WriteLine($"Writing EventSource {name}");
                    EventSourceLayout layout = GenerateEventSource(name, s_maxNumberOfEvents, s_maxNumberOfEventArguments);
                    eventSourceLayouts.Add(name, layout);
                }

                WritePostEventSourceFileContents(eventSourceLayouts);
            }
        }

        private static void WritePreEventSourceFileContents()
        {
            WriteLine("using System;");
            WriteLine("using System.Collections;");
            WriteLine("using System.Collections.Generic;");
            WriteLine("using System.Collections.ObjectModel;");
            WriteLine("using System.Diagnostics;");
            WriteLine("using System.Diagnostics.Tracing;");
            WriteLine("using System.Globalization;");
            WriteLine("using System.IO;");
            WriteLine("using System.Linq;");
            WriteLine("using System.Runtime.CompilerServices;");
            WriteLine("using System.Text;");
            WriteLine("using System.Threading;");
            WriteLine("using Microsoft.Diagnostics.NETCore.Client;");
            WriteLine("using Microsoft.Diagnostics.Tracing;");
            WriteLine("using Microsoft.Diagnostics.Tracing.Etlx;");
            WriteLine("");
            WriteLine("namespace EventSourceTest");
            WriteLine("{");
        }

        private static EventSourceLayout GenerateEventSource(string name, int maxNumberOfEvents, int maxNumberOfEventArguments)
        {
            int numberOfEvents = s_rand.Next(maxNumberOfEvents);
            bool isSelfDescribing = GetRandomBool();

            WriteLine($"    class {name}: EventSource");
            WriteLine("    {");
            WriteLine($"        public {name}()");

            if (isSelfDescribing)
            {
                WriteLine("            : base(EventSourceSettings.EtwSelfDescribingEventFormat)");
            }

            WriteLine("        {");
            WriteLine("             // Empty constructor body");
            WriteLine("        }");
            WriteLine("");

            List<EventLayout> events = new List<EventLayout>();
            for (int i = 0; i < numberOfEvents; ++i)
            {
                int numberOfEventArgs = s_rand.Next(maxNumberOfEventArguments);
                events.Add(GenerateEvent(i + 1, numberOfEventArgs, isSelfDescribing));
            }

            WriteLine("    }");
            WriteLine("");

            return new EventSourceLayout()
            {
                Name = name,
                IsSelfDescribing = isSelfDescribing,
                EventLayouts = events
            };
        }

        private static EventLayout GenerateEvent(int id, int numberOfEventArgs, bool isSelfDescribing)
        {
            bool hasLevel = GetRandomBool();
            int level = (int)EventLevel.Informational;
            bool hasKeywords = GetRandomBool();
            int keywords = (int)EventKeywords.None;
            bool hasOpcode = GetRandomBool();
            int opcode = (int)EventOpcode.Info;
            bool hasVersion = GetRandomBool();
            int version = 0;

            WriteLine("");

            // Write EventAttribute
            Write($"        [Event({id}");
            if (hasLevel)
            {
                level = s_rand.Next((int)EventLevel.Verbose);
                Write($", Level = (EventLevel){level}");
            }

            if (hasKeywords)
            {
                keywords = s_rand.Next();
                Write($", Keywords = (EventKeywords){keywords}");
            }

            if (hasOpcode)
            {
                opcode = s_rand.Next((int)EventOpcode.Receive);
                Write($", Opcode = (EventOpcode){opcode}");
            }

            if (hasVersion)
            {
                version = s_rand.Next(50);
                Write($", Version = {version}");
            }

            // End of EventAttribute
            WriteLine(")]");

            string methodName = "TestEvent" + id.ToString();
            Write($"        public void {methodName}(");
            List<EventArgument> arguments = Enumerable.Range(0, numberOfEventArgs).Select(i =>
            {
                return new EventArgument()
                {
                    Name = "arg" + i.ToString(),
                    Type = GetRandomEventSourceArgType(isSelfDescribing)
                };
            }).ToList();

            for (int i = 0; i < arguments.Count; ++i)
            {
                EventArgument argument = arguments[i];
                Write($"{argument.Type} {argument.Name}");
                if (i < arguments.Count - 1)
                {
                    Write(", ");
                }
            }

            // End of argument list
            WriteLine(")");

            WriteLine("        {");

            Write($"            WriteEvent({id}");
            if (arguments.Count > 0)
            {
                Write(", ");
            }

            for (int i = 0; i < arguments.Count; ++i)
            {
                EventArgument argument = arguments[i];
                Write(argument.Name);
                if (i < arguments.Count - 1)
                {
                    Write(", ");
                }
            }

            // End of method call
            WriteLine(");");

            WriteLine("        }");

            return new EventLayout()
            {
                Name = methodName,
                ID = id,
                Level = level,
                Keywords = keywords,
                Opcode = opcode,
                Version = version,
                Arguments = arguments
            };
        }

        private static EventArgumentType GetRandomEventSourceArgType(bool isSelfDescribing)
        {
            // TODO: user types would be nice
            if (isSelfDescribing && GetRandomBool())
            {
                // Generate an array type                
                return (s_rand.Next(10)) switch
                {
                    0 => new EventArgumentType() { Type = "ushort[]", IsArray = true, ArrayType = "ushort" },
                    1 => new EventArgumentType() { Type = "byte[]", IsArray = true, ArrayType = "byte" },
                    2 => new EventArgumentType() { Type = "char[]", IsArray = true, ArrayType = "char" },
                    3 => new EventArgumentType() { Type = "double[]", IsArray = true, ArrayType = "double" },
                    4 => new EventArgumentType() { Type = "float[]", IsArray = true, ArrayType = "float" },
                    5 => new EventArgumentType() { Type = "int[]", IsArray = true, ArrayType = "int" },
                    6 => new EventArgumentType() { Type = "uint[]", IsArray = true, ArrayType = "uint" },
                    7 => new EventArgumentType() { Type = "long[]", IsArray = true, ArrayType = "long" },
                    8 => new EventArgumentType() { Type = "ulong[]", IsArray = true, ArrayType = "ulong" },
                    9 => new EventArgumentType() { Type = "short[]", IsArray = true, ArrayType = "short" },
                    _ => throw new Exception(),
                };
            }
            else
            {
                return (s_rand.Next(10)) switch
                {
                    0 => new EventArgumentType() { Type = "ushort", IsArray = false, ArrayType = null },
                    1 => new EventArgumentType() { Type = "byte", IsArray = false, ArrayType = null },
                    2 => new EventArgumentType() { Type = "char", IsArray = false, ArrayType = null },
                    3 => new EventArgumentType() { Type = "double", IsArray = false, ArrayType = null },
                    4 => new EventArgumentType() { Type = "float", IsArray = false, ArrayType = null },
                    5 => new EventArgumentType() { Type = "int", IsArray = false, ArrayType = null },
                    6 => new EventArgumentType() { Type = "uint", IsArray = false, ArrayType = null },
                    7 => new EventArgumentType() { Type = "long", IsArray = false, ArrayType = null },
                    8 => new EventArgumentType() { Type = "ulong", IsArray = false, ArrayType = null },
                    9 => new EventArgumentType() { Type = "short", IsArray = false, ArrayType = null },
                    _ => throw new Exception(),
                };
            }
        }

        private static void WritePostEventSourceFileContents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            WriteLine("    class Program");
            WriteLine("    {");
            WriteLine("        static int s_successCount = 0;");
            WriteLine("");
            WriteLine("        static readonly string traceIDValidationMessage = \"Expected ID {0} but got ID {1} for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string traceVersionValidationMessage = \"Expected version {0} but got version {1} for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string traceLevelValidationMessage = \"Expected level {0} but got level {1} for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string traceOpcodeValidationMessage = \"Expected opcode {0} but got opcode {1} for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string tracePayloadValidationMessage = \"Expected {0} payload items but got {1} items for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string tracePayloadNamesValidationMessage = \"Expected argument name {0} but got name {1} for EventSource={2} Event={3}\";");
            WriteLine("        static readonly string tracePayloadTypeValidationMessage = \"Expected type {0} but got type {1} for EventSource={2} Event={3} Argument={4}\";");
            WriteLine("        static readonly string tracePayloadValueValidationMessage = \"Expected argument value {0} but got value {1} for EventSource={2} Event={3} Argument={4}\";");
            WriteLine("");
            WriteLine("        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)");
            WriteLine("        {");
            WriteLine("            int processId = Process.GetCurrentProcess().Id;");
            WriteLine("            DiagnosticsClient client = new DiagnosticsClient(processId);");
            WriteLine("            return client.StartEventPipeSession(providers, /* requestRunDown */ false);");
            WriteLine("        }");
            WriteLine("");
            WriteLine("        static bool ArraysEqual<T>(T[] lhs, T[] rhs)");
            WriteLine("        {");
            WriteLine("            if (lhs.Length != rhs.Length)");
            WriteLine("            {");
            WriteLine("                return false;");
            WriteLine("            }");
            WriteLine("");
            WriteLine("            for (int i = 0; i < lhs.Length; ++i)");
            WriteLine("            {");
            WriteLine("                if (!lhs[i].Equals(rhs[i]))");
            WriteLine("                {");
            WriteLine("                    return false;");
            WriteLine("                }");
            WriteLine("            }");
            WriteLine("");
            WriteLine("            return true;");
            WriteLine("        }");
            WriteLine("");

            GenerateWriteEvents(eventSourceLayouts);
            GenerateValidateEvents(eventSourceLayouts);

            WriteLine("        static void Main(string[] args)");
            WriteLine("        {");
            WriteLine("            List<EventPipeProvider> providers = new List<EventPipeProvider>");
            WriteLine("            {");
            WriteLine("                new EventPipeProvider(\"Microsoft - Windows - DotNETRuntime\", EventLevel.Informational, (long)EventKeywords.All),");

            foreach (string name in eventSourceLayouts.Keys)
            {
                WriteLine($"                new EventPipeProvider(\"{name}\", EventLevel.Informational, (long)EventKeywords.All),");
            }

            WriteLine("            };");
            WriteLine("");
            WriteLine("            using (EventPipeSession session = AttachEventPipeSessionToSelf(providers))");
            WriteLine("            {");
            WriteLine("                WriteEvents();");
            WriteLine("                ManualResetEvent allEventsReceivedEvent = new ManualResetEvent(false);");
            WriteLine("");
            WriteLine("                int eventCount = 0;");
            WriteLine("                var source = new EventPipeEventSource(session.EventStream);");
            WriteLine("                source.Dynamic.All += (TraceEvent traceEvent) =>");
            WriteLine("                {");
            WriteLine("                    ++eventCount;");
            WriteLine("                    ValidateEvent(traceEvent);");
            WriteLine("                };");

            WriteLine("");
            WriteLine("                Thread processingThread = new Thread(new ThreadStart(() =>");
            WriteLine("                {");
            WriteLine("                    source.Process();");
            WriteLine("                }));");
            WriteLine("");
            WriteLine("                Console.WriteLine(\"Starting processing thread\");");
            WriteLine("                processingThread.Start();");
            WriteLine("");
            WriteLine("                session.Stop();");
            WriteLine("");
            WriteLine("                Console.WriteLine(\"Joining processing thread\");");
            WriteLine("                processingThread.Join();");
            WriteLine("                Console.WriteLine($\"Number of events successfully validated={s_successCount}\");");
            WriteLine("                Console.WriteLine($\"Total number of events={eventCount}\"); ");
            WriteLine("            }");
            WriteLine("        }");
            WriteLine("    }");
            WriteLine("}");
        }

        private static void GenerateWriteEvents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            WriteLine("        static void WriteEvents()");
            WriteLine("        {");

            for (int i = 0; i < eventSourceLayouts.Keys.Count; ++i)
            {
                string name = eventSourceLayouts.Keys.ElementAt(i);
                string eventSourceName = "eventSource" + i.ToString();
                WriteLine($"            {name} {eventSourceName} = new {name}(); ");

                EventSourceLayout layout = eventSourceLayouts[name];
                foreach (EventLayout eventLayout in layout.EventLayouts)
                {
                    GenerateMethodCallForLayout(eventSourceName, eventLayout);
                }

                WriteLine("");
            }

            WriteLine("        }");
            WriteLine("");
        }

        private static void GenerateMethodCallForLayout(string eventSourceName, EventLayout eventLayout)
        {
            eventLayout.ArgumentValues = new List<string>();
        
            Write($"            {eventSourceName}.{eventLayout.Name}(");
            for (int i = 0; i < eventLayout.Arguments.Count; ++i)
            {
                EventArgument argument = eventLayout.Arguments[i];
                string argValue = GenerateRandomArgumentValue(argument);
                eventLayout.ArgumentValues.Add(argValue);
                Write(argValue);

                if (i < eventLayout.Arguments.Count - 1)
                {
                    Write(", ");
                }
            }

            WriteLine(");");
        }

        private static string GenerateRandomArgumentValue(EventArgument argument)
        {
            int arrayElements = s_rand.Next(s_maxArrayElements);
            EventArgumentType argumentType = argument.Type;

            if (argumentType.IsArray)
            {
                object[] objArray = null;
                string castString = null;
                if (argumentType.Type == "bool[]")
                {
                    castString = "(bool)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => GetRandomBool().ToString().ToLower()).ToArray();
                }
                else if (argumentType.Type == "byte[]")
                {
                    castString = "(byte)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(byte.MinValue, byte.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "sbyte[]")
                {
                    castString = "(sbyte)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(sbyte.MinValue, sbyte.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "char[]")
                {
                    castString = "(char)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(char.MinValue, char.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "decimal[]")
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "double[]")
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.NextDouble()).ToArray();
                }
                else if (argumentType.Type == "float[]")
                {
                    castString = "(float)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.NextDouble()).ToArray();
                }
                else if (argumentType.Type == "int[]")
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "uint[]")
                {
                    castString = "(uint)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(0, int.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "long[]")
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "ulong[]")
                {
                    castString = "(ulong)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(0, int.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "short[]")
                {
                    castString = "(short)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(short.MinValue, short.MaxValue)).ToArray();
                }
                else if (argumentType.Type == "ushort[]")
                {
                    castString = "(ushort)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(ushort.MinValue, ushort.MaxValue)).ToArray();
                }

                StringBuilder arrayString = new StringBuilder();
                arrayString.Append($"new {argumentType} {{");
                for (int i = 0; i < objArray.Length; ++i)
                {
                    if (castString != null)
                    {
                        arrayString.Append(castString);
                    }

                    arrayString.Append(objArray[i]);
                    if (i < objArray.Length - 1)
                    {
                        arrayString.Append(", ");
                    }
                }
                arrayString.Append("}");

                return arrayString.ToString();
            }
            else if (argumentType.Type == "bool")
            {
                return GetRandomBool().ToString().ToLower();
            }
            else if (argumentType.Type == "byte")
            {
                return "(byte)" + s_rand.Next(byte.MinValue, byte.MaxValue).ToString();
            }
            else if (argumentType.Type == "sbyte")
            {
                return "(sbyte)" + s_rand.Next(sbyte.MinValue, sbyte.MaxValue).ToString();
            }
            else if (argumentType.Type == "char")
            {
                return "(char)" + s_rand.Next(char.MinValue, char.MaxValue).ToString();
            }
            else if (argumentType.Type == "decimal")
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (argumentType.Type == "double")
            {
                return s_rand.NextDouble().ToString();
            }
            else if (argumentType.Type == "float")
            {
                return "(float)" + s_rand.NextDouble().ToString();
            }
            else if (argumentType.Type == "int")
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (argumentType.Type == "uint")
            {
                return "(uint)" + s_rand.Next(0, int.MaxValue).ToString();
            }
            else if (argumentType.Type == "long")
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (argumentType.Type == "ulong")
            {
                return "(ulong)" + s_rand.Next(0, int.MaxValue).ToString();
            }
            else if (argumentType.Type == "short")
            {
                return "(short)" + s_rand.Next(short.MinValue, short.MaxValue).ToString();
            }
            else if (argumentType.Type == "ushort")
            {
                return "(ushort)" + s_rand.Next(0, ushort.MaxValue).ToString();
            }
            else
            {
                throw new ArgumentException();
            }
        }

        private static void GenerateValidateEvents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            WriteLine("        static void ValidateEvent(TraceEvent traceEvent)");
            WriteLine("        {");
            foreach (string name in eventSourceLayouts.Keys)
            {
                WriteLine($"            if (traceEvent.ProviderName == \"{name}\")");
                WriteLine("            {");
                WriteLine($"                if (Validate{name}(traceEvent))");
                WriteLine("                {");
                WriteLine("                     ++s_successCount;");
                WriteLine("                }");
                WriteLine("");
                WriteLine("                return;");
                WriteLine("            }");
                WriteLine("");
            }
            WriteLine("        }");
            WriteLine("");

            foreach (string name in eventSourceLayouts.Keys)
            {
                WriteLine($"        static bool Validate{name}(TraceEvent traceEvent)");
                WriteLine("        {");
                EventSourceLayout layout = eventSourceLayouts[name];
                WriteLine($"            if (traceEvent.ProviderName == \"{layout.Name}\")");
                WriteLine("            {");

                foreach (EventLayout eventLayout in layout.EventLayouts)
                {
                    WriteLine($"                if (traceEvent.EventName == \"{eventLayout.Name}\")");
                    WriteLine("                {");

                    // Self describing EventSources don't report their IDs/Versions correctly over ETW/EventPipe
                    if (!layout.IsSelfDescribing)
                    {
                        WriteLine($"                    if ((int)traceEvent.ID != {eventLayout.ID}) {{ Console.WriteLine(traceIDValidationMessage, {eventLayout.ID}, (int)traceEvent.ID, \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                        WriteLine($"                    if ((int)traceEvent.Version != {eventLayout.Version}) {{ Console.WriteLine(traceVersionValidationMessage, {eventLayout.Version}, (int)traceEvent.Version, \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                    }
                    else
                    {
                        WriteLine("                    // Skipping ID/Version validation because this EventSource is using SelfDescribing events.");
                    }

                    WriteLine($"                    if ((int)traceEvent.Level != {eventLayout.Level}) {{ Console.WriteLine(traceLevelValidationMessage, {eventLayout.Level}, (int)traceEvent.Level, \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                    WriteLine($"                    if ((int)traceEvent.Keywords != {eventLayout.Keywords}) {{ Console.WriteLine($\"Expected keywords {eventLayout.Keywords} but got keywords{{(int)traceEvent.Keywords}} for EventSource={layout.Name} Event={eventLayout.Name}\"); return false; }}");
                    WriteLine($"                    if ((int)traceEvent.Opcode != {eventLayout.Opcode}) {{ Console.WriteLine(traceOpcodeValidationMessage, {eventLayout.Opcode}, (int)traceEvent.Opcode, \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                    WriteLine("                     ++s_successCount;");

                    WriteLine($"                    if (traceEvent.PayloadNames.Count() != {eventLayout.Arguments.Count}) {{ Console.WriteLine(tracePayloadValidationMessage, {eventLayout.Arguments.Count}, traceEvent.PayloadNames.Count(), \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                    for (int i = 0; i < eventLayout.Arguments.Count; ++i)
                    {
                        WriteLine($"                    if (traceEvent.PayloadNames[{i}] != \"{eventLayout.Arguments[i].Name}\") {{ Console.WriteLine(tracePayloadNamesValidationMessage, \"{eventLayout.Arguments[i].Name}\", traceEvent.PayloadNames[{i}], \"{layout.Name}\", \"{eventLayout.Name}\"); return false; }}");
                        //WriteLine("        static readonly string tracePayloadTypeValidationMessage = \"Expected type {0} but got type {1} for EventSource={2} Event={3} Argument={4}\";");
                        WriteLine($"                    if (traceEvent.PayloadValue({i}).GetType() != typeof({eventLayout.Arguments[i].Type})) {{ Console.WriteLine(tracePayloadTypeValidationMessage, \"{eventLayout.Arguments[i].Type}\", traceEvent.PayloadValue({i}).GetType(), \"{layout.Name}\", \"{eventLayout.Name}\", \"{eventLayout.Arguments[i].Name}\"); return false; }}");
                        if (eventLayout.Arguments[i].Type.IsArray)
                        {
                            WriteLine($"                    if (!ArraysEqual(({eventLayout.Arguments[i].Type})traceEvent.PayloadValue({i}), {eventLayout.ArgumentValues[i]})) {{ Console.WriteLine(tracePayloadValueValidationMessage, {eventLayout.ArgumentValues[i]}, traceEvent.PayloadValue({i}), \"{layout.Name}\", \"{eventLayout.Name}\", \"{eventLayout.Arguments[i].Name}\"); return false; }}");
                        }
                        else
                        {
                            WriteLine($"                    if (({eventLayout.Arguments[i].Type})traceEvent.PayloadValue({i}) != {eventLayout.ArgumentValues[i]}) {{  Console.WriteLine(tracePayloadValueValidationMessage, {eventLayout.ArgumentValues[i]}, traceEvent.PayloadValue({i}), \"{layout.Name}\", \"{eventLayout.Name}\", \"{eventLayout.Arguments[i].Name}\"); return false; }}");
                        }
                    }

                    WriteLine("");
                    WriteLine("                    return true;");
                    WriteLine("                }");
                }

                WriteLine("            }");
                WriteLine("");
                WriteLine("            return false;");
                WriteLine("        }");
                WriteLine("");
            }

        }

        private static bool GetRandomBool()
        {
            return s_rand.Next(2) == 1;
        }
    }
}
