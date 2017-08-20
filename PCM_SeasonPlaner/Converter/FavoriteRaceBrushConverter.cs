using System.Windows.Media;
using System.Windows.Data;
using System;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Converts an integer value to a brush - negative values return red, non-negative values return null.
   /// </summary>
   public class CvpFavoriteRaceBrushConverter : IValueConverter
   {
      public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         if (value is Int16)
            if ((Int16)value == 1)
               return DBLoader.cIsFavoriteRace;
         return DBLoader.cIsNotFavoriteRace;
      }

      public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
}
