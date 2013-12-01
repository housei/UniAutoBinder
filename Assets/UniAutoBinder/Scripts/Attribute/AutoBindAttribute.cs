using System;

namespace UniAutoBinder
{
	[AttributeUsage(
		AttributeTargets.Class|AttributeTargets.Field,
		AllowMultiple=false,
		Inherited=true )
	]
	public class AutoBindAttribute:Attribute
	{
		public string name;
		public bool searchParent;
		
		public AutoBindAttribute(string name=null,bool searchParent=false)
		{
			this.name = name;
			this.searchParent = searchParent;
		}
		
	}
}
