namespace PropertyEnabledInspector
{
	/// <summary>
	/// Various modes of inspector permissions. Defaults to Selective.
	/// </summary>
	public enum InspectorPermissions
	{
		/// <summary>
		/// You must manually allow your properties to be inspected by using the
		/// <see cref="InspectAttribute"/> attribute.
		/// </summary>
		Selective,
		/// <summary>
		/// Will automatically show all public properties in inspector.
		/// </summary>
		AllNonPrivate
	}

}