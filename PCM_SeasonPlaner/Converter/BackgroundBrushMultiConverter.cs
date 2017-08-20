using System.Windows.Media;
using System.Windows.Data;
using System;

namespace PCM_SeasonPlaner
{
   public class CvpBackgroundBrushMultiConverter : IMultiValueConverter
   {
      public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         object value = values[0];
         object selected = values[1];
         if (selected is Boolean)
         {
            if ((bool)selected)
               return Brushes.Aquamarine;
         }
         int i = 0;
         if (value is Int16)
            i = (Int16)value;
         else if (value is Decimal)
            i = (int)(Decimal)value;
         if (i > 0)
         {
            if (i > 77)
               return DBLoader.cManagement_Carac_Huge;
            if (i > 74)
               return DBLoader.cManagement_Carac_VeryGood;
            if (i > 69)
               return DBLoader.cManagement_Carac_Good;
            if (i > 62)
               return DBLoader.cManagement_Carac_Bad;
         }
         return DBLoader.cManagement_Carac_VeryBad;
      }

      public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
      {
         throw new NotImplementedException();
      }
   }
}
