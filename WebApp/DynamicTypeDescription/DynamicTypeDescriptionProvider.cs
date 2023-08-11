using System.ComponentModel;

namespace WebApp.DynamicTypeDescription
{
	public class DynamicTypeDescriptionProvider : TypeDescriptionProvider
	{
		private readonly TypeDescriptionProvider _provider;
		private readonly List<PropertyDescriptor> _properties = new();

		public DynamicTypeDescriptionProvider(Type type)
		{
			_provider = TypeDescriptor.GetProvider(type);
		}

		public IList<PropertyDescriptor> Properties => _properties;

		public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
		{
			return new DynamicCustomTypeDescriptor(this, _provider.GetTypeDescriptor(objectType, instance));
		}

		private class DynamicCustomTypeDescriptor : CustomTypeDescriptor
		{
			private readonly DynamicTypeDescriptionProvider provider;

			public DynamicCustomTypeDescriptor(DynamicTypeDescriptionProvider provider, ICustomTypeDescriptor? descriptor)
				  : base(descriptor)
			{
				this.provider = provider;
			}

			public override PropertyDescriptorCollection GetProperties()
			{
				return GetProperties(null);
			}

			public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
			{
				var properties = new PropertyDescriptorCollection(null);

				foreach (PropertyDescriptor property in base.GetProperties(attributes))
					properties.Add(property);

				foreach (var property in provider.Properties)
					properties.Add(property);

				return properties;
			}
		}
	}
}
