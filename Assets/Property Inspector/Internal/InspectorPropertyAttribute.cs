using System;
using UnityEngine;

[AttributeUsage( AttributeTargets.Property )]
public class InspectorPropertyAttribute : Attribute
{
	public string ExplicitBackingFieldName { get; set; }
	public InspectorPropertyAttribute() {}

	public InspectorPropertyAttribute( string explicitBackingFieldName )
	{
		this.ExplicitBackingFieldName = explicitBackingFieldName;
	}
}

[AttributeUsage( AttributeTargets.Class )]
public class EnablePropertyInspection : Attribute {}