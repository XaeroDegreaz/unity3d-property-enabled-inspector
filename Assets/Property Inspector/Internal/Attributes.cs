using System;
using System.Collections.Generic;

namespace PropertyEnabledInspector
{
	/// <summary>
	/// Enables inspection of class properties alongside class fields.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class )]
	public class EnablePropertyInspectionAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the permission level of the property inspector.
		/// <seealso cref="InspectorPermissions"/>
		/// </summary>
		public InspectorPermissions InspectorPermission { get; set; }

		public EnablePropertyInspectionAttribute() {}

		public EnablePropertyInspectionAttribute( InspectorPermissions inspectorPermission )
		{
			this.InspectorPermission = inspectorPermission;
		}
	}

	/// <summary>
	/// Explicitly allows a property to be inspected.
	/// </summary>
	[AttributeUsage( AttributeTargets.Property )]
	public class InspectAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the explicit string name of the backing field.
		/// This only necessary if your property is not-an auto property,
		/// but is of type <see cref="Array"/>, or <see cref="List{T}"/>.
		/// </summary>
		public string ExplicitBackingFieldName { get; set; }
		public InspectAttribute() {}

		public InspectAttribute( string explicitBackingFieldName )
		{
			this.ExplicitBackingFieldName = explicitBackingFieldName;
		}
	}

	/// <summary>
	/// Explicitly hides a property from the inspector.
	/// </summary>
	/// <remarks>
	/// Please note that this does not stop the private backing field from being serialized.
	/// If you are concerned about this, then you should not use an auto-property, and
	/// should mark your backing field with the <see cref="NonSerializedAttribute"/> attribute.
	/// </remarks>
	[AttributeUsage( AttributeTargets.Property )]
	public class IgnoreAttribute : Attribute {}
}