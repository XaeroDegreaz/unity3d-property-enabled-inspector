using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[SerializePrivateVariables]
[EnablePropertyInspection]
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
	public int SomeIntField;
	public Vector4 Quat;
	public GameObject[] TestArray;

	//# Will not be inspected.
	public Dictionary<string, string> TestDict;

	#endregion

	#region Public Properties

	[InspectorProperty( ExplicitBackingFieldName = "_testList" )]
	public List<string> TestList
	{
		get { return _testList; }
		set { _testList = value; }
	}

	[InspectorProperty]
	public int SomeInt
	{
		get { return SomeIntField; }
		set { SomeIntField = value; }
	}

	[InspectorProperty]
	public List<Transform> TestAutoList { get; set; }

	[InspectorProperty]
	public int SomeInt2 { get; set; }

	[InspectorProperty]
	public GameObject SomeGO { get; set; }

	[InspectorProperty]
	public Vector3 SomeVec { get; set; }

	[InspectorProperty]
	public bool HasRabies { get; set; }

	#endregion
}
