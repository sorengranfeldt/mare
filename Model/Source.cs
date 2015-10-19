using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			Trace.TraceInformation("enter-transform");
			Trace.Indent();
			try
			{
				if (this.Transforms != null)
				{
					foreach (Transform t in Transforms.Transform)
					{
						Trace.TraceInformation("input[{0}]: '{1}'", t.GetType().Name, value);
						value = t.Convert(value);
						Trace.TraceInformation("output[{0}]: '{1}'", t.GetType().Name, value);
					}
				}
				else
				{
					Trace.TraceInformation("no-transforms");
				}
				Trace.TraceInformation("return-value: '{0}'", value);
			}
			catch (Exception ex)
			{
				Trace.TraceError("transform {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-transform");
			}
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
							throw new NotSupportedException("[RDN] is not valid on MVEntry");
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
			if (string.IsNullOrEmpty(Value))
			{
				switch (this.ActionOnNullSource)
				{
					case AttributeAction.None:
						Trace.TraceInformation("decline-mapping-since-no-default-action");
						throw new DeclineMappingException("no-default-action");
					case AttributeAction.Delete:
						Trace.TraceInformation("deleting-target-attribute-value: attr: {0}, type: {1}", this.Name, at);
						if (direction.Equals(Direction.Import))
							mventry[this.Name].Delete();
						else
							csentry[this.Name].Delete();
						break;
					case AttributeAction.SetDefault:
						Trace.TraceInformation("set-target-attribute-default-value: attr: {0}, type: {1}, value: '{2}'", this.Name, at, this.DefaultValue);
						if (direction.Equals(Direction.Import))
							mventry[this.Name].Value = this.DefaultValue;
						else
							csentry[this.Name].Value = this.DefaultValue;
						break;
					default:
						throw new DeclineMappingException("no-default-action");
				}
			}
			else
			{
				Trace.TraceInformation("set-target-attribute-value: attr: {0}, type: {1}, value: '{2}'", this.Name, at, Value);
				if (direction.Equals(Direction.Import))
				{
					if (at == AttributeType.Reference)
					{
						Exception ex = new NotSupportedException("cannot-import-to-reference");
						Trace.TraceError("mapping-exception {0}", ex.GetBaseException());
						throw ex;
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
