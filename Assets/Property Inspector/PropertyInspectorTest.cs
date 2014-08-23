using System;
using System.Collections.Generic;
using PropertyEnabledInspector;
using UnityEngine;

#pragma warning disable 618
[SerializePrivateVariables]
#pragma warning restore 618
[EnablePropertyInspection( InspectorPermissions.AllNonPrivate )]
public class PropertyInspectorTest : MonoBehaviour
{
	#region Private Backing Fields

	private List<string> _testList;

	/// <summary>
	/// This is setup like a public backing field for the SomeInt property 
	/// to demonstrate it's working in the editor.
	/// </summary>
	private int _someIntField;

	[NonSerialized]
	private string _anotherIgnore;

	#endregion

	#region Private Instance Fields

	//# Because of [SerializePrivateVariables], you should manually mark the private
	//# fields that you do not want to be serialized.
	[NonSerialized]
	private string _doNotSerializeMe;

	#endregion

	#region Public Instance Fields

	public Vector3 SomeVector;
	public string SomeString;

	#endregion

	#region Public Properties

	//# Arrays, and Lists that are not auto-properties must declare their backing field.
	[Inspect( "_testList" )]
	public List<string> TestList
	{
		get { return _testList; }
		set { _testList = value; }
	}

	//# Arrays, and Lists that are auto-properties are no problem.
	public List<Transform> TestAutoList { get; set; }

	//# No Inspect attribute needed because of [EnablePropertyInspection( InspectorPermissions.AllNonPrivate )]
	public int SomeInt
	{
		get { return _someIntField; }
		set { _someIntField = value; }
	}

	//# Auto-properties are no problem.
	public bool IsDead { get; set; }

	//# As the attribute implies, this property will not be shown in inspector.
	[Ignore]
	public bool IsIgnore { get; set; }

	//# Set this property to ignore, and also mark its backing field as NonSerialized
	[Ignore]
	public string AnotherIgnore
	{
		get { return _anotherIgnore; }
		set { _anotherIgnore = value; }
	}

	#endregion
}
