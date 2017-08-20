using PCM_SeasonPlaner.Languages;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows;
using System.Linq;
using System.Data;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für AdvancedPlanner.xaml
   /// </summary>
   public partial class AdvancedPlanner : Page
   {
      private const int _intLevelHeight = 15;
      private const int _intFitnessPlanWeeks = 44;

      private Typeface _tfDataGrid;
      private BrushConverter _brushConv = new BrushConverter();
      private SolidColorBrush _scbBackgroundNormal = new SolidColorBrush(Colors.LightGray);
      private SolidColorBrush _scbBackgroundSelected = new SolidColorBrush(Colors.Silver);
      private SolidColorBrush _scbForegroundImportant = new SolidColorBrush(Colors.Yellow);
      private SolidColorBrush _scbForegroundNormal = new SolidColorBrush(Colors.LightGreen);

      private String _strDefaultFilterRace = "HlpClassName NOT IN ('NationalChampionship','NationalChampionshipITT','Olympics','OlympicsTT','Track','U23_1NCup','U23_2NCup')"; 
      private DataView _dvSubsquadConfig;
      private DataView _dvCyclist_Popup;
      private DataView _dvCyclist;
      private DataView _dvRace;

      private Boolean _blnPageLoaded = false;
      private Boolean _blnDataViewsLoaded = false;
      private Boolean _blnRacesInitialized = false;
      private Boolean _blnControlsInitialized = false;

      private Binding _bindRaceNameVisibility = new Binding("Visibility");
      private Binding _bindStageProfileVisibility = new Binding("Visibility");

      private Int16 _intFitnessPlanSizeActual;
      private Int16 _intCalendarSizeActual;
      private Int16 _intDateRangeDefault;

      private TimelinePanel[] _tlpCyclist = new TimelinePanel[Const.MaxCyclist + Const.MaxGroups];
      private TextBlock[,] _tbCyclist = new TextBlock[Const.MaxCyclist, 2];
      private Grid[] _grdCyclist = new Grid[Const.MaxCyclist];

      private String _FitnessPlanCopy = string.Empty;
      private String _FitnessPlanUndo = string.Empty;
      private Int16 _intCyclistIndexPrevious = -1;
      private Boolean _blnFitnessProgramDirty = false;
      private DoubleAnimation _animActive = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(.4)));
      private DoubleAnimation _animInactive = new DoubleAnimation();

      private Point _pntDragStart;


      public AdvancedPlanner()
      {
         _animActive.AutoReverse = true;
         _animActive.RepeatBehavior = RepeatBehavior.Forever;
         // Interface vorbereiten
         InitializeComponent();
         _bindRaceNameVisibility.Source = this.txbVisibilityRaceName;
         _bindStageProfileVisibility.Source = this.txbVisibilityStageProfile;
         this.ckbVisibilityStageProfile.IsChecked = false;
         this.ckbVisibilityStageProfile_Click(new object(), new RoutedEventArgs());
      }


      private void Page_Loaded(object sender, RoutedEventArgs e)
      {
         _blnDataViewsLoaded = false;
         _intDateRangeDefault = Convert.ToInt16(App.Current.Properties["CalendarSize"].ToString());
         this.cboCalendarSize.SelectedValue = _intDateRangeDefault;
         this.cboFitnessPlanSize.SelectedValue = Convert.ToInt16(App.Current.Properties["FitnessPlanSize"].ToString());
         string strPageName = LanguageOptions.Text("Page_AdvancedPlanner");
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + strPageName;
         this.Title = strPageName;
         // Daten vorbereiten
         _dvSubsquadConfig = DBLoader.GetDataView("DYN_subsquad_config", false);
         _dvCyclist = DBLoader.GetDataView("DYN_cyclist", true);
         _dvRace = DBLoader.GetDataView("STA_Race", false);
         _dvRace.RowFilter = _strDefaultFilterRace;
         // Alle Controls erstellen und einstellen
         this.InitializeControls();
         this.SetCalendarSize();
         // Race-Kalender mit Events füllen
         this.InitializeRaces();
         // Fahrer anzeigen gemäss Gruppierung
         this.DisplayGroupsCyclists();
         // Popup vorbereiten
         _dvCyclist_Popup = DBLoader.GetDataView("DYN_cyclist", false);
         _dvCyclist_Popup.RowFilter = "IDcyclist=0";
         this.dgOwnTeam_TranslateColumnHeaders();
         // Source für Country-Filter
         DataView dvHlp = DBLoader.GetDataView("STA_country", true);
         dvHlp.Sort = "CONSTANT";
         this.cboRaceFilterCountry.ItemsSource = dvHlp;
         // Sortierung einstellen für Drag+Drop >> ACHTUNG: Sortierung darf nicht mehr geändert werden
         _dvCyclist.Sort = "SortGroup";
         _dvRace.Sort = "IDrace";
         // Fitnessplan + geplante Rennen löschen
         this.ClearSelection();
         this.ClearRaces();
         if (_blnPageLoaded)
            this.tlpCyclistFitness_Load("");
         _blnDataViewsLoaded = true;
         _blnPageLoaded = true;
      }


      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
         this.dgOwnTeam.ItemsSource = null;
         _dvCyclist_Popup.Dispose();
         _dvCyclist_Popup = null;
         _dvCyclist.Dispose();
         _dvCyclist = null;
         _dvRace.Dispose();
         _dvRace = null;
      }


      public void SaveBeforeClosing()
      {
         this.tlpCyclistFitness_ChangesSaved();
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         //this.dgRace.CommitEdit();
         //App.Current.Properties["RaceCalendarExpanded"] = this.expRaceCalendar.IsExpanded;
         //App.Current.Properties["ShowActiveEventsOnlyPlan"] = this.ckbShowActiveEventsOnlyPlan.IsChecked;
         //App.Current.Properties["ShowActiveEventsOnlyCalendar"] = this.ckbShowActiveEventsOnlyCalendar.IsChecked;
      }



      #region Initialisierungen

      private void InitializeControls()
      {
         if (_blnControlsInitialized) return;
         _blnControlsInitialized = true;
         this.tlpTeamRace.ShowActiveEventsOnly = true;
         this.tlpTeamRace.LayoutType = TimelinePanel.LayoutTypes.TeamSchedule;
         this.tlpRaceListPT.LayoutType = TimelinePanel.LayoutTypes.RaceWithHeader;
         this.tlpRaceListC0.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
         this.tlpRaceListC1.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
         this.tlpRaceListC2.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
         this.tlpRaceListXX.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
         this.tlpCyclistFitness.LayoutType = TimelinePanel.LayoutTypes.FitnessBars;
         this.tlpCyclistFitness.GridSize = TimelinePanel.GridSizes.Month;
         this.tlpCyclistRace.LayoutType = TimelinePanel.LayoutTypes.FitnessSchedule;
         this.tlpCyclistRace.GridSize = TimelinePanel.GridSizes.Month;
         DateTime dtmSeparator = new DateTime(DBLoader.Year, 1, 1).AddDays(1);
         DateTime dtmEndDate = new DateTime(DBLoader.Year, 11, 1);//.AddMonths(10);
         TimeSpan tsHlp = dtmEndDate.Subtract(new DateTime(DBLoader.Year, 1, 1));
         TimeSpan tsInterval = TimeSpan.FromSeconds((tsHlp.TotalSeconds / _intFitnessPlanWeeks));
         Int16 intWeekIndex = 0;
         while (dtmSeparator < dtmEndDate)
         {
            Rectangle r = new Rectangle();
            r.Height = _intLevelHeight;
            r.Fill = DBLoader.ColorTrainingLevel(1);
            r.AddHandler(TimelinePanel.MouseUpEvent, new MouseButtonEventHandler(tlpCyclistFitness_MouseUp));
            r.SetValue(TimelinePanel.EventEndDateProperty, dtmSeparator.AddDays(4));
            r.SetValue(TimelinePanel.EventStartDateProperty, dtmSeparator);
            r.SetValue(HlpProp.IndexProperty, intWeekIndex++);
            this.tlpCyclistFitness.Children.Add(r);
            dtmSeparator = dtmSeparator.Add(tsInterval);
         }
         // Label + Kalender-Bereich für die Fahrer erstellen und merken
         _dvCyclist.Sort = "HelpIndex";
         for (Int16 i = 0; i < _dvCyclist.Count; ++i)
         {
            TimelinePanel tlp = new TimelinePanel();
            //tlp.AddHandler(TimelinePanel.MouseDownEvent, new MouseButtonEventHandler(CyclistTimeLine_MouseDown), true); //Code noch nicht perfekt
            tlp.AddHandler(TimelinePanel.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(tlpRaceList_PreviewMouseLeftButtonDown));
            tlp.AddHandler(TimelinePanel.PreviewMouseMoveEvent, new MouseEventHandler(tlpRaceList_PreviewMouseMove));
            tlp.AddHandler(TimelinePanel.DragEnterEvent, new DragEventHandler(tlpCyclistRace_DragEnter));
            tlp.AddHandler(TimelinePanel.DragOverEvent, new DragEventHandler(tlpCyclistRace_DragOver));
            tlp.AddHandler(TimelinePanel.DropEvent, new DragEventHandler(tlpCyclistRace_Drop));
            tlp.SetValue(HlpProp.IdProperty, _dvCyclist[i]["IDcyclist"].ToString());
            tlp.HorizontalAlignment = HorizontalAlignment.Stretch;
            //tlp.Name = string.Format("tlpCyclistRace{0:00}", i);
            tlp.SetValue(HlpProp.IndexProperty, i);
            tlp.ShowActiveEventsOnly = true;
            tlp.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
            tlp.Background = _scbBackgroundNormal;
            tlp.AllowDrop = true;
            tlp.Height = 15;
            _tlpCyclist[i] = tlp;
            Grid grd = new Grid();
            grd.Width = 99.0;
            grd.Height = 15.0;
            grd.Background = _scbBackgroundNormal;
            grd.FlowDirection = FlowDirection.LeftToRight;
            grd.SetValue(HlpProp.IndexProperty, i);
            grd.SetValue(HlpProp.IdProperty, _dvCyclist[i]["IDcyclist"].ToString());
            grd.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(CyclistHeader_MouseDown), true);
            TextBlock tb = new TextBlock();
            tb.VerticalAlignment = VerticalAlignment.Bottom;
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            tb.Text = ResizeString(" " + _dvCyclist[i]["gene_sz_firstname"].ToString().Substring(0, 1) + "." + _dvCyclist[i]["gene_sz_lastname"]);
            _tbCyclist[i, 0] = tb; 
            grd.Children.Add(tb);
            tb = new TextBlock();
            tb.VerticalAlignment = VerticalAlignment.Bottom;
            tb.HorizontalAlignment = HorizontalAlignment.Right;
            tb.Text = string.Format("{0:00}", _dvCyclist[i]["RaceDaysPlanned"]);
            _tbCyclist[i, 1] = tb; 
            grd.Children.Add(tb);
            _grdCyclist[i] = grd; 
         }
         // Kalender-Bereich für die Gruppen suchen, einstellen und merken
         for (int i = 0; i < Const.MaxGroups; ++i)
         {
            TimelinePanel tlp = (TimelinePanel)this.FindName("tlpGroupRace" + i);
            tlp.ShowActiveEventsOnly = true;
            tlp.LayoutType = TimelinePanel.LayoutTypes.RaceNoHeader;
            _tlpCyclist[Const.MaxCyclist + i] = tlp;
         }
      }

      private void InitializeRaces()
      {
         if (_blnRacesInitialized) return;
         _blnRacesInitialized = true;
         _dvRace.Sort = "StartDate";
         foreach (DataRowView drvR in _dvRace)
         {
            if ((DateTime)drvR["StartDate"] > new DateTime(DBLoader.Year, 10, 31))
               break;
            DateTime dtEnd = (DateTime)drvR["EndDate"];
            string strIDrace = drvR["IDrace"].ToString();
            DateTime dtStart = (DateTime)drvR["StartDate"];
            string strClass = drvR["HlpClassName"].ToString();
            string strCountry = drvR["fkIDcountry"].ToString();
            string strTypeTour = drvR["HlpTypeTour"].ToString();
            string strBackground = drvR["HlpClassColor"].ToString();
            string strRaceName = drvR["gene_sz_race_name"].ToString();
            string strSponsorObjective = drvR["SponsorObjectif"].ToString();
            string strRaceNameShort = drvR["gene_sz_abbreviation"].ToString();
            string strParticipationAllowed = drvR["Participation_Allowed"].ToString();
            bool blnActive = !string.IsNullOrEmpty(strSponsorObjective);
            switch (strClass)
            {
               case "WorldChampionship":
               case "WorldChampionshipITT":
               case "FranceGrandTour":
               case "AutreGrandTour":
               case "ClassiqueMajeure":
               case "AutresTours":
               case "AutresClassiques":
                  this.tlpRaceListPT.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, strParticipationAllowed, strBackground, "Race"));
                  break;
               case "Cont2HC":
               case "Cont1HC":
                  this.tlpRaceListC0.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, strParticipationAllowed, strBackground, "Race"));
                  break;
               case "Cont21":
               case "Cont11":
                  this.tlpRaceListC1.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, strParticipationAllowed, strBackground, "Race"));
                  break;
               case "Cont22":
               case "Cont12":
                  this.tlpRaceListC2.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, strParticipationAllowed, strBackground, "Race"));
                  break;
               case "Cya_Cup":
               case "Cont22U":
               case "Cont12U":
                  this.tlpRaceListXX.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, strParticipationAllowed, strBackground, "Race"));
                  break;
            }
            blnActive = (bool)drvR["Participation_Team"];
            this.tlpTeamRace.Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, string.Empty, strBackground, "TeamRace"));
            for (int i = 0; i < Const.MaxGroups; ++i)
            {
               blnActive = (bool)drvR[string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + i)];
               _tlpCyclist[Const.MaxCyclist + i].Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, string.Empty, strBackground, "Group"));
            }
            for (int i = 0; i < _dvCyclist.Count; ++i)
            {
               blnActive = (bool)drvR[string.Format("Participation_Cyclist{0:00}", i)];
               _tlpCyclist[i].Children.Add(RaceObject(strIDrace, strRaceName, strRaceNameShort, strCountry, strClass, strSponsorObjective, strTypeTour, dtStart, dtEnd, blnActive, string.Empty, strBackground, "Cyclist"));
            }
         }
      }

      private void DisplayGroupsCyclists()
      {
         Grid grdGroup = null;
         for (int i = 0; i < Const.MaxGroups; ++i)
         {
            ((Expander)this.FindName("expGroupRace" + i)).Visibility = Visibility.Collapsed;
            grdGroup = (Grid)this.FindName("grdGroupRace" + i);
            grdGroup.RowDefinitions.Clear();
            grdGroup.Children.Clear();
         }

         Int16 intRowIndex = 0;
         int intGroupIndex = 0;
         int intGroupIdxno = (int)Math.Pow(2, Const.MaxGroups);
         _dvCyclist.Sort = "SortGroup, SortIndex, gene_sz_lastname";
         for (int i = 0; i < _dvCyclist.Count; ++i)
         {
            int intSortGroup = (int)Math.Pow(2, Const.MaxGroups);
            try { intSortGroup = (Int16)_dvCyclist[i]["SortGroup"]; }
            catch { }
            if (intGroupIdxno != intSortGroup)
            {
               intGroupIdxno = intSortGroup;
               intGroupIndex = (Int16)(Math.Log(intGroupIdxno) / Math.Log(2));
               grdGroup = (Grid)this.FindName("grdGroupRace" + intGroupIndex);
               if (intGroupIndex == Const.MaxGroups)
                  ((Label)this.FindName("lblGroupRace" + intGroupIndex)).Content = " " + LanguageOptions.Text("Page_AdvancedPlanner/CyclistGrouping/NoGroup");
               else
               {
                  _dvSubsquadConfig.RowFilter = string.Format("SortGroup={0}", intGroupIdxno);
                  try { ((Label)this.FindName("lblGroupRace" + intGroupIndex)).Content = " " + _dvSubsquadConfig[0]["gene_sz_name"].ToString(); }
                  catch { ((Label)this.FindName("lblGroupRace" + intGroupIndex)).Content = " " + LanguageOptions.Text("Page_AdvancedPlanner/CyclistGrouping/Groups") + (intGroupIndex + 1); }
               }
               ((Expander)this.FindName("expGroupRace" + intGroupIndex)).Visibility = Visibility.Visible;
               intRowIndex = -1;
            }
            RowDefinition rd = new RowDefinition();
            rd.Height = GridLength.Auto;
            grdGroup.RowDefinitions.Add(rd);
            Grid grd = _grdCyclist[(Int16)_dvCyclist[i]["HelpIndex"]];
            Grid.SetRow(grd, ++intRowIndex);
            grdGroup.Children.Add(grd);
            TimelinePanel tlp = _tlpCyclist[(Int16)_dvCyclist[i]["HelpIndex"]];
            Grid.SetRow(tlp, intRowIndex);
            Grid.SetColumn(tlp, 1);
            grdGroup.Children.Add(tlp);
         }
      }

      private Grid RaceObject(string strIDrace, string strRaceName, string strRaceNameShort, string strCountry, string strClass, string strSponsorObjective, string strTypeTour, DateTime dtStart, DateTime dtEnd, bool blnActive, string strAllowed, string strBackground, string strType)
      {
         Grid grd = new Grid();
         grd.Height = 14;
         grd.Margin = new Thickness(0, 0, 1, 0);
         grd.SetValue(HlpProp.IdProperty, strIDrace);
         grd.SetValue(TimelinePanel.EventEndDateProperty, dtEnd);
         grd.SetValue(TimelinePanel.EventClassProperty, strClass);
         grd.SetValue(TimelinePanel.EventStartDateProperty, dtStart);
         grd.SetValue(TimelinePanel.EventIsActiveProperty, blnActive);
         grd.SetValue(TimelinePanel.EventAllowedProperty, strAllowed);
         grd.SetValue(TimelinePanel.EventCountryProperty, strCountry);
         if (strType != "Group")
            grd.Background = (SolidColorBrush)_brushConv.ConvertFromString(strBackground);
         if (!string.IsNullOrEmpty(strSponsorObjective))
            grd.ToolTip = strRaceName + Environment.NewLine + strSponsorObjective;
         else
            grd.ToolTip = strRaceName;
         if (!string.IsNullOrEmpty(strRaceNameShort))
         {
            TextBlock tb = new TextBlock();
            tb.SetValue(Grid.ColumnSpanProperty, 99);
            tb.SetBinding(TextBlock.VisibilityProperty, _bindRaceNameVisibility);
            tb.VerticalAlignment = VerticalAlignment.Center;
            if (!string.IsNullOrEmpty(strSponsorObjective))
               tb.Foreground = _scbForegroundImportant;
            else
               tb.Foreground = _scbForegroundNormal;
            tb.Text = strRaceNameShort;
            grd.Children.Add(tb);
         }
         int intStageCount = dtEnd.Subtract(dtStart).Days + 1;
         for (int i = 0; i < intStageCount; i++)
         {
            Image img = new Image();
            Boolean blnShowImage = true;
            grd.ColumnDefinitions.Add(new ColumnDefinition());
            try { 
               if (i==0)
                  img.Source = BitmapFrame.Create(new Uri(DBLoader.PathStageImage(strTypeTour))); 
               else
                  img.Source = BitmapFrame.Create(new Uri(DBLoader.PathStageImage("empty"))); 
            }
            catch { blnShowImage = false; }
            if (blnShowImage)
            {
               img.SetBinding(TextBlock.VisibilityProperty, _bindStageProfileVisibility);
               img.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
               img.StretchDirection = StretchDirection.Both;
               img.Stretch = Stretch.Fill;
               Grid.SetColumn(img, i);
               grd.Children.Add(img);
            }
         }
         switch (strType)
         {
            case "Fitness":
               break;
            case "Cyclist":
               grd.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(CyclistRace_MouseDown), true);
               break;
            case "Group":
               grd.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(GroupRace_MouseDown), true);
               break;
            case "TeamRace":
               grd.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(TeamRace_MouseDown), true);
               break;
            case "Race":
               grd.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(Race_MouseDown), true);
               break;
         }
         return grd;
      }

      private string ResizeString(string strText)
      {
         if (_tfDataGrid == null)
            _tfDataGrid = new Typeface(this.expRaceList.FontFamily, this.expRaceList.FontStyle, this.expRaceList.FontWeight, this.expRaceList.FontStretch);

         double dblWidth = 0;
         int intLength = strText.Length + 1;
         do
         {
            FormattedText ft = new FormattedText(strText.Substring(0, --intLength), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _tfDataGrid, this.expRaceList.FontSize, Brushes.Black);
            dblWidth = ft.Width;
         } while (dblWidth > 77);
         if (intLength == strText.Length)
            return strText;
         return strText.Substring(0, intLength - 1) + "..";
      }

      #endregion 


      #region Calendar Size

      private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
      {
         this.sbRaces.Value -= Math.Sign(e.Delta);
         this.SetCalendarSize();
         e.Handled = true;
      }

      private void sbRaces_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
      {
         this.SetCalendarSize();
         e.Handled = true;
      }

      private void cboCalendarSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_blnDataViewsLoaded)
            this.SetCalendarSize();
         e.Handled = true;
      }

      private void cboFitnessPlanSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (_blnDataViewsLoaded)
            this.SetFitnessPlanSize();
         e.Handled = true;
      }

      private void SetCalendarSize()
      {
         if (this.cboCalendarSize.SelectedIndex > -1)
         {
            Int16 intSize = Convert.ToInt16(this.cboCalendarSize.SelectedValue.ToString());
            if (_intCalendarSizeActual != intSize)
            {
               this.sbRaces.Value = 0; // Reset
               this.sbRaces.Maximum = 10 - intSize;
               App.Current.Properties["CalendarSize"] = intSize.ToString();
               _intCalendarSizeActual = intSize;
            }
         }
         else
            _intCalendarSizeActual = _intDateRangeDefault;

         DateTime dtStart = new DateTime(DBLoader.Year, 1 + (int)sbRaces.Value, 1);
         DateTime dtEnd = dtStart.AddMonths(Convert.ToInt32(_intCalendarSizeActual)).AddDays(-1);

         this.tlpRaceListPT.StartDate = dtStart;
         this.tlpRaceListPT.EndDate = dtEnd;
         this.tlpRaceListC0.StartDate = dtStart;
         this.tlpRaceListC0.EndDate = dtEnd;
         this.tlpRaceListC1.StartDate = dtStart;
         this.tlpRaceListC1.EndDate = dtEnd;
         this.tlpRaceListC2.StartDate = dtStart;
         this.tlpRaceListC2.EndDate = dtEnd;
         this.tlpRaceListXX.StartDate = dtStart;
         this.tlpRaceListXX.EndDate = dtEnd;
         this.tlpTeamRace.StartDate = dtStart;
         this.tlpTeamRace.EndDate = dtEnd;

         for (int i = 0; i < _dvCyclist.Count; ++i)
         {
            _tlpCyclist[i].StartDate = dtStart;
            _tlpCyclist[i].EndDate = dtEnd;
         }
         for (int i = 0; i < Const.MaxGroups; ++i)
         {
            _tlpCyclist[Const.MaxCyclist + i].StartDate = dtStart;
            _tlpCyclist[Const.MaxCyclist + i].EndDate = dtEnd;
         }

         this.SetFitnessPlanSize();
      }

      private void SetFitnessPlanSize()
      {
         Int32 intMonth = 0;
         if (this.cboFitnessPlanSize.SelectedIndex > -1)
         {
            Int16 intSize = Convert.ToInt16(this.cboFitnessPlanSize.SelectedValue.ToString());
            if (_intFitnessPlanSizeActual != intSize)
            {
               if (intSize == -1)
               {
                  _intFitnessPlanSizeActual = _intCalendarSizeActual;
                  intMonth = (int)sbRaces.Value;
               }
               else 
               {
                  _intFitnessPlanSizeActual = intSize;
               }
               App.Current.Properties["FitnessPlanSize"] = intSize.ToString();
            }
         }
         else
            _intFitnessPlanSizeActual = _intCalendarSizeActual;

         DateTime dtStart = new DateTime(DBLoader.Year, 1 + intMonth, 1);
         DateTime dtEnd = dtStart.AddMonths(Convert.ToInt32(_intFitnessPlanSizeActual)).AddDays(-1);

         this.tlpCyclistRace.StartDate = dtStart;
         this.tlpCyclistRace.EndDate = dtEnd;
         this.tlpCyclistFitness.StartDate = dtStart;
         this.tlpCyclistFitness.EndDate = dtEnd;
      }

      #endregion


      #region Planung

      #region Rennen auswählen

      private void tlpRaceList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
      {
         _pntDragStart = e.GetPosition(null);
         e.Handled = true;
      }

      private void tlpRaceList_PreviewMouseMove(object sender, MouseEventArgs e)
      {
         Point pntMousePosition = e.GetPosition(null);
         Vector vMove = _pntDragStart - pntMousePosition;
         if (e.LeftButton == MouseButtonState.Pressed)
            if ( Math.Abs(vMove.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(vMove.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
               DataRowView drvRace = null;
               Grid grd = FindAnchestor<Grid>((DependencyObject)e.OriginalSource);
               try { drvRace = _dvRace.FindRows((String)grd.GetValue(HlpProp.IdProperty))[0]; }
               catch { return; }
               DragDrop.DoDragDrop(grd, new DataObject("Race", drvRace), DragDropEffects.Copy);
            }
         e.Handled = true;
      }

      private static T FindAnchestor<T>(DependencyObject current) where T : DependencyObject
      {
         do
         {
            if (current is T)
               return (T)current;
            current = VisualTreeHelper.GetParent(current);
         }
         while (current != null);
         return null;
      }

      #endregion


      #region Race - Ereignisse

      private void Race_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (e.RightButton == MouseButtonState.Pressed)
            this.SetParticipationTeam((String)((Grid)sender).GetValue(HlpProp.IdProperty), true);
            //this.SetParticipationTeam(((Grid)sender).Name.Substring(2)); // An-/Abwählen möglich >> gefährlich, da Rennplanung gelöscht wird (Gruppen/Fahrer)
         e.Handled = true;
      }

      #endregion


      #region Team-Race - Ereignisse

      private void tlpTeamRace_DragEnter(object sender, DragEventArgs e)
      {
         if ((bool)((DataRowView)e.Data.GetData("Race"))["Participation_Team"])
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpTeamRace_DragOver(object sender, DragEventArgs e)
      {
         if ((bool)((DataRowView)e.Data.GetData("Race"))["Participation_Team"])
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpTeamRace_Drop(object sender, DragEventArgs e)
      {
         //if (e.Data.GetDataPresent("Race"))
         this.SetParticipationTeam((DataRowView)e.Data.GetData("Race"), true);
         e.Handled = true;
      }

      private void TeamRace_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (e.RightButton == MouseButtonState.Pressed)
            this.SetParticipationTeam((String)((Grid)sender).GetValue(HlpProp.IdProperty), false);
         e.Handled = true;
      }

      #endregion


      #region Cyclist-Group - Ereignisse

      private void tlpGroupRace_DragEnter(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         if (DBLoader.RaceIsOverlapping(Const.MaxCyclist + (Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty), (DateTime)drvRace["StartDate"], (DateTime)drvRace["EndDate"]))
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpGroupRace_DragOver(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         if (DBLoader.RaceIsOverlapping(Const.MaxCyclist + (Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty), (DateTime)drvRace["StartDate"], (DateTime)drvRace["EndDate"]))
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpGroupRace_Drop(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         // Sicherstellen, dass Rennen überhaupt ausgewählt ist
         if (this.SetParticipationTeam(drvRace, true))
         {  // Rennen in die Gruppe übernehmen (inkl. alle Fahrer)
            this.SetParticipationGroup(drvRace, (Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty), true);
         }
         e.Handled = true;
      }

      private void GroupRace_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (e.RightButton == MouseButtonState.Pressed)
            this.SetParticipationGroup((String)((Grid)sender).GetValue(HlpProp.IdProperty), (Int16)((TimelinePanel)((Grid)sender).Parent).GetValue(HlpProp.IndexProperty), false);
         e.Handled = true;
      }

      #endregion


      #region Cyclist - Ereignisse

      private void tlpCyclistRace_DragEnter(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         if (DBLoader.RaceIsOverlapping((Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty), (DateTime)drvRace["StartDate"], (DateTime)drvRace["EndDate"]))
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpCyclistRace_DragOver(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         if (DBLoader.RaceIsOverlapping((Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty), (DateTime)drvRace["StartDate"], (DateTime)drvRace["EndDate"]))
            e.Effects = DragDropEffects.None;
         e.Handled = true;
      }

      private void tlpCyclistRace_Drop(object sender, DragEventArgs e)
      {
         DataRowView drvRace = (DataRowView)e.Data.GetData("Race");
         // Sicherstellen, dass Rennen überhaupt ausgewählt ist
         if (this.SetParticipationTeam(drvRace, true))
         {  // Rennen dem Fahrer zuweisen
            Int16 intCyclistIndex = (Int16)((TimelinePanel)sender).GetValue(HlpProp.IndexProperty);
            this.SetParticipationCyclist(drvRace, intCyclistIndex, true);
         }
         e.Handled = true;
      }

      private void CyclistRace_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (e.RightButton == MouseButtonState.Pressed)
            this.SetParticipationCyclist((String)((Grid)sender).GetValue(HlpProp.IdProperty), (Int16)((TimelinePanel)((Grid)sender).Parent).GetValue(HlpProp.IndexProperty), false);
         e.Handled = true;
      }

      #endregion


      #region Participation - Team

      private bool SetParticipationTeam(string strIDrace)
      {
         DataRowView drv = _dvRace.FindRows(strIDrace)[0];
         bool blnParticipate = (bool)drv["Participation_Team"];
         return this.SetParticipationTeam(drv, !blnParticipate);
      }

      private bool SetParticipationTeam(string strIDrace, bool blnParticipate)
      {
         return this.SetParticipationTeam(_dvRace.FindRows(strIDrace)[0], blnParticipate);
      }

      private bool SetParticipationTeam(DataRowView drvRace, bool blnParticipate)
      {
         if (blnParticipate && !(Boolean)drvRace["Participation_Team"])
         {
            switch (drvRace["Participation_Allowed"].ToString())
            {
               case "YES":
                  // Teilnahme erlaubt
                  break;
               case "UCI":
                  // Teilnahme nicht erlaubt gemäss UCI Regeln
                  LanguageOptions.ShowMessage("Page_AdvancedPlanner/ParticipationNotAllowedUCI", MessageBoxButton.OK);
                  return false;
               case "ERROR":
                  // Unbekannter Fehler
                  if (LanguageOptions.ShowMessage("Page_AdvancedPlanner/ParticipationNotAllowedError", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                     return false;
                  break;
               default:
                  string strTeamCount = drvRace["Participation_Allowed"].ToString().Substring(10);
                  if (LanguageOptions.ShowMessage("Page_AdvancedPlanner/ParticipationNotAllowedTeamCount", strTeamCount, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                     return false;
                  break;
            }
         }
         drvRace["Participation_Team"] = blnParticipate;
         string strIDrace = drvRace["IDrace"].ToString();
         foreach (UIElement child in this.tlpTeamRace.Children)
         {
            string strChildID = (String)child.GetValue(HlpProp.IdProperty);
            if (strChildID == strIDrace)
            {
               child.SetValue(TimelinePanel.EventIsActiveProperty, blnParticipate);
               break;
            }
         }
         this.tlpTeamRace.Measure(new Size(this.ActualWidth, this.ActualHeight));
         if (!blnParticipate)
            for (Int16 i = 0; i < Const.MaxGroups; ++i)
               this.SetParticipationGroup(drvRace, i, blnParticipate);
         DBLoader.CalculatePlannedRacedays();
         this.RefreshPlannedRacedays();
         return true;
      }

      #endregion


      #region Participation - Group

      private void SetParticipationGroup(string strIDrace, Int16 intGroupIndex, bool blnParticipate)
      {
         this.SetParticipationGroup(_dvRace.FindRows(strIDrace)[0], intGroupIndex, blnParticipate);
      }

      private void SetParticipationGroup(DataRowView drvRace, Int16 intGroupIndex, bool blnParticipate)
      {
         string strIDrace = drvRace["IDrace"].ToString();
         drvRace[string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + intGroupIndex)] = blnParticipate;
         this.TimelinePanelSetParticipate(strIDrace, (Int16)(Const.MaxCyclist + intGroupIndex), blnParticipate);
         DBLoader.CalculatePlannedRacedays(Convert.ToInt16(strIDrace), Const.MaxCyclist + intGroupIndex, blnParticipate);
         foreach (DataRowView drvC in _dvCyclist.FindRows(Math.Pow(2, intGroupIndex)))
         {
            Int16 intIndex = (Int16)drvC["HelpIndex"];
            drvRace[string.Format("Participation_Cyclist{0:00}", intIndex)] = blnParticipate;
            this.TimelinePanelSetParticipate(strIDrace, intIndex, blnParticipate);
            if (_dvCyclist_Popup.Count > 0 && intIndex == (Int16)_dvCyclist_Popup[0]["HelpIndex"])
               this.RepaintRaces();
         }
         this.RefreshPlannedRacedays();
      }

      #endregion


      #region Participation - Cyclist

      private void SetParticipationCyclist(string strIDrace, Int16 intCyclistIndex, bool blnParticipate)
      {
         this.SetParticipationCyclist(_dvRace.FindRows(strIDrace)[0], intCyclistIndex, blnParticipate);
      }

      private void SetParticipationCyclist(DataRowView drvRace, Int16 intCyclistIndex, bool blnParticipate)
      {
         string strIDrace = drvRace["IDrace"].ToString();
         drvRace[string.Format("Participation_Cyclist{0:00}", intCyclistIndex)] = blnParticipate;
         this.TimelinePanelSetParticipate(strIDrace, intCyclistIndex, blnParticipate);
         DBLoader.CalculatePlannedRacedays(Convert.ToInt16(strIDrace), intCyclistIndex, blnParticipate);
         this.RefreshPlannedRacedays();
         if (_dvCyclist_Popup.Count > 0 && intCyclistIndex == (Int16)_dvCyclist_Popup[0]["HelpIndex"])
            this.RepaintRaces();
      }

      #endregion


      #region Participation - Show

      private void TimelinePanelSetParticipate(string strIDrace, Int16 intIndex, bool blnParticipate)
      {
         foreach (UIElement child in _tlpCyclist[intIndex].Children)
         {
            string strChildID = (String)child.GetValue(HlpProp.IdProperty);// child.GetValue(NameProperty).ToString().Substring(2);
            if (strChildID == strIDrace)
            {
               child.SetValue(TimelinePanel.EventIsActiveProperty, blnParticipate);
               break;
            }
         }
         _tlpCyclist[intIndex].Measure(new Size(this.ActualWidth, this.ActualHeight));
      }

      private void RefreshPlannedRacedays()
      {
         for (int i = 0; i < _dvCyclist.Count; ++i)
            _tbCyclist[(Int16)_dvCyclist[i]["HelpIndex"], 1].Text = string.Format("{0:00}", _dvCyclist[i]["RaceDaysPlanned"]);
      }

      #endregion

      #endregion


      #region Filter

      private void ckbRaceFilterSponsorObjective_Click(object sender, RoutedEventArgs e)
      {
         this.tlpRaceListPT.ShowActiveEventsOnly = (bool)this.ckbRaceFilterSponsorObjective.IsChecked;
         this.tlpRaceListC0.ShowActiveEventsOnly = (bool)this.ckbRaceFilterSponsorObjective.IsChecked;
         this.tlpRaceListC1.ShowActiveEventsOnly = (bool)this.ckbRaceFilterSponsorObjective.IsChecked;
         this.tlpRaceListC2.ShowActiveEventsOnly = (bool)this.ckbRaceFilterSponsorObjective.IsChecked;
         this.tlpRaceListXX.ShowActiveEventsOnly = (bool)this.ckbRaceFilterSponsorObjective.IsChecked;
         e.Handled = true;
      }

      private void ckbRaceFilterAllowed_Click(object sender, RoutedEventArgs e)
      {
         this.tlpRaceListPT.ShowAllowedEventsOnly = (bool)this.ckbRaceFilterAllowed.IsChecked;
         this.tlpRaceListC0.ShowAllowedEventsOnly = (bool)this.ckbRaceFilterAllowed.IsChecked;
         this.tlpRaceListC1.ShowAllowedEventsOnly = (bool)this.ckbRaceFilterAllowed.IsChecked;
         this.tlpRaceListC2.ShowAllowedEventsOnly = (bool)this.ckbRaceFilterAllowed.IsChecked;
         this.tlpRaceListXX.ShowAllowedEventsOnly = (bool)this.ckbRaceFilterAllowed.IsChecked;
         e.Handled = true;
      }

      private void ckbRaceFilter_Click(object sender, RoutedEventArgs e)
      {
         string strHlp = string.Empty;
         string[] strClassFilter = new string[0];
         if ((bool)this.ckbRaceFilterWorldTourStage.IsChecked)
            strHlp = string.Concat(strHlp, ",FranceGrandTour,AutreGrandTour,AutresTours");
         if ((bool)this.ckbRaceFilterWorldTourClassic.IsChecked)
            strHlp = string.Concat(strHlp, ",ClassiqueMajeure,AutresClassiques");
         if ((bool)this.ckbRaceFilterHorsCategoryStage.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont2HC");
         if ((bool)this.ckbRaceFilterHorsCategoryClassic.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont1HC");
         if ((bool)this.ckbRaceFilterCategory1Stage.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont21");
         if ((bool)this.ckbRaceFilterCategory1Classic.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont11");
         if ((bool)this.ckbRaceFilterCategory2Stage.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont22");
         if ((bool)this.ckbRaceFilterCategory2Classic.IsChecked)
            strHlp = string.Concat(strHlp, ",Cont12");
         if ((bool)this.ckbRaceFilterOthers.IsChecked)
            strHlp = string.Concat(strHlp, ",Cya_Cup,Cont22U,Cont12U");
         if (strHlp.Length > 0)
            strClassFilter = strHlp.Substring(1).Split(',');
         this.tlpRaceListPT.ClassFilter = strClassFilter;
         this.tlpRaceListC0.ClassFilter = strClassFilter;
         this.tlpRaceListC1.ClassFilter = strClassFilter;
         this.tlpRaceListC2.ClassFilter = strClassFilter;
         this.tlpRaceListXX.ClassFilter = strClassFilter;
         e.Handled = true;
      }

      private void cboRaceFilterCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string[] strCountryFilter = new string[0];
         if (this.cboRaceFilterCountry.SelectedIndex > -1)
            if (Convert.ToInt16(this.cboRaceFilterCountry.SelectedValue.ToString()) > 0)
               strCountryFilter = new string[] { this.cboRaceFilterCountry.SelectedValue.ToString() };
         this.tlpRaceListPT.CountryFilter = strCountryFilter;
         this.tlpRaceListC0.CountryFilter = strCountryFilter;
         this.tlpRaceListC1.CountryFilter = strCountryFilter;
         this.tlpRaceListC2.CountryFilter = strCountryFilter;
         this.tlpRaceListXX.CountryFilter = strCountryFilter;
         e.Handled = true;
      }

      #endregion


      #region Cyclist - Detail

      #region Cyclist auswählen

      private void CyclistHeader_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (!this.tlpCyclistFitness_ChangesSaved()) return;
         this.ClearSelection();
         if (e.RightButton == MouseButtonState.Pressed)
            return; // Rechte Maustaste = Reset
         Grid g = (Grid)sender;
         _intCyclistIndexPrevious = (Int16)g.GetValue(HlpProp.IndexProperty);
         _dvCyclist_Popup.RowFilter = string.Concat("IDcyclist=", g.GetValue(HlpProp.IdProperty));
         this.CyclistSelected((e.ClickCount == 2));
         e.Handled = true;
      }

      private void CyclistTimeLine_MouseDown(object sender, MouseButtonEventArgs e)
      {  //noch nicht perfekt
         if (!this.tlpCyclistFitness_ChangesSaved()) return;
         this.ClearSelection();
         if (e.RightButton == MouseButtonState.Pressed)
            return; // Rechte Maustaste = Reset
         TimelinePanel t = (TimelinePanel)sender;
         _intCyclistIndexPrevious = (Int16)t.GetValue(HlpProp.IndexProperty);
         _dvCyclist_Popup.RowFilter = string.Concat("IDcyclist=", t.GetValue(HlpProp.IdProperty));
         this.CyclistSelected((e.ClickCount == 2));
         e.Handled = true;
      }

      private void CyclistSelected(bool blnDoubleClicked)
      {
         this.lblCyclist.Content = string.Concat(_dvCyclist_Popup[0]["gene_sz_firstname"], " ", _dvCyclist_Popup[0]["gene_sz_lastname"]);
         _tlpCyclist[_intCyclistIndexPrevious].Background = _scbBackgroundSelected;
         _grdCyclist[_intCyclistIndexPrevious].Background = _scbBackgroundSelected;
         this.RepaintRaces();
         _FitnessPlanUndo = _dvCyclist_Popup[0]["FitnessProgramList"].ToString();
         this.tlpCyclistFitness_Load(_FitnessPlanUndo);
         if (this.dgOwnTeam.ItemsSource == null)
            this.dgOwnTeam.ItemsSource = _dvCyclist_Popup;
         if (blnDoubleClicked)
            this.expCyclist.IsExpanded = true;
      }

      private void RepaintRaces() 
      {
         _dvRace.Sort = "StartDate";
         this.ClearRaces();
         foreach (DataRowView drvR in _dvRace)
         {
            bool blnActive = (bool)drvR[string.Format("Participation_Cyclist{0:00}", (Int16)_dvCyclist_Popup[0]["HelpIndex"])];
            if (blnActive)
            {
               DateTime dtEnd = (DateTime)drvR["EndDate"];
               string strIDrace = drvR["IDrace"].ToString();
               DateTime dtStart = (DateTime)drvR["StartDate"];
               string strTypeTour = drvR["HlpTypeTour"].ToString();
               string strBackground = drvR["HlpClassColor"].ToString();
               string strRaceName = drvR["gene_sz_race_name"].ToString();
               string strSponsorObjective = drvR["SponsorObjectif"].ToString();
               this.tlpCyclistRace.Children.Add(RaceObject(strIDrace, strRaceName, "", "", "", strSponsorObjective, strTypeTour, dtStart, dtEnd, false, string.Empty, strBackground, "Fitness"));
               TextBlock txbRaceLabel = new TextBlock();
               txbRaceLabel.SetValue(TimelinePanel.EventStartDateProperty, dtStart);
               txbRaceLabel.RenderTransform = new RotateTransform(90, 5, 10);
               txbRaceLabel.SetValue(TimelinePanel.IsLabelProperty, true);
               //if (string.IsNullOrEmpty(strSponsorObjective))
               //   txbRaceLabel.ToolTip = strRaceName;
               //else
               {
                  //txbRaceLabel.ToolTip = strRaceName + Environment.NewLine + strSponsorObjective;
                  txbRaceLabel.FontWeight = FontWeights.Bold;
               }
               txbRaceLabel.Text = strRaceName;
               txbRaceLabel.Height = 150;
               this.tlpCyclistRace.Children.Add(txbRaceLabel);
            }
         }
         _dvRace.Sort = "IDrace";
      }

      private void ClearRaces()
      {
         Type tBorder = typeof(Border);
         for (int i = this.tlpCyclistRace.Children.Count - 1; i >= 0; i--)
            if (this.tlpCyclistRace.Children[i].GetType() != tBorder)
               this.tlpCyclistRace.Children.RemoveAt(i);
      }

      private void ClearSelection()
      {
         if (_intCyclistIndexPrevious > -1)
         {
            _tlpCyclist[_intCyclistIndexPrevious].Background = _scbBackgroundNormal;
            _grdCyclist[_intCyclistIndexPrevious].Background = _scbBackgroundNormal;
         }
      }

      #endregion


      #region Fitness Plan

      private void cmdFitnessPlanCopy_Click(object sender, RoutedEventArgs e)
      {
         string strWeeks = string.Empty;
         for (int i = 0; i < _intFitnessPlanWeeks; ++i)
         {
            Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[i];
            strWeeks += "," + ((int)r.Height / _intLevelHeight);
         }
         _FitnessPlanCopy = strWeeks.Substring(1);
      }

      private void cmdFitnessPlanPaste_Click(object sender, RoutedEventArgs e)
      {
         this.tlpCyclistFitness_Load(_FitnessPlanCopy);
         _blnFitnessProgramDirty = true;
      }

      private void cmdFitnessPlanClear_Click(object sender, RoutedEventArgs e)
      {
         this.tlpCyclistFitness_Load("");
         _blnFitnessProgramDirty = true;
      }

      private void cmdFitnessPlanUndo_Click(object sender, RoutedEventArgs e)
      {
         this.tlpCyclistFitness_Load(_FitnessPlanUndo);
      }

      private void tlpCyclistFitness_MouseUp(object sender, MouseButtonEventArgs e)
      {
         int intWeek = (Int16)((Rectangle)sender).GetValue(HlpProp.IndexProperty);
         int intDirection = 0;
         switch (e.ChangedButton)
         {
            case MouseButton.Left:
               intDirection = 1;
               break;
            case MouseButton.Right:
               intDirection = -1;
               break;
         }
         this.tlpCyclistFitness_ChangeWeek(intWeek, intDirection);
         e.Handled = true;
      }

      private void tlpCyclistFitness_ChangeWeek(int intWeek, int intDirection)
      {
         this.tlpCyclistFitness_ChangeWeek(intWeek, intDirection, "", new List<int>());
      }
      private void tlpCyclistFitness_ChangeWeek(int intWeek, int intDirection, string strAction, List<int> intValue)
      {
         if (intWeek > (_intFitnessPlanWeeks-1) || intWeek < 0)
            return;
         _blnFitnessProgramDirty = true;
         Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[intWeek];
         int intLevel = ((int)r.Height / _intLevelHeight);
         List<int> intPrevious = new List<int>();
         List<int> intNext = new List<int>();
         switch (strAction)
         {
            case "List":
               if (intLevel < intValue[0])
                  intLevel = intValue[0];
               if (intValue.Count() > 1)
               {
                  intValue.RemoveAt(0);
                  if (intDirection == 1)
                     intNext = intValue;
                  else
                     intPrevious = intValue;
               }
               break;
            case "Increment":
               if (intLevel <= intValue[0])
                  return;
               intLevel = intValue[0];
               if (intDirection == 1)
                  intNext = new List<int> { intLevel + 1 };
               else
                  intPrevious = new List<int> { intLevel + 1 };
               break;
            default:
               intLevel = intLevel + intDirection;
               switch (intLevel)
               {
                  case 0:
                     intDirection = 1;
                     intLevel = 6;
                     break;
                  case 7:
                     intDirection = -1;
                     intLevel = 1;
                     break;
               }
               switch (intDirection)
               {
                  case 1:
                     strAction = "List";
                     switch (intLevel)
                     {
                        case 6:
                           intPrevious = new List<int> { 5, 5, 5, 4, 4, 4, 3, 3, 2, 2 };
                           intNext = new List<int> { 5, 4, 3, 2 };
                           break;
                        case 5:
                           intPrevious = new List<int> { 4, 4, 4, 3, 3, 2, 2 };
                           intNext = new List<int> { 4, 3, 2 };
                           break;
                        case 4:
                           intPrevious = new List<int> { 3, 3, 2, 2 };
                           intNext = new List<int> { 3, 2 };
                           break;
                        case 3:
                           intPrevious = new List<int> { 2, 2 };
                           intNext = new List<int> { 2 };
                           break;
                     }
                     break;
                  case -1:
                     intPrevious = new List<int> { intLevel + 1 };
                     intNext = new List<int> { intLevel + 1 };
                     strAction = "Increment";
                     break;
               }
               break;
         }
         r.BeginAnimation(OpacityProperty, _animInactive);
         r.Fill = DBLoader.ColorTrainingLevel(intLevel);
         r.Height = intLevel * _intLevelHeight;
         if (intPrevious.Count() > 0)
            this.tlpCyclistFitness_ChangeWeek(intWeek - 1, -1, strAction, intPrevious);
         if (intNext.Count() > 0)
            this.tlpCyclistFitness_ChangeWeek(intWeek + 1, 1, strAction, intNext);
      }

      private void tlpCyclistFitness_Load(string strFitnessTraining)
      {
         int[] intWeeks = new int[_intFitnessPlanWeeks];
         if (strFitnessTraining.Length == 87)
         {
            string[] strWeeks = strFitnessTraining.Split(',');
            try { intWeeks = Array.ConvertAll<string, int>(strWeeks, int.Parse); }
            catch { }
         }
         for (int i = 0; i < _intFitnessPlanWeeks; ++i)
         {
            //if (intWeeks[i] == 0)
            //   intWeeks[i] = 1;
            Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[i];
            r.Fill = DBLoader.ColorTrainingLevel(intWeeks[i]);
            r.BeginAnimation(OpacityProperty, _animInactive);
            r.Height = intWeeks[i] * _intLevelHeight;
         }
         _blnFitnessProgramDirty = false;
      }

      private bool tlpCyclistFitness_ChangesSaved()
      {
         if ((_dvCyclist_Popup.Count == 1) && _blnFitnessProgramDirty)
         {
            if (this.tlpCyclistFitness_IsValid())
            {
               string strWeeks = string.Empty;
               for (int i = 0; i < _intFitnessPlanWeeks; ++i)
               {
                  Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[i];
                  strWeeks += "," + ((int)r.Height / _intLevelHeight);
               }
               _dvCyclist_Popup[0]["FitnessProgramList"] = strWeeks.Substring(1);
               _blnFitnessProgramDirty = false;
            }
            else
            {
               LanguageOptions.ShowMessage("Page_AdvancedPlanner/Cyclist/FitnessPlan/ProgramInvalid", MessageBoxButton.OK);
               return false;
            }
         }
         return true;
      }

      private bool tlpCyclistFitness_IsValid()
      {
         int[] intChecks = new int[] { 2, 2, 3, 3, 4, 4, 4, 5, 5, 5, 6, 5, 4, 3, 2 };
         int[] intWeeks = new int[_intFitnessPlanWeeks];
         for (int i = 0; i < _intFitnessPlanWeeks; ++i)
         {
            Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[i];
            intWeeks[i] = (int)r.Height / _intLevelHeight;
         }
         bool blnValid = true;
         for (int i = 0; i < _intFitnessPlanWeeks; ++i)
            this.tlpCyclistFitness.Children[i].BeginAnimation(OpacityProperty, _animInactive);
         //// Zu intensiver Beginn?
         //for (int i = 0; i < 11; ++i)
         //{
         //   if (intWeeks[i] > intChecks[i])
         //   {
         //      Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[i];
         //      r.BeginAnimation(OpacityProperty, _animActive);
         //      blnValid = false;
         //   }
         //}
         for (int intWeek = 0; intWeek < _intFitnessPlanWeeks; ++intWeek)
            if (intWeeks[intWeek] > 2)
            {
               // Zu schnelle Steigerung?
               for (int i = 2; i < 11; ++i)
                  if (intChecks[i] == intWeeks[intWeek])
                  {
                     int intCheck = intWeek;
                     for (int s = (i - 1); s >= 0; --s)
                        if (intCheck > 0 && intWeeks[--intCheck] < intChecks[s])
                        {
                           Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[intCheck];
                           r.BeginAnimation(OpacityProperty, _animActive);
                           blnValid = false;
                        }
                     break;
                  }
               // Zu schnelle Senkung?
               for (int i = 14; i > 9; --i)
                  if (intChecks[i] == intWeeks[intWeek])
                  {
                     int intCheck = intWeek;
                     for (int s = (i + 1); s < 15; ++s)
                        if (intCheck < (_intFitnessPlanWeeks-1) && intWeeks[++intCheck] < intChecks[s])
                        {
                           Rectangle r = (Rectangle)this.tlpCyclistFitness.Children[intCheck];
                           r.BeginAnimation(OpacityProperty, _animActive);
                           blnValid = false;
                        }
                     break;
                  }
            }
         return blnValid;
      }

      #endregion


      #region Cyclist-Eigenschaften

      private void regLayoutSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string strCategory = string.Empty;
         foreach (TabItem t in this.regLayoutSelection.Items)
            if (t.IsSelected)
            {
               strCategory = t.Name.Substring(3);
               break;
            }
         foreach (DataGridColumn c in this.dgOwnTeam.Columns)
         {
            switch (c.SortMemberPath)
            {
               case "SortGroup":
               case "SortIndex":
               case "gene_sz_lastname":
               case "gene_sz_firstname":
               case null:
                  break;
               default:
                  c.Visibility = DBLoader.FieldIsVisible(string.Concat("Page_OwnTeam/", strCategory, "/", c.SortMemberPath));
                  break;
            }
         }
         e.Handled = true;
      }

      private void dgOwnTeam_TranslateColumnHeaders()
      {
         foreach (DataGridColumn c in this.dgOwnTeam.Columns)
         {
            string s = LanguageOptions.Text(string.Concat("DYN_cyclist/", c.SortMemberPath));
            if (!string.IsNullOrEmpty(s))
               c.Header = s;
         }
      }

      #endregion

      #endregion


      private void ckbVisibilityRaceName_Click(object sender, RoutedEventArgs e)
      {
         if ((bool)this.ckbVisibilityRaceName.IsChecked)
            this.txbVisibilityRaceName.Visibility = System.Windows.Visibility.Visible;
         else
            this.txbVisibilityRaceName.Visibility = System.Windows.Visibility.Collapsed;
      }

      private void ckbVisibilityStageProfile_Click(object sender, RoutedEventArgs e)
      {
         if ((bool)this.ckbVisibilityStageProfile.IsChecked)
            this.txbVisibilityStageProfile.Visibility = System.Windows.Visibility.Visible;
         else
            this.txbVisibilityStageProfile.Visibility = System.Windows.Visibility.Collapsed;
      }

   }

}
