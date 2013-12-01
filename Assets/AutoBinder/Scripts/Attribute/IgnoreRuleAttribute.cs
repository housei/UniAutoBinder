using System;

namespace UniAutoBinder
{
	[AttributeUsage(
		AttributeTargets.Method,
		AllowMultiple=false)
	]
	public class IgnoreRuleAttribute : Attribute
	{
			
	}

}