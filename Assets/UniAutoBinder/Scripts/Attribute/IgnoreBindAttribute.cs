using System;

namespace UniAutoBinder
{
	[AttributeUsage(
		AttributeTargets.Field,
		AllowMultiple=false)
	]	
	public class IgnoreBindAttribute:Attribute
	{
		
	}
}