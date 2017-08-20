using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System;

namespace PCM_SeasonPlaner
{

   public class TimelinePanel : Panel
   {
      public enum GridSizes { Nothing, Week, Month };
      public enum LayoutTypes { Nothing, FitnessBars, FitnessSchedule, CyclistSchedule, TeamSchedule, RaceWithHeader, RaceNoHeader };

      private const int _intHeight = 15;


      private TimeSpan _tsContainerSize;
      private Boolean _blnShowHeaders = false;
      private GridSizes _GridSize = GridSizes.Nothing;
      private LayoutTypes _LayoutType = LayoutTypes.Nothing;
      private DoubleAnimation _animInactive = new DoubleAnimation();
      private DoubleAnimation _animActive = new DoubleAnimation(1, .5, new Duration(TimeSpan.FromSeconds(1)));

      public TimelinePanel() : base()
      {
         _animActive.RepeatBehavior = RepeatBehavior.Forever;
         _animActive.AutoReverse = true;
      }


      #region Properties

      #region StartDate

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static readonly DependencyProperty StartDateProperty =
          DependencyProperty.Register("StartDate", typeof(DateTime), typeof(TimelinePanel), 
          new FrameworkPropertyMetadata(new DateTime(DBLoader.Year, 01, 01),
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public DateTime StartDate
      {
         get { return (DateTime)GetValue(StartDateProperty); }
         set { SetValue(StartDateProperty, value); }
      }

      #endregion


      #region EndDate

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static readonly DependencyProperty EndDateProperty =
          DependencyProperty.Register("EndDate", typeof(DateTime), typeof(TimelinePanel),
          new FrameworkPropertyMetadata(new DateTime(DBLoader.Year, 12, 31),
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public DateTime EndDate
      {
         get { return (DateTime)GetValue(EndDateProperty); }
         set { SetValue(EndDateProperty, value); }
      }

      #endregion


      #region LayoutType

      public static readonly DependencyProperty LayoutTypeProperty =
          DependencyProperty.Register("LayoutType", typeof(LayoutTypes), typeof(TimelinePanel), new FrameworkPropertyMetadata(LayoutTypes.Nothing, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutTypePropertyChanged));

      public LayoutTypes LayoutType
      {
         get { return (LayoutTypes)GetValue(LayoutTypeProperty); }
         set { SetValue(LayoutTypeProperty, value); }
      }

      private static void OnLayoutTypePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
      {
         switch ((LayoutTypes)e.NewValue)
          {
             case LayoutTypes.TeamSchedule:
             case LayoutTypes.FitnessSchedule:
             case LayoutTypes.RaceWithHeader:
                ((TimelinePanel)source)._blnShowHeaders = true;
                break;
             default:
                ((TimelinePanel)source)._blnShowHeaders = false;
                break;
          }
      }

      #endregion


      #region GridSize

      public static readonly DependencyProperty GridSizeProperty =
          DependencyProperty.Register("GridSize", typeof(GridSizes), typeof(TimelinePanel), new FrameworkPropertyMetadata(GridSizes.Week, FrameworkPropertyMetadataOptions.AffectsMeasure));

      public GridSizes GridSize
      {
         get { return (GridSizes)GetValue(GridSizeProperty); }
         set { SetValue(GridSizeProperty, value); }
      }

      #endregion


      #region ShowActiveEventsOnly

      public static readonly DependencyProperty ShowActiveEventsOnlyProperty =
          DependencyProperty.Register("ShowActiveEventsOnly", typeof(Boolean), typeof(TimelinePanel),
          new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      public Boolean ShowActiveEventsOnly
      {
         get { return (Boolean)GetValue(ShowActiveEventsOnlyProperty); }
         set { SetValue(ShowActiveEventsOnlyProperty, value); }
      }

      #endregion


      #region ShowAllowedEventsOnly

      public static readonly DependencyProperty ShowAllowedEventsOnlyProperty =
          DependencyProperty.Register("ShowAllowedEventsOnly", typeof(Boolean), typeof(TimelinePanel),
          new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      public Boolean ShowAllowedEventsOnly
      {
         get { return (Boolean)GetValue(ShowAllowedEventsOnlyProperty); }
         set { SetValue(ShowAllowedEventsOnlyProperty, value); }
      }

      #endregion


      #region CountryFilter

      public static readonly DependencyProperty CountryFilterProperty =
          DependencyProperty.Register("CountryFilter", typeof(String[]), typeof(TimelinePanel),
          new FrameworkPropertyMetadata(new String[0],
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      public String[] CountryFilter
      {
         get { return (String[])GetValue(CountryFilterProperty); }
         set { SetValue(CountryFilterProperty, value); }
      }

      #endregion


      #region ClassFilter

      public static readonly DependencyProperty ClassFilterProperty =
          DependencyProperty.Register("ClassFilter", typeof(String[]), typeof(TimelinePanel),
          new FrameworkPropertyMetadata(new String[0],
            FrameworkPropertyMetadataOptions.AffectsMeasure));

      public String[] ClassFilter
      {
         get { return (String[])GetValue(ClassFilterProperty); }
         set { SetValue(ClassFilterProperty, value); }
      }

      #endregion

      #endregion


      #region Attached Properties

      #region EventStartDate

      // EventStartDate Attached Dependency Property
      public static readonly DependencyProperty EventStartDateProperty =
         DependencyProperty.RegisterAttached("EventStartDate", typeof(DateTime?),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventStartDateChanged)));

      protected static void OnEventStartDateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static DateTime? GetEventStartDate(DependencyObject d)
      {
         return (DateTime?)d.GetValue(EventStartDateProperty);
      }

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static void SetEventStartDate(DependencyObject d, DateTime? value)
      {
         d.SetValue(EventStartDateProperty, value);
      }

      #endregion


      #region EventEndDate

      // EventEndDate Attached Dependency Property
      public static readonly DependencyProperty EventEndDateProperty =
         DependencyProperty.RegisterAttached("EventEndDate", typeof(DateTime?),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventEndDateChanged)));

      protected static void OnEventEndDateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static DateTime? GetEventEndDate(DependencyObject d)
      {
         return (DateTime?)d.GetValue(EventEndDateProperty);
      }

      [TypeConverter(typeof(DateTimeTypeConverter))]
      public static void SetEventEndDate(DependencyObject d, DateTime? value)
      {
         d.SetValue(EventEndDateProperty, value);
      }

      #endregion


      #region EventIsActive

      // EventIsActive Attached Dependency Property
      public static readonly DependencyProperty EventIsActiveProperty =
         DependencyProperty.RegisterAttached("EventIsActive", typeof(Boolean),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventIsActiveChanged)));

      protected static void OnEventIsActiveChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      public static Boolean GetEventIsActive(DependencyObject d)
      {
         return (Boolean)d.GetValue(EventIsActiveProperty);
      }

      public static void SetEventIsActive(DependencyObject d, Boolean value)
      {
         d.SetValue(EventIsActiveProperty, value);
      }

      #endregion


      #region EventAllowed

      // EventAllowed Attached Dependency Property
      public static readonly DependencyProperty EventAllowedProperty =
         DependencyProperty.RegisterAttached("EventAllowed", typeof(String),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventAllowedChanged)));

      protected static void OnEventAllowedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      public static String GetEventAllowed(DependencyObject d)
      {
         return (String)d.GetValue(EventAllowedProperty);
      }

      public static void SetEventAllowed(DependencyObject d, String value)
      {
         d.SetValue(EventAllowedProperty, value);
      }

      #endregion


      #region EventCountry

      // EventCountry Attached Dependency Property
      public static readonly DependencyProperty EventCountryProperty =
         DependencyProperty.RegisterAttached("EventCountry", typeof(String),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventCountryChanged)));

      protected static void OnEventCountryChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      public static String GetEventCountry(DependencyObject d)
      {
         return (String)d.GetValue(EventCountryProperty);
      }

      public static void SetEventCountry(DependencyObject d, String value)
      {
         d.SetValue(EventCountryProperty, value);
      }

      #endregion


      #region EventClass

      // EventClass Attached Dependency Property
      public static readonly DependencyProperty EventClassProperty =
         DependencyProperty.RegisterAttached("EventClass", typeof(String),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnEventClassChanged)));

      protected static void OnEventClassChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      public static String GetEventClass(DependencyObject d)
      {
         return (String)d.GetValue(EventClassProperty);
      }

      public static void SetEventClass(DependencyObject d, String value)
      {
         d.SetValue(EventClassProperty, value);
      }

      #endregion

      
      #region IsLabel

      // IsLabel Attached Dependency Property
      public static readonly DependencyProperty IsLabelProperty =
         DependencyProperty.RegisterAttached("IsLabel", typeof(Boolean),
         typeof(TimelinePanel),
             new PropertyMetadata(new PropertyChangedCallback(OnIsLabelChanged)));

      protected static void OnIsLabelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
      {
      }

      public static Boolean GetIsLabel(DependencyObject d)
      {
         return (Boolean)d.GetValue(IsLabelProperty);
      }

      public static void SetIsLabel(DependencyObject d, Boolean value)
      {
         d.SetValue(IsLabelProperty, value);
      }

      #endregion

      #endregion


      #region Overridden functions

      protected override Size MeasureOverride(Size availableSize)
      {
         List<UIElement> uieBorders = new List<UIElement>();
         bool blnDrawBorders = (_GridSize != this.GridSize) || (_LayoutType != this.LayoutType);
         _LayoutType = this.LayoutType;
         _GridSize = this.GridSize;
         Type tBorder = typeof(Border);
         foreach (UIElement child in Children)
         {
            if (child.GetType() != tBorder)
               child.Measure(availableSize);
            else if (blnDrawBorders)
               uieBorders.Add(child);
         }
         if (blnDrawBorders)
         {
            foreach (UIElement e in uieBorders)
               Children.Remove(e);
            DateTime dtmSeparator = new DateTime(DBLoader.Year, 1, 1);
            DateTime dtmEndDate = new DateTime(DBLoader.Year, 11, 1);
            while (dtmSeparator < dtmEndDate)
            {
               Border b = new Border();
               TextBlock l = new TextBlock();
               switch (this.GridSize)
               {
                  case GridSizes.Week:
                     b.SetValue(TimelinePanel.EventStartDateProperty, dtmSeparator);
                     if (_blnShowHeaders)
                        l.Text = (dtmSeparator).ToString("dd.MM.");
                     dtmSeparator = dtmSeparator.AddDays(7);
                     break;
                  case GridSizes.Month:
                     b.SetValue(TimelinePanel.EventStartDateProperty, dtmSeparator);
                     if (_blnShowHeaders)
                        l.Text = (dtmSeparator).ToString("MMMM");
                     dtmSeparator = dtmSeparator.AddMonths(1);
                     break;
               }
               b.SetValue(Panel.ZIndexProperty, -1);
               b.BorderThickness = new Thickness(1, 0, 0, 0);
               b.BorderBrush = Brushes.DarkGray;
               if (_blnShowHeaders)
                  b.Child = l;
               Children.Add(b);
            }
         }
         return new Size(double.IsPositiveInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height);
      }

      protected override Size ArrangeOverride(Size finalSize)
      {
         this.Precalculate();
         Type tBorder = typeof(Border);
         bool blnFirstElement = true;
         int intTop = _blnShowHeaders ? 16 : 0;
         double[] dblUsedWidth = new double[15];
         double dblMinimumHeight = 15; // Mindesthöhe, sonst sieht es doof aus
         bool blnShowActiveEventsOnly = (Boolean)GetValue(ShowActiveEventsOnlyProperty);
         bool blnShowAllowedEventsOnly = (Boolean)GetValue(ShowAllowedEventsOnlyProperty);
         string[] strCountryFilter = (string[])GetValue(CountryFilterProperty);
         string[] strClassFilter = (string[])GetValue(ClassFilterProperty);
         foreach (UIElement child in this.Children)
         {
            if (child.GetType() == tBorder)
            {
               if (!double.IsNaN(this.Height))
               {
                  DateTime dtmDate = (DateTime)child.GetValue(EventStartDateProperty);
                  double dblScaledLeft = ScaleDate(dtmDate);
                  double dblLeft = finalSize.Width * dblScaledLeft;
                  child.Arrange(new Rect(dblLeft, 0, 70, this.Height)); 
               }
            }
            else 
            {
               bool blnShowEvent = true;
               string strAllowed = (String)child.GetValue(EventAllowedProperty);
               if (blnShowAllowedEventsOnly && (strAllowed != "YES"))
                  blnShowEvent = false;
               bool blnEventIsActive = (Boolean)child.GetValue(EventIsActiveProperty);
               if (blnShowActiveEventsOnly && !blnEventIsActive)
                  blnShowEvent = false;
               if (blnShowEvent && strCountryFilter.Length > 0)
               {
                  blnShowEvent = false;
                  string strCountry = (string)child.GetValue(EventCountryProperty);
                  if (strCountryFilter.Contains(strCountry))
                     blnShowEvent = true;
               }
               if (blnShowEvent && strClassFilter.Length > 0)
               {
                  blnShowEvent = false;
                  string strClass = (string)child.GetValue(EventClassProperty);
                  if (strClassFilter.Contains(strClass))
                     blnShowEvent = true;
               }
               if (!blnShowEvent)
               {
                  child.Visibility = Visibility.Collapsed;
               }
               else
               {
                  DateTime dtmDate = (DateTime)child.GetValue(EventStartDateProperty);
                  double dblScaledLeft = ScaleDate(dtmDate);
                  if ((Boolean)child.GetValue(IsLabelProperty))
                  {
                     child.Arrange(new Rect(finalSize.Width * dblScaledLeft, _intHeight + 14, child.DesiredSize.Width, child.DesiredSize.Height));
                     child.Visibility = Visibility.Visible;
                  }
                  else
                  {
                     dtmDate = (DateTime)child.GetValue(EventEndDateProperty);
                     double dblScaledRight = ScaleDate(dtmDate.AddDays(1));
                     if (dblScaledLeft < 1 && dblScaledRight > 0)
                     {
                        int intPosition = 0;
                        double width = child.DesiredSize.Width;
                        double height = child.DesiredSize.Height;
                        double dblLeft = finalSize.Width * dblScaledLeft;
                        double dblRight = finalSize.Width * dblScaledRight;
                        width = dblRight - dblLeft;
                        if (this.LayoutType == LayoutTypes.FitnessBars)
                        {
                           child.Arrange(new Rect(dblLeft, finalSize.Height - height, width, height));
                        }
                        else
                        {
                           if (blnFirstElement)
                           {
                              for (int i = 1; i < 15; ++i)
                                 dblUsedWidth[i] = dblLeft;
                              dblUsedWidth[0] = dblRight;
                              blnFirstElement = false;
                           }
                           else
                           {
                              while (intPosition < 15 && dblLeft < dblUsedWidth[intPosition])
                                 ++intPosition;
                              dblUsedWidth[intPosition] = dblRight;
                           }
                           child.Arrange(new Rect(dblLeft, (intPosition * _intHeight) + intTop, width, height));
                           if (dblMinimumHeight < (intPosition * _intHeight) + intTop + height)
                              dblMinimumHeight = (intPosition * _intHeight) + intTop + height;
                           child.SetValue(WidthProperty, width);
                           //if (!blnShowActiveEventsOnly && blnEventIsActive)
                           //   child.BeginAnimation(OpacityProperty, _animActive);
                           //else
                           //   child.BeginAnimation(OpacityProperty, _animInactive);
                        }
                        child.Visibility = Visibility.Visible;
                     }
                     else
                        child.Visibility = Visibility.Collapsed;
                  }
               }
            }
         }
         if (double.IsNaN(this.Height))
            foreach (UIElement child in this.Children)
            {
               if (child.GetType() == typeof(Border))
               {
                  DateTime dtmDate = (DateTime)child.GetValue(EventStartDateProperty);
                  double dblScaledLeft = ScaleDate(dtmDate);
                  double dblLeft = finalSize.Width * dblScaledLeft;
                  child.Arrange(new Rect(dblLeft, 0, 70, dblMinimumHeight)); 
               }
            }
         this.MinHeight = dblMinimumHeight;
         return finalSize;
      }

      #endregion


      #region private functions

      private void Precalculate()
      {
         _tsContainerSize = this.EndDate - this.StartDate;
      }

      private double ScaleDate(DateTime date)
      {
         TimeSpan tsLocation = date - this.StartDate;
         return (double)tsLocation.Days / (double)_tsContainerSize.Days;
      }

      #endregion

   }

}


