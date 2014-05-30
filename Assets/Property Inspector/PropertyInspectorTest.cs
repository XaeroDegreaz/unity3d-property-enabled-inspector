using System;
using System.Collections.Generic;
using PropertyEnabledInspector;
using UnityEngine;

[SerializePrivateVariables]
[EnablePropertyInspection( InspectorPermissions.AllNonPrivate )]
public class PropertyInspectorTest : MonoBehaviour
{
	#region Private Backing Fields

	private List<string> _testList;

	#endregion

	#region Public Instance Fields

	/// <summary>
	/// This is setup like a public backing field for the SomeInt property 
	/// to demonstrate it's working in the editor.
	/// </summary>
	private int _someIntField;

	private Vector4 Quat;
	private GameObject[] TestArray;

	#endregion

	#region Public Properties

	[Inspect( "_testList" )]
	public List<string> TestList
	{
		get { return _testList; }
		set { _testList = value; }
	}

	public int SomeInt
	{
		get { return _someIntField; }
		set { _someIntField = value; }
	}

	public bool IsDead { get; set; }

	[Ignore]
	public bool IsIgnore { get; set; }

	public List<Transform> TestAutoList { get; set; }

	#endregion
}
