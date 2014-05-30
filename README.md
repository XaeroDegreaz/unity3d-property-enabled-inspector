## Unity3D Property Enabled Inspector

This is a drop-in replacement of the Unity3D editor inspector that adds the ability to see, and work with class properties the same way you can with class fields. 

High level overview:

First, mark your class with the `[EnablePropertyInspectionAttribute]` and `[SerializePrivateVariables]` attributes. This will let the inspector know that it is allowed to initialize on your `MonoBehaviour`. Because this editor enables properties to be inspected on ALL `MonoBehaviour` derived classes, this is important (this way you have control over whether or not this is the default editor inspector for all MonoBehaviours).

Secondly, simply mark any public property with the attribute `[InspectAttribute]`, and it will be shown in the inspector. This will work with auto-properties as well as manually managed property accessors / backing fields.

If you wish to forego using the Inspect attribute on all of your properties, you may construct the `[EnablePropertyInspection]` attribute like so: 

```C#
[EnablePropertyInspection( [InspectorPermissions].AllNonPrivate )]
```

If you wish to ignore a property, you may mark it with the attributes `[IgnoreAttribute]`, or `[HideInInspector]`.

It is important that you mark your target class with with the `[SerializePrivateVariables]` attributes if you want to work with auto properties.

If you wish to explicitly name a backing field to use for a property, you may do so by setting the `ExplicitBackingFieldName` property of the `[InspectAttribute]` attribute, like so:

```C#
[Inspect( "_myBackingFieldName")]
```
or
```C#
[Inspect( ExplicitBackingFieldName = "myBackingFieldName )]
```

If you're going to expose `Array`, or `List` _properties_ in the inspector, you _MUST_ use one of the above methods to name your backing field if you're not going to use an auto-property.
