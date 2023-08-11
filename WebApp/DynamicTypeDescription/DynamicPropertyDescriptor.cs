using System.ComponentModel;

namespace WebApp.DynamicTypeDescription;

public class DynamicPropertyDescriptor<TTarget, TProperty> : PropertyDescriptor
{
	private readonly Action<TTarget, TProperty?> _setter;
	private readonly Func<TTarget, TProperty?> _getter;
	private readonly string _propertyName;

	public DynamicPropertyDescriptor(string propertyName, Func<TTarget, TProperty?> getter, Action<TTarget, TProperty?> setter, Attribute[]? attributes)
		: base(propertyName, attributes ?? new Attribute[] { })
	{
		_setter = setter;
		_getter = getter;
		_propertyName = propertyName;
	}

	public override bool Equals(object? obj)
	{
		return obj is DynamicPropertyDescriptor<TTarget, TProperty> o && o._propertyName.Equals(_propertyName);
	}

	public override int GetHashCode()
	{
		return _propertyName.GetHashCode();
	}

	public override bool CanResetValue(object component)
	{
		return true;
	}

	public override Type ComponentType => typeof(TTarget);

	public override object? GetValue(object? component)
	{
		if (component == null)
			throw new InvalidOperationException("Component object is null.");

		return _getter((TTarget)component);
	}

	public override bool IsReadOnly => _setter == null;

	public override Type PropertyType => typeof(TProperty);

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object? component, object? value)
	{
		if (component == null)
			throw new InvalidOperationException("Component object is null.");

		_setter((TTarget)component, (TProperty?)value);
	}

	public override bool ShouldSerializeValue(object component)
	{
		return true;
	}
}