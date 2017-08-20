using System;
using System.Windows.Data;

namespace PCM_SeasonPlaner
{
   [ValueConversion(typeof(Object), typeof(Int32))]
   public class TimestampToDate : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         //  gerechnet wird ab der UNIX Epoche
         System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
         try
         {
            if (value == null)
               return dateTime;
            else
               return dateTime = dateTime.AddSeconds((double)value);
         }
         catch
         {
            return dateTime;
         }
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         try
         {
            TimeSpan t = ((DateTime)value - new DateTime(1970, 1, 1));
            return (int)t.TotalSeconds;
         }
         catch
         {
            return 0;
         }
      }
   }
}
