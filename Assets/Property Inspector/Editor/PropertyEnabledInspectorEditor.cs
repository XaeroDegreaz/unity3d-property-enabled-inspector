using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PropertyEnabledInspector
{
	/// <summary>
	/// This is a drop-in replacement of the Unity3D editor inspector that adds the ability to
	/// see, and work with class properties the same way you can with class fields.
	/// </summary>
	/// <remarks>
	/// High level overview:
	/// 
	/// First, mark your class with the <see cref="EnablePropertyInspectionAttribute"/>, and <see cref="SerializePrivateVariables"/> attributes. This will let the inspector know that it is
	/// allowed to initialize on your MonoBehaviour. Because this editor enables properties to be inspected on ALL MonoBehaviour
	/// derived classes, this is important (this way you have control over whether or not this is the default editor inspector for all MonoBehaviours).
	/// 
	/// 
	/// Secondly, simply mark any public property with the attribute <see cref="InspectAttribute"/>, and it will be shown in the inspector.
	/// This will work with auto-properties as well as manually managed property accessors / backing fields.
	/// 
	/// If you wish to forego using the Inspect attribute on all of your properties, you may construct the EnablePropertyInspection attribute like so:
	/// <example>
	/// <code>
	/// [EnablePropertyInspection( InspectorPermissions.AllNonPrivate )]
	/// </code>
	/// </example>
	/// 
	/// If you wish to ignore a property, you may mark it with the attributes <see cref="IgnoreAttribute"/>, or <see cref="HideInInspector"/>.
	/// 
	/// It is important that you mark your target class with with the <see cref="SerializePrivateVariables"/> attributes if you want to work
	/// with auto properties.
	/// 
	/// If you wish to explicitly name a backing field to use for a property, you may do so by setting the ExplicitBackingFieldName
	/// property of the <see cref="InspectAttribute"/> attribute, like so:
	/// <example>
	/// <code>
	/// [Inspect( "_myBackingFieldName" )]
	/// </code>
	/// </example>
	/// 
	/// or
	/// <example>
	/// <code>
	/// [Inspect( ExplicitBackingFieldName = "myBackingFieldName" )]
	/// </code>
	/// </example>
	/// 
	/// 
	/// If you're going to expose Array, or List *properties* in the inspector, you *MUST* use one of the above methods to name your backing field
	/// if you're not going to use an auto-property.
	/// </remarks>
	[CustomEditor( typeof ( MonoBehaviour ), true )]
	public class PropertyEnabledInspectorEditor : Editor
	{
		private IEnumerable<PropertyInfo> properties;
		private IEnumerable<FieldInfo> fields;
		private EnablePropertyInspectionAttribute permissionAttribute;
		private const BindingFlags publicFlags = BindingFlags.Instance | BindingFlags.Public;

		/// <summary>
		/// Cache our properties, and fields on startup.
		/// </summary>
		private void OnEnable()
		{
			var attributes = target.GetType().GetCustomAttributes( false );
			permissionAttribute = attributes.FirstOrDefault( a => a is EnablePropertyInspectionAttribute ) as EnablePropertyInspectionAttribute;

			if ( permissionAttribute == null )
			{
				return;
			}

#pragma warning disable 618
			var privateVariablesAttribute = attributes.FirstOrDefault( a => a is SerializePrivateVariables ) as SerializePrivateVariables;
#pragma warning restore 618

			if ( privateVariablesAttribute == null )
			{
				permissionAttribute = null;
				Debug.LogError( typeof ( EnablePropertyInspectionAttribute ) + " requires the SerializePrivateVariables attribute on your class. " );
				return;
			}


			var type = target.GetType();

			/*
			 * By default, we only want to expose public properties of the target's class.
			 * We do not expose public properties from parent class unless the properties are explicitly marked with
			 * an [Inspect] attribute. Ergo, you'd need access to the source of the parent class if you want them
			 * to be inspectable.
			 * 
			 * If we didn't have this restriction, then we'd inadvertently expose properties all the way up to the
			 * base MonoBehaviour properties, and that would be quite messy ;)
			 */
			properties = type.GetProperties( publicFlags )
							 .Where( property => predicateForAttribute<IgnoreAttribute>( property, 0 ) &&
												 predicateForAttribute<HideInInspector>( property, 0 ) &&
												 ( property.DeclaringType == target.GetType() || predicateForAttribute<InspectAttribute>( property ) ) );

			if ( permissionAttribute.InspectorPermission == InspectorPermissions.Selective )
			{
				properties = properties.Where( property => predicateForAttribute<InspectAttribute>( property ) );
			}

			//# This should cover all of the base cases for all public fields that are exposed by default in the Unity editor.
			fields = type.GetFields( publicFlags )
						 .Where( field => predicateForAttribute<NonSerializedAttribute>( field, 0 ) &&
										  predicateForAttribute<IgnoreAttribute>( field, 0 ) &&
										  predicateForAttribute<HideInInspector>( field, 0 ) )
						 .Distinct();
		}

		/// <summary>
		/// This method purposely does not call base.OnInspectorGUI(). We are serializing all private
		/// fields that are not marked with <see cref="NonSerializedAttribute"/>. This allows us to
		/// properly support auto-properties, and their compiler generated backing fields.
		/// 
		/// Public fields are shown at the top, public properties are shown below.
		/// </summary>
		public override void OnInspectorGUI()
		{
			if ( permissionAttribute == null )
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
		protected virtual object getMemberValueFromGUI( MemberInfo member )
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
				return EditorGUILayout.Vector2Field( niceName, (Vector2) value );
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
				serializedObject.Update();

				var result = EditorGUILayout.PropertyField( serializedObject.FindProperty( fieldName ), new GUIContent( niceName ), true );

				registerUndo( member );

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

		private bool predicateForAttribute<T>( MemberInfo member, int count = 1 ) where T : Attribute, new()
		{
			var t = new T();
			return member.GetCustomAttributes( t.GetType(), false ).Length == count;
		}

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

			serializedObject.Update();
			registerUndo( member );

			//# If we get this far, they are different; go ahead and assign gui to member.
			if ( isFieldInfo )
			{
				( (FieldInfo) member ).SetValue( target, guiValue );
			} else
			{
				( (PropertyInfo) member ).SetValue( target, guiValue, null );
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void registerUndo( MemberInfo member )
		{
			Undo.RecordObject( target, string.Format( "Inspector ({0}=>{1}.{2})", target.name, member.DeclaringType, member.Name ) );
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
				var attr = (InspectAttribute) obj.GetType()
												 .GetProperty( propertyName )
												 .GetCustomAttributes( typeof ( InspectAttribute ), false )
												 .FirstOrDefault();

				if ( attr == null || string.IsNullOrEmpty( attr.ExplicitBackingFieldName ) )
				{
					return null;
				}

				fieldInfo = obj.GetType()
							   .GetField( attr.ExplicitBackingFieldName, publicFlags | BindingFlags.NonPublic );

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
}