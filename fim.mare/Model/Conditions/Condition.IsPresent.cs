// july 11, 2018, soren granfeldt
//	- added IsPresent condition

using Microsoft.MetadirectoryServices;

namespace FIM.MARE
{

	public class IsPresent : ConditionBase
	{
		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			if (Source.Equals(EvaluateAttribute.CSEntry))
			{
				return csentry[AttributeName].IsPresent;
			}
			if (Source.Equals(EvaluateAttribute.MVEntry))
			{
				return mventry[AttributeName].IsPresent;
			}
			return false; // we should never get here
		}
	}

}
