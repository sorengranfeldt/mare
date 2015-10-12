using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FIM.MARE
{
	[XmlInclude(typeof(Attribute)), XmlInclude(typeof(Constant))]
	public class Value
	{
		[XmlAttribute("DefaultValue")]
		public string DefaultValue { get; set; }

		[XmlAttribute("ActionOnNullSource")]
		[XmlTextAttribute()]
		public AttributeAction ActionOnNullSource { get; set; }

		[XmlElement("Transforms")]
		public Transforms Transforms { get; set; }

		public string Transform(string value)
		{
			if (this.Transforms != null)
			{
				Trace.TraceInformation("Transforming value: '{0}'", value);
				foreach (Transform t in Transforms.Transform)
				{
					Trace.TraceInformation("Input[{0}]: '{1}'", t.GetType(), value);
					value = t.Convert(value);
					Trace.TraceInformation("Output[{0}]: '{1}'", t.GetType(), value);
				}
			}
			else
			{
				Trace.TraceInformation("No transform entries");
			}
			Trace.TraceInformation("Returning value: '{0}'", value);
			return value;
		}

	}
	public class Attribute : Value
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		public string GetValueOrDefault(Direction direction, CSEntry csentry, MVEntry mventry)
		{
			string value = this.DefaultValue;
			bool sourceValueIsPresent = false;
			if (Name.Equals("[DN]") || Name.Equals("[RDN]") || Name.Equals("[ObjectType]"))
			{
				sourceValueIsPresent = true;
			}
			else
			{
				sourceValueIsPresent = direction.Equals(Direction.Import) ? csentry[Name].IsPresent : mventry[Name].IsPresent;
			}
			if (sourceValueIsPresent)
			{
				if (direction.Equals(Direction.Import))
				{
					switch (Name)
					{
						case "[DN]":
							value = csentry.DN.ToString();
							break;
						case "[RDN]":
							value = csentry.RDN;
							break;
						case "[ObjectType]":
							value = csentry.ObjectType;
							break;
						default:
							value = csentry[Name].Value;
							break;
					}
				}
				else
				{
					switch (Name)
					{
						case "[DN]":
							value = mventry.ObjectID.ToString();
							break;
						case "[ObjectType]":
							value = mventry.ObjectType;
							break;
						case "[RDN]":
							throw new Exception("[RDN] is not valid on MVEntry");
						default:
							value = mventry[Name].Value;
							break;
					}
				}
			}
			return value;
		}
		public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string Value)
		{
			AttributeType at = direction.Equals(Direction.Import) ? mventry[this.Name].DataType : csentry[this.Name].DataType;
			Trace.TraceInformation("Target attribute type: {0}", at);
			if (string.IsNullOrEmpty(Value))
			{
				switch (this.ActionOnNullSource)
				{
					case AttributeAction.None:
						throw new DeclineMappingException("No default action");
					case AttributeAction.Delete:
						if (direction.Equals(Direction.Import))
							mventry[this.Name].Delete();
						else
							csentry[this.Name].Delete();
						break;
					case AttributeAction.SetDefault:
						if (direction.Equals(Direction.Import))
							mventry[this.Name].Value = this.DefaultValue;
						else
							csentry[this.Name].Value = this.DefaultValue;
						break;
					default:
						throw new DeclineMappingException("No default action");
				}
			}
			else
			{
				if (direction.Equals(Direction.Import))
				{
					if (at == AttributeType.Reference)
					{
						throw new NotSupportedException("Cannot import to reference");
					}
					else
					{
						mventry[this.Name].Value = Value;
					}
				}
				else
				{
					switch (at)
					{
						case AttributeType.Reference:
							csentry[this.Name].ReferenceValue = csentry.MA.CreateDN(Value);
							break;
						case AttributeType.Integer:
							csentry[this.Name].IntegerValue = long.Parse(Value);
							break;
						default:
							csentry[this.Name].Value = Value;
							break;
					}
				}
			}
		}

	}
	public class Constant : Value
	{
		[XmlAttribute("Value")]
		public string Value { get; set; }
	}
	public class SourceExpression
	{
		[XmlElement("Source")]
		public List<Value> Source { get; set; }
	}
}
