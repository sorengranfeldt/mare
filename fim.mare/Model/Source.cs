// october 16, 2015 | soren granfeldt
//	-added support for updating multivalues
//	-added multivalue attribute type
// januar 28, 2016 | soren granfeldt
//	-fixed two bugs for export flows in SetTargetValue (using mventry instead of csentry) and added .clear for multivalues
//	-fixed FromValueCollection to handle single value flowed to multivalued

using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;

namespace FIM.MARE
{
	public enum TransformDirection
	{
		Source,
		Target
	}
	[XmlInclude(typeof(Attribute)), XmlInclude(typeof(Constant)), XmlInclude(typeof(MultiValueAttribute))]
	public class Value
	{
		[XmlAttribute("DefaultValue")]
		public string DefaultValue { get; set; }

		[XmlAttribute("ActionOnNullSource")]
		[XmlTextAttribute()]
		public AttributeAction ActionOnNullSource { get; set; }

		[XmlElement("Transforms")]
		public Transforms Transforms { get; set; }

		public object Transform(object value, TransformDirection direction)
		{
			Tracer.TraceInformation("enter-transform {0}", direction);
			try
			{
				if (this.Transforms != null)
				{
					foreach (Transform t in Transforms.Transform)
					{
						Tracer.TraceInformation("input[{0}]: '{1}'", t.GetType().Name, value);
						value = t.Convert(value);
						Tracer.TraceInformation("output[{0}]: '{1}'", t.GetType().Name, value);
					}
				}
				else
				{
					Tracer.TraceInformation("no-transforms");
				}
				Tracer.TraceInformation("return-value: '{0}'", value);
			}
			catch (Exception ex)
			{
				Tracer.TraceError("transform {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Tracer.TraceInformation("exit-transform {0}", direction);
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
			if (Name.Equals("[DN]") || Name.Equals("[RDN]") || Name.Equals("[ObjectType]") || Name.Equals("[ConnectionChangeTime]"))
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
						case "[ConnectionChangeTime]":
							value = csentry.ConnectionChangeTime.ToString("yyyy-MM-ddTHH:mm:ss.000");
							break;
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
		public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, object Value)
		{
			AttributeType at = direction.Equals(Direction.Import) ? mventry[this.Name].DataType : csentry[this.Name].DataType;
			bool isMultivalued = direction.Equals(Direction.Import) ? mventry[this.Name].IsMultivalued : csentry[this.Name].IsMultivalued;
			Tracer.TraceInformation("target-attribute: name: {0}, type: {1}, is-multivalue: {2}", this.Name, at, isMultivalued);
			if (Value == null || string.IsNullOrEmpty(Value.ToString()))
			{
				switch (this.ActionOnNullSource)
				{
					case AttributeAction.None:
						Tracer.TraceInformation("decline-mapping-since-no-default-action");
						throw new DeclineMappingException("no-default-action");
					case AttributeAction.Delete:
						Tracer.TraceInformation("deleting-target-attribute-value: attr: {0}, type: {1}", this.Name, at);
						if (direction.Equals(Direction.Import))
							mventry[this.Name].Delete();
						else
							csentry[this.Name].Delete();
						break;
					case AttributeAction.SetDefault:
						Tracer.TraceInformation("set-target-attribute-default-value: attr: {0}, type: {1}, value: '{2}'", this.Name, at, this.DefaultValue);
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
				Tracer.TraceInformation("set-target-attribute-value: attr: {0}, type: {1}, value: '{2}', direction: {3}", this.Name, at, Value, direction);
				if (direction.Equals(Direction.Import))
				{
					if (at == AttributeType.Reference)
					{
						Exception ex = new NotSupportedException("cannot-import-to-reference");
						Tracer.TraceError("mapping-exception {0}", ex.GetBaseException());
						throw ex;
					}
					else
					{
						if (isMultivalued)
						{
							mventry[this.Name].Values.Clear();
							foreach (object val in FromValueCollection(Value))
								mventry[this.Name].Values.Add(val as string);
						}
						else
						{
							mventry[this.Name].Value = Value as string;
						}
					}
				}
				else
				{
					switch (at)
					{
						case AttributeType.Reference:
							csentry[this.Name].ReferenceValue = csentry.MA.CreateDN(Value as string);
							break;
						case AttributeType.Integer:
							if (isMultivalued)
							{
								csentry[this.Name].Values.Clear();
								foreach (object val in FromValueCollection(Value))
									csentry[this.Name].Values.Add(long.Parse(Value as string));
							}
							else
							{
								csentry[this.Name].IntegerValue = long.Parse(Value as string);
							}
							break;
						default:
							if (isMultivalued)
							{
								csentry[this.Name].Values.Clear();
                                foreach (object val in FromValueCollection(Value))
									csentry[this.Name].Values.Add(val as string);
							}
							else
							{
								csentry[this.Name].Value = Value as string;
							}
							break;
					}
				}
			}
		}
		protected List<object> FromValueCollection(object value)
		{
			List<object> values = new List<object>();
			if (value.GetType() == typeof(List<object>))
			{
				Tracer.TraceInformation("already-type-list");
				values = (List<object>)value;
			}
			else
			{
				Tracer.TraceInformation("source-converting-to-list");
				if (value.GetType() == typeof(System.String))
				{
					values.Add(value);
					return values;
				}
				ValueCollection vc = (ValueCollection)value;
				foreach (Microsoft.MetadirectoryServices.Value val in vc)
				{
					values.Add(val.ToString());
				}
			}
			return values;
		}
	}
	public class Constant : Value
	{
		[XmlAttribute("Value")]
		public string Value { get; set; }
	}
	public class MultiValueAttribute : Value
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }
		public object GetValueOrDefault(Direction direction, CSEntry csentry, MVEntry mventry)
		{
			object value = this.DefaultValue;
			bool sourceValueIsPresent = direction.Equals(Direction.Import) ? csentry[Name].IsPresent : mventry[Name].IsPresent;
			if (direction.Equals(Direction.Import))
			{
				value = csentry[Name].Values;
			}
			else
			{
				value = mventry[Name].Values;
			}
			return value;
		}
	}

	public class SourceExpression
	{
		[XmlElement("Source")]
		public List<Value> Source { get; set; }
	}

}