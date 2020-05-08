using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

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
        public EventLevel Level { get; internal set; }
        public EventKeywords Keywords { get; internal set; }
        public EventOpcode Opcode { get; internal set; }
        public int Version { get; internal set; }
        public List<EventArgument> Arguments { get; internal set; }
        public int ID { get; internal set; }
    }

    class Program
    {
        static Random s_rand = new Random();

        static void Main(string[] args)
        {
            // TODO: once done testing, unleash the fury!
            //int numberOfEventSources      = 200;
            //int maxNumberOfEvents         = 5000;
            //int maxNumberOfEventArguments = 1000;
            int numberOfEventSources = 2;
            int maxNumberOfEvents = 5;
            int maxNumberOfEventArguments = 10;

            WritePreEventSourceFileContents();

            List<string> eventSourceNames = Enumerable.Range(0, numberOfEventSources).Select(i => "TestEventSource" + i.ToString()).ToList();
            Dictionary<string, EventSourceLayout> eventSourceLayouts = new Dictionary<string, EventSourceLayout>();
            foreach (string name in eventSourceNames)
            {
                EventSourceLayout layout = GenerateEventSource(name, maxNumberOfEvents, maxNumberOfEventArguments);
                eventSourceLayouts.Add(name, layout);
            }

            WritePostEventSourceFileContents(eventSourceLayouts);
        }

        private static void WritePreEventSourceFileContents()
        {
            Console.WriteLine("using System;");
            Console.WriteLine("using System.Collections;");
            Console.WriteLine("using System.Collections.Generic;");
            Console.WriteLine("using System.Collections.ObjectModel;");
            Console.WriteLine("using System.Diagnostics;");
            Console.WriteLine("using System.Diagnostics.Tracing;");
            Console.WriteLine("using System.Globalization;");
            Console.WriteLine("using System.IO;");
            Console.WriteLine("using System.Linq;");
            Console.WriteLine("using System.Runtime.CompilerServices;");
            Console.WriteLine("using System.Text;");
            Console.WriteLine("using System.Threading;");
            Console.WriteLine("using Microsoft.Diagnostics.NETCore.Client;");
            Console.WriteLine("using Microsoft.Diagnostics.Tracing;");
            Console.WriteLine("using Microsoft.Diagnostics.Tracing.Etlx;");
            Console.WriteLine("");
            Console.WriteLine("namespace EventSourceTest");
            Console.WriteLine("{");
        }

        private static EventSourceLayout GenerateEventSource(string name, int maxNumberOfEvents, int maxNumberOfEventArguments)
        {
            int numberOfEvents = s_rand.Next(maxNumberOfEvents);
            bool isSelfDescribing = GetRandomBool();

            Console.WriteLine($"    class {name}: EventSource");
            Console.WriteLine("    {");
            Console.WriteLine($"        public {name}()");

            if (isSelfDescribing)
            {
                Console.WriteLine("            : base(EventSourceSettings.EtwSelfDescribingEventFormat)");
            }

            Console.WriteLine("        {");
            Console.WriteLine("             // Empty constructor body");
            Console.WriteLine("        }");
            Console.WriteLine("");

            List<EventLayout> events = new List<EventLayout>();
            for (int i = 0; i < numberOfEvents; ++i)
            {
                int numberOfEventArgs = s_rand.Next(maxNumberOfEventArguments);
                events.Add(GenerateEvent(i, numberOfEventArgs, isSelfDescribing));
            }

            Console.WriteLine("    }");
            Console.WriteLine("");

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

            Console.WriteLine("");

            // Write EventAttribute
            Console.Write($"        [Event({id}");
            if (hasLevel)
            {
                level = s_rand.Next((int)EventLevel.Verbose);
                Console.Write($", Level = (EventLevel){level}");
            }

            if (hasKeywords)
            {
                keywords = s_rand.Next();
                Console.Write($", Keywords = (EventKeywords){keywords}");
            }

            if (hasOpcode)
            {
                opcode = s_rand.Next((int)EventOpcode.Receive);
                Console.Write($", Opcode = (EventOpcode){opcode}");
            }

            if (hasVersion)
            {
                version = s_rand.Next(50);
                Console.Write($", Version = {version}");
            }

            // End of EventAttribute
            Console.WriteLine(")]");

            string methodName = "TestEvent" + id.ToString();
            Console.Write($"        public void {methodName}(");
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
                Console.Write($"{argument.Type} {argument.Name}");
                if (i < arguments.Count - 1)
                {
                    Console.Write(", ");
                }
            }

            // End of argument list
            Console.WriteLine(")");

            Console.WriteLine("        {");
            
            Console.Write($"            WriteEvent({id}");
            if (arguments.Count > 0)
            {
                Console.Write(", ");
            }

            for (int i = 0; i < arguments.Count; ++i)
            {
                EventArgument argument = arguments[i];
                Console.Write(argument.Name);
                if (i < arguments.Count - 1)
                {
                    Console.Write(", ");
                }
            }

            // End of method call
            Console.WriteLine(");");

            Console.WriteLine("        }");

            return new EventLayout()
            {
                Name = methodName,
                ID = id,
                Level = (EventLevel)level,
                Keywords = (EventKeywords)keywords,
                Opcode = (EventOpcode)opcode,
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
            Console.WriteLine("    class Program");
            Console.WriteLine("    {");
            Console.WriteLine("        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)");
            Console.WriteLine("        {");
            Console.WriteLine("            int processId = Process.GetCurrentProcess().Id;");
            Console.WriteLine("            DiagnosticsClient client = new DiagnosticsClient(processId);");
            Console.WriteLine("            return client.StartEventPipeSession(providers, /* requestRunDown */ false);");
            Console.WriteLine("        }");

            GenerateValidateEvents(eventSourceLayouts);
            GenerateWriteEvents(eventSourceLayouts);

            Console.WriteLine("        static void Main(string[] args)");
            Console.WriteLine("        {");
            Console.WriteLine("            List<EventPipeProvider> providers = new List<EventPipeProvider>");
            Console.WriteLine("            {");
            Console.WriteLine("                new EventPipeProvider(\"Microsoft - Windows - DotNETRuntime\", EventLevel.Informational, (long)EventKeywords.All),");

            foreach (string name in eventSourceLayouts.Keys)
            {
                Console.WriteLine($"                new EventPipeProvider(\"{name}\", EventLevel.Informational, (long)EventKeywords.All),");
            }

            Console.WriteLine("            };");
            Console.WriteLine("");
            Console.WriteLine("            using (EventPipeSession session = AttachEventPipeSessionToSelf(providers))");
            Console.WriteLine("            {");
            Console.WriteLine("                WriteEvents();");
            Console.WriteLine("                ManualResetEvent allEventsReceivedEvent = new ManualResetEvent(false);");
            Console.WriteLine("");
            Console.WriteLine("                int eventCount = 0;");
            Console.WriteLine("                var source = new EventPipeEventSource(session.EventStream);");
            Console.WriteLine("                source.Dynamic.All += (TraceEvent traceEvent) =>");
            Console.WriteLine("                {");
            Console.WriteLine("                    ++eventCount;");
            Console.WriteLine("                    ValidateEvent(traceEvent);");
            Console.WriteLine("                };");

            Console.WriteLine("");
            Console.WriteLine("                Thread processingThread = new Thread(new ThreadStart(() =>");
            Console.WriteLine("                {");
            Console.WriteLine("                    source.Process();");
            Console.WriteLine("                }));");
            Console.WriteLine("");
            Console.WriteLine("                Console.WriteLine(\"Starting processing thread\");");
            Console.WriteLine("                processingThread.Start();");
            Console.WriteLine("");
            Console.WriteLine("                session.Stop();");
            Console.WriteLine("");
            Console.WriteLine("                Console.WriteLine(\"Joining processing thread\");");
            Console.WriteLine("                processingThread.Join();");
            Console.WriteLine("            }");
            Console.WriteLine("        }");
            Console.WriteLine("    }");
            Console.WriteLine("}");
        }

        private static void GenerateWriteEvents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            Console.WriteLine("        static void WriteEvents()");
            Console.WriteLine("        {");
            Console.WriteLine("            throw new NotImplementedException();");
            Console.WriteLine("        }");
            Console.WriteLine("");
        }

        private static void GenerateValidateEvents(Dictionary<string, EventSourceLayout> eventSourceLayouts)
        {
            Console.WriteLine("        static void ValidateEvent(TraceEvent traceEvent)");
            Console.WriteLine("        {");
            Console.WriteLine("            // TODO: implement");
            Console.WriteLine("            throw new NotImplementedException();");
            Console.WriteLine("        }");
            Console.WriteLine("");
        }

        private static bool GetRandomBool()
        {
            return s_rand.Next(2) == 1;
        }
    }
}
