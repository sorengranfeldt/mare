// Version History
// March 20, 2014 | Soren Granfeldt
//  - initiated

using System;
using Microsoft.MetadirectoryServices;
using System.Security.Principal;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Granfeldt
{
    /// <summary>
    /// Summary description for MAExtensionObject.
    /// </summary>
    public class SampleRulesExtension : IMASynchronization
    {

        public SampleRulesExtension()
        {
        }

        void IMASynchronization.Initialize()
        {
        }

        void IMASynchronization.Terminate()
        {
        }

        bool IMASynchronization.ShouldProjectToMV(CSEntry csentry, out string MVObjectType)
        {
            //
            // TODO: Remove this throw statement if you implement this method
            //
            throw new EntryPointNotImplementedException();
        }

        DeprovisionAction IMASynchronization.Deprovision(CSEntry csentry)
        {
            //
            // TODO: Remove this throw statement if you implement this method
            //
            throw new EntryPointNotImplementedException();
        }

        bool IMASynchronization.FilterForDisconnection(CSEntry csentry)
        {
            //
            // TODO: write connector filter code
            //
            throw new EntryPointNotImplementedException();
        }

        void IMASynchronization.MapAttributesForJoin(string FlowRuleName, CSEntry csentry, ref ValueCollection values)
        {
            //
            // TODO: write join mapping code
            //
            throw new EntryPointNotImplementedException();
        }

        bool IMASynchronization.ResolveJoinSearch(string joinCriteriaName, CSEntry csentry, MVEntry[] rgmventry, out int imventry, ref string MVObjectType)
        {
            //
            // TODO: write join resolution code
            //
            throw new EntryPointNotImplementedException();
        }

        void IMASynchronization.MapAttributesForImport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
        {
            switch (FlowRuleName)
            {
                case "City":
                    mventry["city"].Value = new Guid().ToString();
                    break;
                default:
                    throw new NotImplementedException("Import flow rule" + FlowRuleName + " not found");
            }
        }

        void IMASynchronization.MapAttributesForExport(string FlowRuleName, MVEntry mventry, CSEntry csentry)
        {
            switch (FlowRuleName)
            {
                default:
                    throw new NotImplementedException("Export flow rule" + FlowRuleName + " not found");
            }
        }
    }
}
