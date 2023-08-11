using System.ComponentModel;

namespace WebApp.DynamicTypeDescription
{
	public class DynamicPropertyManager<TTarget> : IDisposable
	{
		private readonly DynamicTypeDescriptionProvider provider;
		private readonly TTarget? target;

		public DynamicPropertyManager()
		{
			var type = typeof(TTarget);

			provider = new DynamicTypeDescriptionProvider(type);
			TypeDescriptor.AddProvider(provider, type);
		}

		public DynamicPropertyManager(TTarget target)
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			this.target = target;

			provider = new DynamicTypeDescriptionProvider(typeof(TTarget));
			TypeDescriptor.AddProvider(provider, target);
		}

		public IList<PropertyDescriptor> Properties
		{
			get { return provider.Properties; }
		}

		public void Dispose()
		{
			if (ReferenceEquals(target, null))
			{
				TypeDescriptor.RemoveProvider(provider, typeof(TTarget));
			}
			else
			{
				TypeDescriptor.RemoveProvider(provider, target);
			}
		}

		public static DynamicPropertyDescriptor<TTargetType, TPropertyType>
		   CreateProperty<TTargetType, TPropertyType>(string displayName, Func<TTargetType, TPropertyType?> getter, Action<TTargetType, TPropertyType?> setter, Attribute[] attributes)
		{
			return new DynamicPropertyDescriptor<TTargetType, TPropertyType>(displayName, getter, setter, attributes);
		}

		public static DynamicPropertyDescriptor<TTargetType, TPropertyType>
			CreateProperty<TTargetType, TPropertyType>(string displayName, Func<TTargetType, TPropertyType?> getHandler, Attribute[]? attributes)
		{
			return new DynamicPropertyDescriptor<TTargetType, TPropertyType>(displayName, getHandler, (_, _) => { }, attributes);
		}
	}
}
