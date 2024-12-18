using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace OpenLoco.Gui.Models.Converters
{
	public class EnumToBooleanConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Enum enumValue && parameter is Enum enumParameter)
			{
				return enumValue.Equals(enumParameter);
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool boolValue && boolValue && parameter is Enum enumParameter)
			{
				return enumParameter;
			}
			return null;
		}
	}
	public class EnumFlagToBooleanConverter : IValueConverter
	{
		public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Enum enumValue && parameter is Enum enumParameter)
			{
				_ = enumValue.HasFlag(enumParameter);
			}
			return false;
		}

		public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool isChecked && parameter is Enum flag)
			{
				if (isChecked)
				{
					return System.Convert.ChangeType(flag, targetType);
				}
				else
				{
					//Need to get the value of the current enum to remove the flag
					if (System.Convert.ChangeType(value, targetType) is Enum currentEnumValue)
					{
						return Enum.ToObject(targetType, System.Convert.ToInt32(currentEnumValue) & ~System.Convert.ToInt32(flag));
					}
					else
					{
						return Enum.ToObject(targetType, 0);
					}
				}
			}

			return BindingOperations.DoNothing; // Or throw an exception
		}
	}
}
