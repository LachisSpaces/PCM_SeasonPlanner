using System.ComponentModel;
using System.Globalization;
using System;

public class DateTimeTypeConverter : TypeConverter
{
    // Methods
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return (sourceType == typeof(string));
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return (destinationType == typeof(string));
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
        DateTimeFormatInfo format = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
        return DateTime.ParseExact(value.ToString(), format.ShortDatePattern, culture);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == null)
        {
            throw new ArgumentNullException("destinationType");
        }
        if (culture == null)
        {
            throw new ArgumentNullException("culture");
        }
        DateTime? nullable = value as DateTime?;
        if (!nullable.HasValue || (destinationType != typeof(string)))
        {
            throw new NotSupportedException();
        }
        DateTimeFormatInfo format = (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
        return nullable.Value.ToString(format.ShortDatePattern, culture);
    }
}

