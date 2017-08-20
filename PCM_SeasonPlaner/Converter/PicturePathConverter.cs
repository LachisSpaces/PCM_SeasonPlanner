using System.Windows.Data;
using System;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Hängt den Applications-Pfad vor den relativen Pfad in value
   /// </summary>
   public class CvtPicturePathConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (value is string)
            return string.Concat(DBLoader.ApplicationFolder, value);
         return "";
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
}
