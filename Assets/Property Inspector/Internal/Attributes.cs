using System;

namespace PropertyEnabledInspector
{
	[AttributeUsage( AttributeTargets.Class )]
	public class EnablePropertyInspectionAttribute : Attribute
	{
		public InspectionModes InspectionMode = InspectionModes.Selective;

		public EnablePropertyInspectionAttribute() {}

		public EnablePropertyInspectionAttribute( InspectionModes inspectionMode )
		{
			this.InspectionMode = inspectionMode;
		}
	}

	[AttributeUsage( AttributeTargets.Property )]
	public class InspectAttribute : Attribute
	{
		public string ExplicitBackingFieldName { get; set; }
		public InspectAttribute() {}

		public InspectAttribute( string explicitBackingFieldName )
		{
			this.ExplicitBackingFieldName = explicitBackingFieldName;
		}
	}

	[AttributeUsage( AttributeTargets.Property )]
	public class IgnoreAttribute : Attribute {}
}