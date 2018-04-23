// april 23, 2018 | soren granfeldt
//  - added Not transform

using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{

    public class Not : Transform
    {
        public override object Convert(object value)
        {
            if (value == null) return value;
            bool boolValue = bool.Parse(value as string);
            value = (!boolValue).ToString();
            return value;
        }
    }
 
}
