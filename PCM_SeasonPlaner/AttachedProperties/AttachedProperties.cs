using System.Windows;
using System;

namespace PCM_SeasonPlaner
{
   class HlpProp
   {

      public static readonly DependencyProperty IndexProperty = DependencyProperty.RegisterAttached("Index",
          typeof(Int16), typeof(HlpProp), new FrameworkPropertyMetadata(null));

      public static Int16 GetIndex(UIElement element)
      {
         if (element == null)
            throw new ArgumentNullException("element");
         return (Int16)element.GetValue(IndexProperty);
      }
      public static void SetIndex(UIElement element, Int16 value)
      {
         if (element == null)
            throw new ArgumentNullException("element");
         element.SetValue(IndexProperty, value);
      }


      public static readonly DependencyProperty IdProperty = DependencyProperty.RegisterAttached("Id",
          typeof(String), typeof(HlpProp), new FrameworkPropertyMetadata(null));

      public static String GetId(UIElement element)
      {
         if (element == null)
            throw new ArgumentNullException("element");
         return (String)element.GetValue(IdProperty);
      }
      public static void SetId(UIElement element, String value)
      {
         if (element == null)
            throw new ArgumentNullException("element");
         element.SetValue(IdProperty, value);
      }

   }
}
