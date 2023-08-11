using System.ComponentModel;

namespace WebApp.DynamicTypeDescription;

public class DynamicPropertyManager<TTarget> : IDisposable
{
	private readonly DynamicTypeDescriptionProvider _provider;
	private readonly TTarget? _target;

	public DynamicPropertyManager()
	{
		var type = typeof(TTarget);

		_provider = new DynamicTypeDescriptionProvider(type);
		TypeDescriptor.AddProvider(_provider, type);
	}

	public DynamicPropertyManager(TTarget target)
	{
		if (target == null)
			throw new ArgumentNullException(nameof(target));

		_target = target;

		_provider = new DynamicTypeDescriptionProvider(typeof(TTarget));
		TypeDescriptor.AddProvider(_provider, target);
	}

	public IList<PropertyDescriptor> Properties => _provider.Properties;

	public void Dispose()
	{
		if (_target is null)
		{
			TypeDescriptor.RemoveProvider(_provider, typeof(TTarget));
		}
		else
		{
			TypeDescriptor.RemoveProvider(_provider, _target);
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