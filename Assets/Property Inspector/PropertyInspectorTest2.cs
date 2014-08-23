using System.Collections.Generic;
using PropertyEnabledInspector;
using UnityEngine;

#pragma warning disable 618
[SerializePrivateVariables]
#pragma warning restore 618
[EnablePropertyInspection]
public class PropertyInspectorTest2 : MonoBehaviour
{
	#region Private Backing Fields

	private List<string> _testList;

	/// <summary>
	/// This is setup like a public backing field for the SomeInt property 
	/// to demonstrate it's working in the editor.
	/// </summary>
	private int _someIntField;

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
	[Inspect]
	public List<Transform> TestAutoList { get; set; }

	//# Will not show up without Inspect attribute.
	public int SomeInt
	{
		get { return _someIntField; }
		set { _someIntField = value; }
	}

	//# Will not show up without Inspect attribute.
	public bool IsDead { get; set; }

	//# Will not show up without Inspect attribute (Ignore is redundant).
	[Ignore]
	public bool IsIgnore { get; set; }

	#endregion
}
