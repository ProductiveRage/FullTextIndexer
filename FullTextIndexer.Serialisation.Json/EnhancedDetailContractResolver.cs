using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FullTextIndexer.Serialisation.Json
{
    /// <summary>
    /// For applicable types, this will serialise a property with the full type name of the instance being serialised and will include all private properties and fields
    /// instead of just the public properties (note that some data can not be serialised and so will NOT be included - properties and fields that are delegates will be
    /// skipped over)
    /// </summary>
    public sealed class EnhancedDetailContractResolver : DefaultContractResolver
	{
		private readonly string _typeNameProperty;
		private readonly Predicate<Type> _typeFilter;
		public EnhancedDetailContractResolver(string typeNameProperty, Predicate<Type> typeFilter)
		{
			if (string.IsNullOrWhiteSpace(typeNameProperty))
				throw new ArgumentException($"Null/blank {nameof(typeNameProperty)} specified");
			if (typeFilter == null)
				throw new ArgumentNullException(nameof(typeFilter));

			_typeNameProperty = typeNameProperty;
			_typeFilter = typeFilter;

			// Ignore anything to do with [Serializable] or ISerializable, all serialisation should be handled explicitly by JSON.NET (in particular, it's important that
			// the ISerializable GetObjectData implementation of the EnglishPluralityStringNormaliser be ignored since that will write away the private data itself and
			// the CreateProperties method below won't be called - which means that the type name property won't be injected)
			IgnoreSerializableAttribute = true;
			IgnoreSerializableInterface = true;
		}

		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			if (!_typeFilter(type))
				return base.CreateProperties(type, memberSerialization);

			var props = GetTypePlusAnyInheritedTypes()
				.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				.Select(p => CreateProperty(p, memberSerialization))
				.Union(
					GetTypePlusAnyInheritedTypes()
						.SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
						.Select(f => CreateProperty(f, memberSerialization))
				)
				.ToList();
			foreach (var prop in props)
			{
				// If we try to serialise EVERYTHING then we can run into problems - for example, EnglishPluralityStringNormaliser generates a "_normaliser" Func that
				// will result in a frankly terrifying
				//   An unhandled exception of type 'System.ExecutionEngineException' occurred in Unknown Module.
				// failure if we try to serialise it. So, instead, try to serialise all private and public properties except for delegates (it's possible that there
				// are others that should be ignored but this will do for now - if similar problems are encountered in the future then maybe some more types-to-ignore
				// can get added here).
				// - Note: Not serialising these properties requires that the classes that have the delegate references are able to recreate them when instantiated,
				//   this is the case with the EnglishPluralityStringNormaliser (when using the BinaryFormatter to serialise/deserialise, it skips the private
				//   "_normaliser" property and rebuilds the Func when deserialised)
				var isSupportedPropertyType = !typeof(Delegate).IsAssignableFrom(prop.PropertyType);
				prop.Readable = isSupportedPropertyType;
				prop.Writable = isSupportedPropertyType;
			}

			// Add a phantom string property to every class which will resolve to the simple type name of the class (via the value provider) during serialization
			// (inspired by http://stackoverflow.com/a/24174253/3813189)
			props.Insert(0, new JsonProperty
			{
				DeclaringType = type,
				PropertyType = typeof(string),
				PropertyName = _typeNameProperty,
				ValueProvider = SimpleTypeNameProvider.Instance,
				Readable = true,
				Writable = false
			});
			return props;

			IEnumerable<Type> GetTypePlusAnyInheritedTypes()
			{
				var currentType = type;
				while (currentType != null)
				{
					yield return currentType;
					currentType = currentType.BaseType;
				}
			}
		}

		private sealed class SimpleTypeNameProvider : IValueProvider
		{
			private static SimpleTypeNameProvider _instance = new SimpleTypeNameProvider();
			public static SimpleTypeNameProvider Instance => _instance;
			private SimpleTypeNameProvider() { }
	
			public object GetValue(object target)
			{
				return target.GetType().AssemblyQualifiedName;
			}

			public void SetValue(object target, object value) { }
		}
	}
}
