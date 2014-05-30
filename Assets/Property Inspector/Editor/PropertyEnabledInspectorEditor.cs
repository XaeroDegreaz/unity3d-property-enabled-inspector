using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// This editor class makes available the inclusion of class properties inside the inspector.
/// 
/// First, mark your class with the <see cref="EnablePropertyInspection"/>, and <see cref="SerializePrivateVariables"/> attributes. This will let the inspector know that it is
/// allowed to initialize on your MonoBehaviour. Because this editor enables properties to be inspected on ALL MonoBehaviour
/// derived classes, this is important (this way you have control over whether or not this is the default editor inspector for all MonoBehaviours).
/// 
/// Secondly, simply mark any public property with the attribute <see cref="InspectorPropertyAttribute"/>, and it will be shown in the inspector.
/// This will work with auto-properties as well as manually managed property accessors / backing fields.
/// 
/// It is important that you mark your target class with with the <see cref="SerializePrivateVariables"/> attributes if you want to work
/// with auto properties.
/// 
/// If you wish to explicitly name a backing field to use for a property, you may do so by setting the ExplicitBackingFieldName
/// property of the <see cref="InspectorPropertyAttribute"/> attribute, like so:
/// <example>
/// [InspectorProperty("_myBackingFieldName")]
/// </example>
/// 
/// or
/// <example>
/// [InspectorProperty(ExplicitBackingFieldName = "myBackingFieldName")]
/// </example>
/// 
/// If you're going to expose Array, or List *properties* in the inspector, you *MUST* use one of the above methods to name your backing field
/// if you're not going to use an auto-property.
/// </summary>
[CustomEditor( typeof ( MonoBehaviour ), true )]
public class PropertyEnabledInspectorEditor : Editor
{
	private IEnumerable<PropertyInfo> properties;
	private IEnumerable<FieldInfo> fields;
	private bool hasPermissionToRun = false;
	private bool hasSerializePrivateVariables = false;

	/// <summary>
	/// Cache our properties, and fields on startup.
	/// </summary>
	private void OnEnable()
	{
		hasPermissionToRun = target.GetType()
								   .GetCustomAttributes( typeof ( EnablePropertyInspection ), false )
								   .Count() == 1;
		if ( !hasPermissionToRun )
		{
			return;
		}

		hasSerializePrivateVariables = target.GetType()
											 .GetCustomAttributes( typeof ( SerializePrivateVariables ), false )
											 .Count() == 1;
		if ( !hasSerializePrivateVariables )
		{
			Debug.LogError( "PropertyEnabledInspectorEditor requires the SerializePrivateVariables attribute on your class. " );
			hasPermissionToRun = false;
		}

		properties = target.GetType()
						   .GetProperties( BindingFlags.Instance | BindingFlags.Public )
						   .Where( p => p.GetCustomAttributes( typeof ( InspectorPropertyAttribute ), false ).Length == 1 );
		fields = target.GetType()
					   .GetFields( BindingFlags.Instance | BindingFlags.Public )
					   .Where( f => f.GetCustomAttributes( typeof ( NonSerializedAttribute ), false ).Length == 0 );
	}

	/// <summary>
	/// This method purposely does not call base.OnInspectorGUI(). We are serializing all private
	/// fields that are not marked with <see cref="NonSerializedAttribute"/>. This allows us to
	/// properly support auto-properties, and their compiler generated backing fields.
	/// </summary>
	public override void OnInspectorGUI()
	{
		if ( !hasPermissionToRun || !hasSerializePrivateVariables )
		{
			base.OnInspectorGUI();
			return;
		}

		//# Try to perform in such a way that it resembles the default inspector since we
		//# cannot call base.OnInspectorGUI (because we are serializing private variables for property backing fields).
		foreach ( var field in fields )
		{
			var guiValue = getMemberValueFromGUI( field );

			//# We need to skip attempting to assign member values for arrays, and lists as these are handled by
			//# unity's serializer.
			if ( typeof ( object[] ).IsAssignableFrom( field.FieldType ) || field.GetValue( target ) is IList )
			{
				continue;
			}

			updateMemberValue( field, guiValue );
		}

		foreach ( var property in properties )
		{
			var guiVal = getMemberValueFromGUI( property );

			//# We need to skip attempting to assign member values for arrays, and lists as these are handled by
			//# unity's serializer.
			if ( typeof ( object[] ).IsAssignableFrom( property.PropertyType ) || property.GetValue( target, null ) is IList )
			{
				continue;
			}

			updateMemberValue( property, guiVal );
		}
	}

	#region EditorGUI Generation Code

	/// <summary>
	/// Retrieve a value from an EditorGUI control that corresponds to the target <see cref="MemberInfo"/>
	/// </summary>
	/// <param name="member">The member of the class being inspected.</param>
	/// <returns></returns>
	private object getMemberValueFromGUI( MemberInfo member )
	{
		var niceName = ObjectNames.NicifyVariableName( member.Name );
		Type type;
		object value;

		if ( member is FieldInfo )
		{
			value = ( (FieldInfo) member ).GetValue( target );
			type = ( (FieldInfo) member ).FieldType;
		} else
		{
			value = ( (PropertyInfo) member ).GetValue( target, null );
			type = ( (PropertyInfo) member ).PropertyType;
		}

		if ( type == typeof ( string ) )
		{
			return EditorGUILayout.TextField( niceName, (string) value );
		}

		if ( type == typeof ( int ) )
		{
			return EditorGUILayout.IntField( niceName, (int) value );
		}

		if ( type == typeof ( float ) || type == typeof ( double ) )
		{
			return EditorGUILayout.FloatField( niceName, (float) value );
		}

		if ( type == typeof ( bool ) )
		{
			return EditorGUILayout.Toggle( niceName, (bool) value );
		}

		if ( type == typeof ( Vector2 ) )
		{
			return EditorGUILayout.Vector3Field( niceName, (Vector2) value );
		}

		if ( type == typeof ( Vector3 ) )
		{
			return EditorGUILayout.Vector3Field( niceName, (Vector3) value );
		}

		if ( type == typeof ( Vector4 ) )
		{
			return EditorGUILayout.Vector4Field( niceName, (Vector4) value );
		}

		if ( typeof ( object[] ).IsAssignableFrom( type ) || value is IList )
		{
			var fieldName = member.Name;

			//# Arrays, and Lists need a bit more checking done when they are properties.
			if ( member is PropertyInfo )
			{
				var backingField = getBackingField( target, member.Name );

				if ( backingField == null )
				{
					return null;
				}

				fieldName = backingField.Name;
			}

			EditorGUI.BeginChangeCheck();

			var result = EditorGUILayout.PropertyField( serializedObject.FindProperty( fieldName ), new GUIContent( niceName ),
														true );

			if ( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
			}

			return result;
		}

		//# Catch-all for all other Components / Unity Objects
		if ( typeof ( Object ).IsAssignableFrom( type ) )
		{
			return EditorGUILayout.ObjectField( niceName, (Object) value, type, true );
		}

		return null;
	}

	#endregion

	#region Utility Methods

	/// <summary>
	/// Updates the member value with what's inside the corresponding EditorGUI control.
	/// </summary>
	/// <param name="member">The member of the class being inspected.</param>
	/// <param name="guiValue">The value of the corresponding EditorGUI control</param>
	private void updateMemberValue( MemberInfo member, object guiValue )
	{
		bool isFieldInfo = member is FieldInfo;
		object memberValue = ( isFieldInfo )
							 ? ( (FieldInfo) member ).GetValue( target )
							 : ( (PropertyInfo) member ).GetValue( target, null );

		//# Same instance
		if ( guiValue == memberValue )
		{
			return;
		}

		//# Same hashcode
		if ( guiValue != null && memberValue != null && guiValue.GetHashCode() == memberValue.GetHashCode() )
		{
			return;
		}

		//# If we get this far, they are different; go ahead and assign gui to member.
		if ( isFieldInfo )
		{
			( (FieldInfo) member ).SetValue( target, guiValue );
		} else
		{
			( (PropertyInfo) member ).SetValue( target, guiValue, null );
		}
	}

	private string getAutoBackingFieldName( string propertyName )
	{
		return string.Format( "<{0}>k__BackingField", propertyName );
	}

	private FieldInfo getBackingField( object obj, string propertyName )
	{
		//# Check for a compiler generated auto backing field.
		var fieldInfo = obj.GetType()
						   .GetField( getAutoBackingFieldName( propertyName ), BindingFlags.Instance | BindingFlags.NonPublic );

		//# If no auto field is found, we check the attribute to see if there is an explicitly named backing field for non-auto properties.
		if ( fieldInfo == null )
		{
			var attr = (InspectorPropertyAttribute) obj.GetType()
													   .GetProperty( propertyName )
													   .GetCustomAttributes( typeof ( InspectorPropertyAttribute ), false )
													   .FirstOrDefault();

			if ( attr == null || string.IsNullOrEmpty( attr.ExplicitBackingFieldName ) )
			{
				return null;
			}

			fieldInfo = obj.GetType()
						   .GetField( attr.ExplicitBackingFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );

			if ( fieldInfo == null )
			{
				return null;
			}

			return fieldInfo;
		}

		return fieldInfo;
	}

	#endregion
}
