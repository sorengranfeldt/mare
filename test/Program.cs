using FIM.MARE;
using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        public class AttribDetached : Attrib
        {
            public override byte[] BinaryValue
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override bool BooleanValue
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override AttributeType DataType
            {
                get { throw new NotImplementedException(); }
            }

            public override void Delete()
            {
                throw new NotImplementedException();
            }

            public override long IntegerValue
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override bool IsMultivalued
            {
                get { throw new NotImplementedException(); }
            }

            public override bool IsPresent
            {
                get { throw new NotImplementedException(); }
            }

            public override Microsoft.MetadirectoryServices.ManagementAgent LastContributingMA
            {
                get { throw new NotImplementedException(); }
            }

            public override DateTime LastContributionTime
            {
                get { throw new NotImplementedException(); }
            }

            public override string Name
            {
                get { throw new NotImplementedException(); }
            }

            public override ReferenceValue ReferenceValue
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string StringValue
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string Value
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override ValueCollection Values
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class ManagementAgentDetached : Microsoft.MetadirectoryServices.ManagementAgent
        {

            public override ReferenceValue CreateDN(string dn)
            {
                throw new NotImplementedException();
            }

            public override ReferenceValue EscapeDNComponent(params string[] parts)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { return "AD"; }
            }

            public override string NormalizeString(string value)
            {
                throw new NotImplementedException();
            }

            public override string[] UnescapeDNComponent(string component)
            {
                throw new NotImplementedException();
            }

            public override ReferenceValue CreateDN(Microsoft.MetadirectoryServices.Value dn)
            {
                throw new NotImplementedException();
            }

            public override ReferenceValue EscapeDNComponent(params Microsoft.MetadirectoryServices.Value[] parts)
            {
                throw new NotImplementedException();
            }
        }

        public class cs : CSEntry
        {
            public ManagementAgentDetached privateMA;

            public override void CommitNewConnector()
            {
                throw new NotImplementedException();
            }

            public override DateTime ConnectionChangeTime
            {
                get { throw new NotImplementedException(); }
            }

            public override RuleType ConnectionRule
            {
                get { throw new NotImplementedException(); }
            }

            public override ConnectionState ConnectionState
            {
                get { throw new NotImplementedException(); }
            }

            public override ReferenceValue DN
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Deprovision()
            {
                throw new NotImplementedException();
            }

            public override AttributeNameEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public override Microsoft.MetadirectoryServices.ManagementAgent MA
            {
                get { return new ManagementAgentDetached(); }
            }

            public override ValueCollection ObjectClass
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string ObjectType
            {
                get { throw new NotImplementedException(); }
            }

            public override string RDN
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string ToString()
            {
                throw new NotImplementedException();
            }

            public override Attrib this[string attributeName]
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class mv : MVEntry
        {
            public override ConnectedMACollection ConnectedMAs
            {
                get { throw new NotImplementedException(); }
            }

            public override AttributeNameEnumerator GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public override Guid ObjectID
            {
                get { throw new NotImplementedException(); }
            }

            public override string ObjectType
            {
                get { throw new NotImplementedException(); }
            }

            public override string ToString()
            {
                throw new NotImplementedException();
            }

            public override Attrib this[string attributeName]
            {
                get
                {
                    AttribDetached ad = new AttribDetached();
                    throw new NotImplementedException();
                }
            }
        }

        static void Main(string[] args)
        {
            // http://blogs.msdn.com/b/sergeim/archive/2008/12/10/how-to-do-etw-logging-from-net-application.aspx 
            // debugging tools - http://msdn.microsoft.com/en-US/windows/desktop/bg162891 
            // EventProviderTraceListener listener = new EventProviderTraceListener(providerId.ToString());
            //TraceSource source = new TraceSource("FIM MA Rules Extension", SourceLevels.All);
            //source.Listeners.Add(listener);
            //source.TraceData(TraceEventType.Warning | TraceEventType.Start, 2, new object[] { "abc", "def", true, 123 });
            //source.TraceEvent(TraceEventType.Warning, 12, "Provider guid: {0}", new object[] { providerId });
            //source.TraceEvent(TraceEventType.Information, 12, "Null value: {0}", nuller);
            //source.TraceInformation("string {0}, bool {1}, int {2}, ushort {3}", new object[] { "abc", false, 123, (UInt32)5 });

            //source.TraceInformation("This is sample trace record ");
            //source.TraceEvent(TraceEventType.Error, 1, "This is a plain error");

            RulesExtension re = new RulesExtension();
            List<FIM.MARE.ManagementAgent> MAs = re.config.ManagementAgent;
            List<FlowRule> rules = re.config.ManagementAgent.FirstOrDefault().FlowRule;

            cs c = new cs();
            mv m = new mv();

            //instance.MapAttributesForImport(rules.Where(rule => rule.Name == "lastLogonTime").FirstOrDefault().Name, c, m);

            //re.MapAttributesForImportExportDetached(rules.Where(rule => rule.Name == "ObjectGuidToString").FirstOrDefault().Name, c, m);


        }
    }
}
