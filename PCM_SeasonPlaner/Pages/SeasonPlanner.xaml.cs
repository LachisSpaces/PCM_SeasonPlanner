using PCM_SeasonPlaner.Languages;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Microsoft.Windows.Controls;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows;
using System.Data;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für SeasonPlanner.xaml
   /// </summary>
   public partial class SeasonPlanner : Page
   {

      private Typeface _tfDataGrid;

      private DataView _dvRace;
      private DataView _dvCyclist;
      private DataView _dvCyclist_Popup;
      private DataView _dvSubsquadConfig;

      private int _intPopupHelpIndex;
      private Point _pntMouse, _pntPopup;
      private bool _blnPopupMove = false;
      private bool _blnPopupIsOpen = false;

      private string _strDefaultRaceTimeLineSize;
      private string _strDefaultFilterRace = "HlpClassName NOT IN ('NationalChampionship','NationalChampionshipITT','Olympics','OlympicsTT','Track','U23_1NCup','U23_2NCup')"; // ACHTUNG: Im Code wird angenommen, dass nur HlpClassName gefiltert wird
      private string[] _strColumnName = new string[37];
      private Int16 _intCyclistColIndexPrevious = -1;
      private Int16[] _intCyclistColIndex;

      private DataGridColumn _dgcHeaderRaceInfo;
      private bool _blnRaceTimelineFilled = false;
      private bool _blnPageLoaded = false;
      private ScrollViewer _svRaceScrollbar;

      public SeasonPlanner()
      {
         _intCyclistColIndex = new Int16[Const.MaxCyclist];
         // Interface vorbereiten
         InitializeComponent();
         this.dgRace.AddHandler(MouseWheelEvent, new RoutedEventHandler(dgRace_MouseWheelHorizontal), true);
         this.dgRace.ItemContainerGenerator.StatusChanged += new EventHandler(dgRace_ItemContainerGenerator_StatusChanged);
         // Oberste Spalte sichern, damit sie nicht verloren geht
         _dgcHeaderRaceInfo = this.dgRace.Columns[0];
         // Einstellungen setzten
         _strDefaultRaceTimeLineSize = (string)App.Current.Properties["CalendarSize"];
         this.expRaceCalendar.IsExpanded = (bool)App.Current.Properties["RaceCalendarExpanded"];
         this.ckbShowActiveEventsOnlyCalendar.IsChecked = (bool)App.Current.Properties["ShowActiveEventsOnlyCalendar"];
         this.ckbShowActiveEventsOnlyPlan.IsChecked = (bool)App.Current.Properties["ShowActiveEventsOnlyPlan"];
      }

      private void Page_Loaded(object sender, RoutedEventArgs e)
      {
         _blnPageLoaded = true;
         string strPageName = LanguageOptions.Text("Page_SeasonPlaner");
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + strPageName;
         this.Title = strPageName;
         // Daten vorbereiten
         _dvSubsquadConfig = DBLoader.GetDataView("DYN_subsquad_config", false);
         _dvCyclist = DBLoader.GetDataView("DYN_cyclist", true);
         _dvRace = DBLoader.GetDataView("STA_Race", false);
         _dvRace.Sort = "StartDate";
         // Race-Kalender mit Events füllen
         this.tlpRaceTimeline_AddRaces();
         // Source für Datagrids setzen
         this.dgRace_SetFilter(); // Filter setzen, da ..OnlyPlan nicht automatisch funktioniert (nur Userinteraktion)
         this.dgRace.ItemsSource = _dvRace;
         // Race-Kalender anzeigen
         this.tlpRaceTimeline_Update();
         // Planer anpassen und mit den Fahrern füllen
         this.dgRaces_TranslateColumnHeader();
         this.dgRaces_AddColumns();
         // Popup vorbereiten
         _dvCyclist_Popup = DBLoader.GetDataView("DYN_cyclist", false);
         this.dgOwnTeam_TranslateColumnHeaders();
         // Source für Country-Filter
         DataView dvHlp = DBLoader.GetDataView("STA_country", true);
         dvHlp.Sort = "CONSTANT";
         this.cboRaceFilterCountry.ItemsSource = dvHlp;
      }


      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
         this.dgOwnTeam.ItemsSource = null;
         this.dgRace.ItemsSource = null;
         _dvCyclist_Popup.Dispose();
         _dvCyclist_Popup = null;
         _dvCyclist.Dispose();
         _dvCyclist = null;
         _dvRace.Dispose();
         _dvRace = null;
      }

      public void SaveBeforeClosing()
      {
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.dgRace.CommitEdit();
         App.Current.Properties["RaceCalendarExpanded"] = this.expRaceCalendar.IsExpanded;
         App.Current.Properties["ShowActiveEventsOnlyPlan"] = this.ckbShowActiveEventsOnlyPlan.IsChecked;
         App.Current.Properties["ShowActiveEventsOnlyCalendar"] = this.ckbShowActiveEventsOnlyCalendar.IsChecked;
      }


      void dgRace_ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
      {
         if (this.dgRace.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
         {
            this.sbRaces.Maximum = _dvRace.Count - (int)this.dgRace_Scrollbar.ViewportHeight - 1;
            this.sbRaces.ViewportSize = this.dgRace_Scrollbar.ViewportHeight;
            if (_blnPageLoaded)
            {
               this.tlpRaceTimeline_Update();
               _blnPageLoaded = false;
            }
         }
      }

      private void dgRace_SetFilter()
      {
         string strClassFilter = "", strFilter = "";

         if ((bool)this.ckbRaceFilterWorldTourStage.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'FranceGrandTour','AutreGrandTour','AutresTours'");
         if ((bool)this.ckbRaceFilterWorldTourClassic.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'ClassiqueMajeure','AutresClassiques'");
         if ((bool)this.ckbRaceFilterHorsCategoryStage.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont2HC'");
         if ((bool)this.ckbRaceFilterHorsCategoryClassic.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont1HC'");
         if ((bool)this.ckbRaceFilterCategory1Stage.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont21'");
         if ((bool)this.ckbRaceFilterCategory1Classic.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont11'");
         if ((bool)this.ckbRaceFilterCategory2Stage.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont22'");
         if ((bool)this.ckbRaceFilterCategory2Classic.IsChecked)
            strClassFilter = string.Concat(strClassFilter, ",'Cont12'");
         if (strClassFilter.Length > 0)
            strFilter = "HlpClassName IN (" + strClassFilter.Substring(1) + ")";
         else
            strFilter = _strDefaultFilterRace; 

         if (this.cboRaceFilterCountry.SelectedIndex > -1)
            if (Convert.ToInt16(this.cboRaceFilterCountry.SelectedValue.ToString()) > 0)
               strFilter = string.Concat(strFilter, " AND fkIDcountry=" + this.cboRaceFilterCountry.SelectedValue);

         if ((bool)this.ckbShowActiveEventsOnlyPlan.IsChecked)
            strFilter = string.Concat(strFilter, " AND Participation_Team=True");
         if (_blnPopupIsOpen && (bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
            strFilter = string.Concat(strFilter, " AND ", string.Format("Participation_Cyclist{0:00}=true", _intPopupHelpIndex));

         //if (strFilter != _strDefaultFilterRace)
         //   strFilter = '(' + strFilter + ") OR HlpMinRiders=-1";
         _dvRace.RowFilter = strFilter;
         //_dvRace.Sort = "StartDate";
      }

      private void dgRaces_TranslateColumnHeader()
      {
         string strHeader = "";
         DataGridColumn dgc = this.dgRace.Columns[0];
         string[] strColumns = "gene_sz_race_name,lf,StartDate,EndDate,gene_i_number_stages,Participation_Team,CyclistsAllowed,Participation_Count".Split(',');
         foreach (string strColumn in strColumns)
         {
            if (strColumn == "lf")
               strHeader += "\n\n\n";
            else
            {
               string s = LanguageOptions.Text(string.Concat("STA_race/", strColumn));
               if (!string.IsNullOrEmpty(s))
                  strHeader += "\n " + s;
            }
         }
         dgc.Header = strHeader.Substring(1);
      }

      private void dgRaces_AddColumns()
      {
         Int16 intColIndex = 0;
         int intGroupIndex = 0, intGroupIdxno = (int)Math.Pow(2, Const.MaxGroups);
         Type typBoolean = Type.GetType("System.Boolean");
         RotateTransform rtRotate = new RotateTransform(-90);
         this.dgRace.Columns.Clear();
         this.dgRace.Columns.Add(_dgcHeaderRaceInfo);
         _dvCyclist.Sort = "SortGroup, SortIndex, gene_sz_lastname";
         for (int i = 0; i < _dvCyclist.Count; ++i)
         {
            DataTemplate dt;
            string strColumn;
            int intSortGroup = (int)Math.Pow(2, Const.MaxGroups);
            try { intSortGroup = (Int16)_dvCyclist[i]["SortGroup"]; }
            catch { }
            DataGridTemplateColumn c;
            FrameworkElementFactory fwfGridCell, fwfGridHeader, fwfCheckBox, fwfTextBlock;
            if (intGroupIdxno != intSortGroup)
            {
               intGroupIdxno = intSortGroup;
               intGroupIndex = (Int16)(Math.Log(intGroupIdxno) / Math.Log(2));
               fwfGridHeader = new FrameworkElementFactory(typeof(Grid));
               fwfGridHeader.SetValue(Grid.WidthProperty, 110.0);
               fwfGridHeader.SetValue(Grid.LayoutTransformProperty, rtRotate);
               fwfGridHeader.SetValue(Grid.FlowDirectionProperty, FlowDirection.LeftToRight);
               fwfGridHeader.SetValue(Grid.MarginProperty, new Thickness(-4.0, -5.0, -5.0, -4.0));
               fwfGridHeader.SetValue(Grid.BackgroundProperty, new SolidColorBrush(Colors.DarkGray));
               fwfGridCell = new FrameworkElementFactory(typeof(Grid));
               fwfGridCell.SetValue(Grid.BackgroundProperty, new SolidColorBrush(Colors.DarkGray));
               fwfGridCell.SetValue(Grid.LayoutTransformProperty, rtRotate);
               fwfTextBlock = new FrameworkElementFactory(typeof(TextBlock));
               fwfTextBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Bottom);
               fwfTextBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
               if (intGroupIndex == Const.MaxGroups)
                  fwfTextBlock.SetValue(TextBlock.TextProperty, " " + LanguageOptions.Text("Page_SeasonPlaner/CyclistGrouping/NoGroup"));
               else
               {
                  strColumn = string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + intGroupIndex);
                  _dvSubsquadConfig.RowFilter = "SortGroup=" + intGroupIdxno;
                  try { fwfTextBlock.SetValue(TextBlock.TextProperty, " " + _dvSubsquadConfig[0]["gene_sz_name"].ToString()); }
                  catch { fwfTextBlock.SetValue(TextBlock.TextProperty, " " + LanguageOptions.Text("Page_SeasonPlaner/CyclistGrouping/Groups") + (intGroupIndex + 1)); }
                  fwfCheckBox = new FrameworkElementFactory(typeof(CheckBox));
                  fwfCheckBox.SetValue(CheckBox.NameProperty, strColumn);
                  fwfCheckBox.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
                  fwfCheckBox.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                  fwfCheckBox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(ckbParticipationCyclist_Click), true);
                  fwfCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + intGroupIndex)));
                  fwfGridCell.AppendChild(fwfCheckBox);
               }
               fwfGridHeader.AppendChild(fwfTextBlock);
               c = new DataGridTemplateColumn();
               dt = new DataTemplate();
               dt.VisualTree = fwfGridHeader;
               c.HeaderTemplate = dt;
               dt = new DataTemplate();
               dt.VisualTree = fwfGridCell;
               c.CellTemplate = dt;
               c.MaxWidth = 18;
               this.dgRace.Columns.Add((DataGridColumn)c);
               ++intColIndex;
            }
            Binding b = new Binding("Content");
            fwfGridHeader = new FrameworkElementFactory(typeof(Grid));
            fwfGridHeader.SetValue(Grid.WidthProperty, 110.0);
            fwfGridHeader.SetValue(Grid.LayoutTransformProperty, rtRotate);
            fwfGridHeader.SetValue(Grid.FlowDirectionProperty, FlowDirection.LeftToRight);
            fwfGridHeader.SetValue(Grid.MarginProperty, new Thickness(-4.0, -5.0, -5.0, -4.0));
            fwfGridHeader.SetValue(Grid.BackgroundProperty, new SolidColorBrush(Colors.LightGray));
            fwfGridHeader.AddHandler(Grid.MouseDownEvent, new MouseButtonEventHandler(CyclistHeader_MouseDown), true);
            fwfGridHeader.SetValue(Grid.NameProperty, "X" + _dvCyclist[i]["IDcyclist"] + "X" + _dvCyclist[i]["HelpIndex"] + "X" + i);
            fwfTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            fwfTextBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Bottom);
            fwfTextBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            b.RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent);
            fwfTextBlock.SetBinding(TextBlock.TextProperty, b);
            fwfGridHeader.AppendChild(fwfTextBlock);
            fwfTextBlock = new FrameworkElementFactory(typeof(TextBlock));
            fwfTextBlock.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Bottom);
            fwfTextBlock.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            fwfTextBlock.SetValue(TextBlock.TextProperty, ResizeString(" " + _dvCyclist[i]["gene_sz_firstname"].ToString().Substring(0, 1) + "." + _dvCyclist[i]["gene_sz_lastname"].ToString()));
            fwfGridHeader.AppendChild(fwfTextBlock);
            strColumn = string.Format("Participation_Cyclist{0:00}", _dvCyclist[i]["HelpIndex"]);
            fwfCheckBox = new FrameworkElementFactory(typeof(CheckBox));
            fwfCheckBox.SetValue(CheckBox.NameProperty, strColumn);
            fwfCheckBox.SetValue(CheckBox.LayoutTransformProperty, rtRotate);
            fwfCheckBox.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            fwfCheckBox.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            fwfCheckBox.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler(ckbParticipationCyclist_Click), true);
            fwfCheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding(strColumn));
            fwfGridCell = new FrameworkElementFactory(typeof(Grid));
            Binding b1 = new Binding(string.Format("FavoriteRace_Cyclist{0:00}", _dvCyclist[i]["HelpIndex"]));
            b1.Converter = new CvpFavoriteRaceBrushConverter();
            fwfGridCell.SetValue(Grid.BackgroundProperty, b1);
            fwfGridCell.AppendChild(fwfCheckBox);
            c = new DataGridTemplateColumn();
            c.Header = string.Format("{0:00}", _dvCyclist[i]["RaceDaysPlanned"]).PadLeft(30);
            _intCyclistColIndex[i] = ++intColIndex;
            dt = new DataTemplate();
            dt.VisualTree = fwfGridHeader;
            c.HeaderTemplate = dt;
            dt = new DataTemplate();
            dt.VisualTree = fwfGridCell;
            c.CellTemplate = dt;
            c.MaxWidth = 18;
            this.dgRace.Columns.Add((DataGridColumn)c);
         }
      }

      private void dgRace_DisplayPlannedRacedays()
      {
         for (int i = 0; i < _dvCyclist.Count; ++i)
         {
            Int16 c = _intCyclistColIndex[i];
            if (c > 0)
               this.dgRace.Columns[c].Header = string.Format("{0:00}", _dvCyclist[i]["RaceDaysPlanned"]).PadLeft(30);
         }
      }

      private void dgRace_MouseWheelHorizontal(object sender, RoutedEventArgs e)
      {
         MouseWheelEventArgs eargs = (MouseWheelEventArgs)e;
         sbRaces.Value -= 3 * (double)Math.Sign(eargs.Delta);
         this.tlpRaceTimeline_Update();
      }

      private ScrollViewer dgRace_Scrollbar
      {
         get
         {
            if (_svRaceScrollbar == null)
               _svRaceScrollbar = (ScrollViewer)((Decorator)VisualTreeHelper.GetChild(this.dgRace, 0)).Child;
            return _svRaceScrollbar;
         }
      }


      private void ckbParticipationTeam_Click(object sender, RoutedEventArgs e)
      {
         DataRowView drv = this.dgRace.CurrentCell.Item as DataRowView;
         if (drv != null)
         {
            string strID = drv["IDrace"].ToString();
            DateTime dtStart = (DateTime)drv["StartDate"];
            if (dtStart != new DateTime(DBLoader.Year, 12, 31))
            {
               bool blnParticipate = (bool)((CheckBox)sender).IsChecked;
               foreach (UIElement child in this.tlpRaceTimeline.Children)
               {
                  string strChildID = child.GetValue(NameProperty).ToString().Substring(2);
                  if (strChildID == strID)
                  {
                     child.SetValue(TimelinePanel.EventIsActiveProperty, blnParticipate);
                     break;
                  }
               }
               this.tlpRaceTimeline.Measure(new Size(this.ActualWidth, this.ActualHeight));
               DBLoader.CalculatePlannedRacedays();
               this.dgRace_DisplayPlannedRacedays();
            }
         }
      }

      private void ckbParticipationCyclist_Click(object sender, RoutedEventArgs e)
      {
         DataRowView drv = this.dgRace.CurrentCell.Item as DataRowView;
         if (drv != null)
         {
            //this.xdgRaceCyclists.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
            DBLoader.CalculatePlannedRacedays((Int32)drv["IDrace"], Convert.ToInt16(((CheckBox)sender).Name.Substring(21, 2)), (bool)((CheckBox)sender).IsChecked);
            this.dgRace_DisplayPlannedRacedays();
         }
      }


      private void sbRaces_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
      {
         if (this.dgRace.Items.Count > 0)
         {
            this.dgRace_Scrollbar.ScrollToVerticalOffset(sbRaces.Value);
            this.tlpRaceTimeline_Update();
         }
      }
     

      private void ckbShowActiveEventsOnlyPlan_Click(object sender, RoutedEventArgs e)
      {
         this.dgRace_SetFilter();
      }

      private void ckbRaceFilter_Click(object sender, RoutedEventArgs e)
      {
         this.dgRace_SetFilter();
      }

      private void cboRaceFilterCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         this.dgRace_SetFilter();
      }

      private void ckbShowActiveEventsOnlyCyclist_Click(object sender, RoutedEventArgs e)
      {
         this.dgRace_SetFilter();
      }

      private void cboRaceTimeLineSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         this.tlpRaceTimeline_Update();
      }


      private void tlpRaceTimeline_AddRaces()
      {
         if (_blnRaceTimelineFilled)
            return;
         _blnRaceTimelineFilled = true;
         _dvRace.RowFilter = _strDefaultFilterRace;
         this.tlpRaceTimeline.ShowActiveEventsOnly = true;
         this.tlpRaceTimeline.StartDate = new DateTime(DBLoader.Year, 1, 1);
         this.tlpRaceTimeline.EndDate = new DateTime(DBLoader.Year, 12, 31);
         foreach (DataRowView drvR in _dvRace)
         {
            if ((DateTime)drvR["StartDate"] > new DateTime(DBLoader.Year, 11, 1))
               break;
            Grid grdRace = new Grid();
            grdRace.Height = 10;
            grdRace.Margin = new Thickness(0, 0, 1, 0);
            grdRace.Name = "ID" + drvR["IDrace"].ToString();
            grdRace.ToolTip = drvR["gene_sz_race_name"].ToString();
            grdRace.SetValue(TimelinePanel.EventEndDateProperty, drvR["EndDate"]);
            grdRace.SetValue(TimelinePanel.EventStartDateProperty, drvR["StartDate"]);
            grdRace.SetValue(TimelinePanel.EventIsActiveProperty, (bool)drvR["Participation_Team"]);
            int intStageCount = Convert.ToInt16(drvR["HlpStageCount"].ToString());
            string[] strStage = drvR["HlpStageList"].ToString().Split(',');
            for (int i = 0; i < intStageCount; ++i)
            {
               ColumnDefinition colDef = new ColumnDefinition();
               grdRace.ColumnDefinitions.Add(colDef);
               Image img = new Image();
               try { img.Source = BitmapFrame.Create(new Uri(DBLoader.GetIconStage(drvR["HlpClassName"].ToString(), strStage[i], drvR["CONSTANT"].ToString()))); }
               catch { img.Source = BitmapFrame.Create(new Uri(DBLoader.GetIconStage("Undefined", "1", drvR["CONSTANT"].ToString()))); }
               img.StretchDirection = StretchDirection.Both;
               img.Stretch = Stretch.Fill;
               Grid.SetColumn(img, i);
               grdRace.Children.Add(img);
            }
            this.tlpRaceTimeline.Children.Add(grdRace);
         }
      }

      private void tlpRaceTimeline_Update()
      {
         string strSize = _strDefaultRaceTimeLineSize;
         if (this.cboRaceTimeLineSize.SelectedIndex > -1)
         {
            strSize = this.cboRaceTimeLineSize.SelectedValue.ToString();
            App.Current.Properties["CalendarSize"] = strSize;
         }
         DateTime d = (DateTime)_dvRace[(int)sbRaces.Value]["StartDate"];
         this.tlpRaceTimeline.StartDate = d;
         switch (strSize)
         {
            case "0":
               int intLastVisibleRace = (int)sbRaces.Value + (int)this.dgRace_Scrollbar.ViewportHeight;
               if (intLastVisibleRace < 0)
                  intLastVisibleRace = 0;
               else if (intLastVisibleRace >= _dvRace.Count)
                  intLastVisibleRace = _dvRace.Count - 1;
               d = ((DateTime)_dvRace[intLastVisibleRace]["StartDate"]).AddDays(1);
               break;
            default:
               d = d.AddMonths(Int16.Parse(strSize));
               break;
         }
         this.tlpRaceTimeline.EndDate = d;
      }


      private string ResizeString(string strText)
      {
         if (_tfDataGrid == null)
            _tfDataGrid = new Typeface(this.dgRace.FontFamily, this.dgRace.FontStyle, this.dgRace.FontWeight, this.dgRace.FontStretch);

         double dblWidth = 0;
         int intLength = strText.Length + 1;
         do
         {
            FormattedText ft = new FormattedText(strText.Substring(0, --intLength), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _tfDataGrid, this.dgRace.FontSize, Brushes.Black);
            dblWidth = ft.Width;
         } while (dblWidth > 77);
         if (intLength == strText.Length)
            return strText;
         return strText.Substring(0, intLength - 1) + "..";
      }


      #region Popup-Funktionen

      private void CyclistHeader_MouseDown(object sender, MouseButtonEventArgs e)
      {
         if (_intCyclistColIndexPrevious > -1)
            this.dgRace.Columns[_intCyclistColIndexPrevious].MaxWidth = 99;
         if (e.RightButton == MouseButtonState.Pressed)
            return; // Rechte Maustaste = Reset
         Grid g = (Grid)sender;
         _intPopupHelpIndex = int.Parse(g.Name.Split('X')[2]);
         _dvCyclist_Popup.RowFilter = "IDcyclist=" + g.Name.Split('X')[1];
         Int16 intCol = _intCyclistColIndex[int.Parse(g.Name.Split('X')[3])];
         this.dgRace.Columns[intCol].MaxWidth = 100;
         _intCyclistColIndexPrevious = intCol;
         
         if (e.ClickCount == 2)
         {
            this.lblPopupTitle.Content = string.Concat(_dvCyclist_Popup[0]["gene_sz_firstname"], " ", _dvCyclist_Popup[0]["gene_sz_lastname"]);
            if (this.dgOwnTeam.ItemsSource == null)
            {
               this.dgOwnTeam.ItemsSource = _dvCyclist_Popup;
               this.popCyclist.HorizontalOffset = 0;
               this.popCyclist.VerticalOffset = 0;
            }
            this.popCyclist.IsOpen = true;
            _blnPopupIsOpen = true;
            if ((bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
               this.dgRace_SetFilter();
         }
      }

      private void btnClose_Click(object sender, RoutedEventArgs e)
      {
         this.popCyclist.IsOpen = false;
         _blnPopupIsOpen = false;
         if ((bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
            this.dgRace_SetFilter();
      }

      private void popCyclist_PreviewMouseDown(object sender, MouseButtonEventArgs e)
      {
         if (_blnPopupIsOpen && e.LeftButton == MouseButtonState.Pressed)
         {
            _pntPopup.X = this.popCyclist.HorizontalOffset;
            _pntPopup.Y = this.popCyclist.VerticalOffset;
            _pntMouse = e.GetPosition(this);
            _blnPopupMove = true;
         }
      }

      private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
      {
         if (_blnPopupMove)
         {
            Point p = _pntPopup + (e.GetPosition(this) - _pntMouse);
            this.popCyclist.HorizontalOffset = p.X;
            this.popCyclist.VerticalOffset = p.Y;
         }
      }

      private void popCyclist_PreviewMouseUp(object sender, MouseButtonEventArgs e)
      {
         _blnPopupMove = false;
      }

      #endregion


      #region Popup-Details

      private void regLayoutSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         string strCategory = "";
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

   }
}
