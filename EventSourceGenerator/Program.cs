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

    class EventArgument
    {
        public string Name { get; internal set; }
        public Type Type { get; internal set; }
    }

    class EventLayout
    {
        public string Name { get; internal set; }
        public int Level { get; internal set; }
        public int Keywords { get; internal set; }
        public int Opcode { get; internal set; }
        public int Version { get; internal set; }
        public List<EventArgument> Arguments { get; internal set; }
        public List<object> ArgumentValues { get; internal set; }
        public int ID { get; internal set; }
    }

    class Program
    {
        static Random s_rand = new Random();

        //static readonly int s_numberOfEventSources = 20;
        //static readonly int s_maxNumberOfEvents = 100;
        //static readonly int s_maxNumberOfEventArguments = 20;
        //static readonly int s_maxArrayElements = 1024;

        static readonly int s_numberOfEventSources = 20;
        static readonly int s_maxNumberOfEvents = 10;
        static readonly int s_maxNumberOfEventArguments = 10;
        static readonly int s_maxArrayElements = 5;

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

        private static Type GetRandomEventSourceArgType(bool isSelfDescribing)
        {
            // TODO: user types would be nice
            if (isSelfDescribing && GetRandomBool())
            {
                // Generate an array type                
                return (s_rand.Next(13)) switch
                {
                    0 => typeof(bool[]),
                    1 => typeof(byte[]),
                    2 => typeof(sbyte[]),
                    3 => typeof(char[]),
                    4 => typeof(decimal[]),
                    5 => typeof(double[]),
                    6 => typeof(float[]),
                    7 => typeof(int[]),
                    8 => typeof(uint[]),
                    9 => typeof(long[]),
                    10 => typeof(ulong[]),
                    11 => typeof(short[]),
                    12 => typeof(ushort[]),
                    _ => throw new Exception(),
                };
            }
            else
            {
                return (s_rand.Next(13)) switch
                {
                    0 => typeof(bool),
                    1 => typeof(byte),
                    2 => typeof(sbyte),
                    3 => typeof(char),
                    4 => typeof(decimal),
                    5 => typeof(double),
                    6 => typeof(float),
                    7 => typeof(int),
                    8 => typeof(uint),
                    9 => typeof(long),
                    10 => typeof(ulong),
                    11 => typeof(short),
                    12 => typeof(ushort),
                    _ => throw new Exception(),
                };
            }
        }

        private static void WritePostEventSourceFileContents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            WriteLine("    class Program");
            WriteLine("    {");
            WriteLine("        static int s_successCount = 0;");
            WriteLine("        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)");
            WriteLine("        {");
            WriteLine("            int processId = Process.GetCurrentProcess().Id;");
            WriteLine("            DiagnosticsClient client = new DiagnosticsClient(processId);");
            WriteLine("            return client.StartEventPipeSession(providers, /* requestRunDown */ false);");
            WriteLine("        }");
            WriteLine("");

            GenerateValidateEvents(eventSourceLayouts);
            GenerateWriteEvents(eventSourceLayouts);

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
            eventLayout.ArgumentValues = new List<object>();
        
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
            WriteLine("");
        }

        private static string GenerateRandomArgumentValue(EventArgument argument)
        {
            int arrayElements = s_rand.Next(s_maxArrayElements);
            Type type = argument.Type;

            if (type.IsArray)
            {
                object[] objArray = null;
                string castString = null;
                if (type == typeof(bool[]))
                {
                    castString = "(bool)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => GetRandomBool().ToString().ToLower()).ToArray();
                }
                else if (type == typeof(byte[]))
                {
                    castString = "(byte)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(byte.MinValue, byte.MaxValue)).ToArray();
                }
                else if (type == typeof(sbyte[]))
                {
                    castString = "(sbyte)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(sbyte.MinValue, sbyte.MaxValue)).ToArray();
                }
                else if (type == typeof(char[]))
                {
                    castString = "(char)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(char.MinValue, char.MaxValue)).ToArray();
                }
                else if (type == typeof(decimal[]))
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (type == typeof(double[]))
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.NextDouble()).ToArray();
                }
                else if (type == typeof(float[]))
                {
                    castString = "(float)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.NextDouble()).ToArray();
                }
                else if (type == typeof(int[]))
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (type == typeof(uint[]))
                {
                    castString = "(uint)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(0, int.MaxValue)).ToArray();
                }
                else if (type == typeof(long[]))
                {
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(int.MinValue, int.MaxValue)).ToArray();
                }
                else if (type == typeof(ulong[]))
                {
                    castString = "(ulong)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(0, int.MaxValue)).ToArray();
                }
                else if (type == typeof(short[]))
                {
                    castString = "(short)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(short.MinValue, short.MaxValue)).ToArray();
                }
                else if (type == typeof(ushort[]))
                {
                    castString = "(ushort)";
                    objArray = Enumerable.Range(0, arrayElements).Select<int, object>(i => s_rand.Next(ushort.MinValue, ushort.MaxValue)).ToArray();
                }

                StringBuilder arrayString = new StringBuilder();
                arrayString.Append($"new {type} {{");
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
            else if (type == typeof(bool))
            {
                return GetRandomBool().ToString().ToLower();
            }
            else if (type == typeof(byte))
            {
                return "(byte)" + s_rand.Next(byte.MinValue, byte.MaxValue).ToString();
            }
            else if (type == typeof(sbyte))
            {
                return "(sbyte)" + s_rand.Next(sbyte.MinValue, sbyte.MaxValue).ToString();
            }
            else if (type == typeof(char))
            {
                return "(char)" + s_rand.Next(char.MinValue, char.MaxValue).ToString();
            }
            else if (type == typeof(decimal))
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (type == typeof(double))
            {
                return s_rand.NextDouble().ToString();
            }
            else if (type == typeof(float))
            {
                return "(float)" + s_rand.NextDouble().ToString();
            }
            else if (type == typeof(int))
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (type == typeof(uint))
            {
                return "(uint)" + s_rand.Next(0, int.MaxValue).ToString();
            }
            else if (type == typeof(long))
            {
                return s_rand.Next(int.MinValue, int.MaxValue).ToString();
            }
            else if (type == typeof(ulong))
            {
                return "(ulong)" + s_rand.Next(0, int.MaxValue).ToString();
            }
            else if (type == typeof(short))
            {
                return "(short)" + s_rand.Next(short.MinValue, short.MaxValue).ToString();
            }
            else if (type == typeof(ushort))
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
            // TODO: validate payload too... have to rework a lot of stuff though.
            WriteLine("        static void ValidateEvent(TraceEvent traceEvent)");
            WriteLine("        {");
            //WriteLine("            Console.WriteLine($\"Attempting to validate event {traceEvent}\");");

            foreach (string name in eventSourceLayouts.Keys)
            {
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
                        WriteLine($"                    if ((int)traceEvent.ID != {eventLayout.ID}) Console.WriteLine($\"Expected ID {eventLayout.ID} but got ID {{(int)traceEvent.ID}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                        WriteLine($"                    if ((int)traceEvent.Version != {eventLayout.Version}) Console.WriteLine($\"Expected version {eventLayout.Version} but got version {{(int)traceEvent.Version}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                    }
                    else
                    {
                        WriteLine("                    // Skipping ID/Version validation because this EventSource is using SelfDescribing events.");
                    }

                    WriteLine($"                    if ((int)traceEvent.Level != {eventLayout.Level}) Console.WriteLine($\"Expected level {eventLayout.Level} but got level {{(int)traceEvent.Level}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                    WriteLine($"                    if ((int)traceEvent.Keywords != {eventLayout.Keywords}) Console.WriteLine($\"Expected keywords {eventLayout.Keywords} but got keywords{{(int)traceEvent.Keywords}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                    WriteLine($"                    if ((int)traceEvent.Opcode != {eventLayout.Opcode}) Console.WriteLine($\"Expected opcode {eventLayout.Opcode} but got opcode {{(int)traceEvent.Opcode}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                    WriteLine("                     ++s_successCount;");

                    WriteLine($"                    if (traceEvent.PayloadNames.Count() != {eventLayout.Arguments.Count}) {{ Console.WriteLine($\"Expected {eventLayout.Arguments.Count} payload items but got {{traceEvent.PayloadNames.Count()}} items for EventSource={layout.Name} Event={eventLayout.Name}\"); return; }}");
                    for (int i = 0; i < eventLayout.Arguments.Count; ++i)
                    {
                        WriteLine($"                    if (traceEvent.PayloadNames[{i}] != \"{eventLayout.Arguments[i].Name}\") Console.WriteLine($\"Expected argument name {eventLayout.Arguments[i].Name} but got name {{traceEvent.PayloadNames[{i}]}} for EventSource={layout.Name} Event={eventLayout.Name}\");");
                    }

                    WriteLine("                }");
                }

                WriteLine("            }");
            }

            WriteLine("        }");
            WriteLine("");
        }

        private static bool GetRandomBool()
        {
            return s_rand.Next(2) == 1;
        }
    }
}
