using PCM_SeasonPlaner.Languages;
using Infragistics.Windows.DataPresenter;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Data;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für RaceList.xaml
   /// </summary>
   public partial class SeasonPlaner_H : Page
   {
      private DataView _STA_race;
      private DataView _DYN_cyclist;
      private DataView _cyclistDetails;
      private DataView _STA_type_rider;
      private int _intPCMVersion;
      private int _intPopupHelpIndex;
      private Typeface _tfDataGrid;
      private Point _pntMouse, _pntPopup;
      private bool _blnPopupMove = false;
      private bool _blnPopupIsOpen = false;
      private string _strDefaultRaceTimeLineSize;
      private string _strDefaultFilter = "fkIDnewUCI_class NOT IN (14,15,31,32,33)";

      public SeasonPlaner_H()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_SeasonPlaner");
         _intPCMVersion = DBLoader.PCMVersion;
         InitializeComponent();
         _tfDataGrid = new Typeface(this.xdgRace.FontFamily, this.xdgRace.FontStyle, this.xdgRace.FontWeight, this.xdgRace.FontStretch);
         // Einstellungen setzten
         _strDefaultRaceTimeLineSize = (string)App.Current.Properties["RaceTimeLineSize"];
         this.expRaceCalendar.IsExpanded = (bool)App.Current.Properties["RaceCalendarExpanded"];
         this.ckbShowActiveEventsOnlyCalendar.IsChecked = (bool)App.Current.Properties["ShowActiveEventsOnlyCalendar"];
         this.ckbShowActiveEventsOnlyPlan.IsChecked = (bool)App.Current.Properties["ShowActiveEventsOnlyPlan"];
         // Race-Kalender mit Events füllen
         _STA_race = DBLoader.GetDataView("STA_Race", false);
         _STA_race.RowFilter = _strDefaultFilter;
         _STA_race.Sort = "StartDate";
         DataView dvStage = DBLoader.GetDataView("STA_Stage", false);
         dvStage.Sort = "gene_i_stage_number";
         this.tlpRaceTimeline.ShowActiveEventsOnly = true;
         this.tlpRaceTimeline.StartDate = new DateTime(2008, 1, 1);
         this.tlpRaceTimeline.EndDate = new DateTime(2008, 12, 31);
         foreach (DataRowView drv in _STA_race)
         {
            if ((DateTime)drv["StartDate"] > new DateTime(2008, 11, 1))
               break;
            Grid grdRace = new Grid();
            grdRace.Height = 10;
            grdRace.Margin = new Thickness(0, 0, 1, 0);
            grdRace.Name = "ID" + drv["IDrace"].ToString();
            grdRace.ToolTip = drv["gene_sz_race_name"].ToString();
            grdRace.SetValue(TimelinePanel.EventEndDateProperty, drv["EndDate"]);
            grdRace.SetValue(TimelinePanel.EventStartDateProperty, drv["StartDate"]);
            grdRace.SetValue(TimelinePanel.EventIsActiveProperty, (bool)drv["Participation_Team"]);
            dvStage.RowFilter = "fkIDrace = " + drv["IDrace"];
            int i = 0;
            foreach (DataRowView drvS in dvStage)
            {
               ColumnDefinition colDef = new ColumnDefinition();
               grdRace.ColumnDefinitions.Add(colDef);
               Image img = new Image();
               img.Stretch = Stretch.Fill;
               img.StretchDirection = StretchDirection.Both;
               img.Source = BitmapFrame.Create(new Uri(DBLoader.GetIconStage(drv["fkIDnewUCI_class"].ToString(), drvS["fkIDstage_type"].ToString(), drvS["fkIDrelief"].ToString(), (bool)drvS["gene_b_HasCobble"], drv["CONSTANT"].ToString())));
               Grid.SetColumn(img, i++);
               grdRace.Children.Add(img);
            }
            this.tlpRaceTimeline.Children.Add(grdRace);
         }
         // Source für Datagrids setzen
         this.xdgRace_SetFilter(); // Filter setzen, da ..OnlyPlan nicht automatisch funktioniert (nur Userinteraktion)
         this.xdgRace.DataSource = _STA_race;
         this.xdgRaceCyclists.DataSource = _STA_race;
         // Source für Popup
         _STA_type_rider = DBLoader.GetDataView("STA_type_rider", false);
         _STA_type_rider.RowFilter = "CONSTANT in ('tour', 'sprint', 'ardennaises', 'flandriennes')";
         _cyclistDetails = DBLoader.GetDataView("DYN_cyclist", false);
         _DYN_cyclist = DBLoader.GetDataView("DYN_cyclist", true);
      }

      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
      }

      public void SaveBeforeClosing()
      {
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgRace.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
         this.xdgRaceCyclists.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
         App.Current.Properties["RaceCalendarExpanded"] = this.expRaceCalendar.IsExpanded;
         App.Current.Properties["ShowActiveEventsOnlyPlan"] = this.ckbShowActiveEventsOnlyPlan.IsChecked;
         App.Current.Properties["ShowActiveEventsOnlyCalendar"] = this.ckbShowActiveEventsOnlyCalendar.IsChecked;
      }


      #region Race Datagrid

      private void xdgRace_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgRace.FieldLayouts[0].Fields)
         {
            string s = LanguageOptions.Text(string.Concat("STA_race/", f.Name));
            if (!string.IsNullOrEmpty(s))
               f.Label = s;
         }
      }

      private void xdgRace_InitializeRecord(object sender, Infragistics.Windows.DataPresenter.Events.InitializeRecordEventArgs e)
      {
         if (e.Record is DataRecord)
         {
            DataRecord dr = (DataRecord)e.Record;
            dr.Cells["CyclistsAllowed"].Value = string.Concat(dr.Cells["gene_i_min_cyclist_by_team"].Value, " / ", dr.Cells["gene_i_max_cyclist_by_team"].Value);
         }
      }

      private void xdgRaceCyclists_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         int intDatGridRow = 0;
         int intGroup = Const.MaxGroups;
         Type typBoolean = Type.GetType("System.Boolean");
         Style styLine = (Style)this.FindResource("styleLine");
         Style styFavRace = (Style)this.FindResource("styleFavoriteRace");
         Style styGroupLabel = (Style)this.FindResource("styleLabelPresenterGroup");
         Style styGroupField = (Style)this.FindResource("styleGroup");
         _DYN_cyclist.Sort = "SortGroup, SortIndex, gene_sz_lastname";
         for (int i = 0; i < _DYN_cyclist.Count; ++i)
         {
            Field f;
            UnboundField u;
            if (intGroup != (Int16)_DYN_cyclist[i]["SortGroup"])
            {
               intGroup = (Int16)_DYN_cyclist[i]["SortGroup"];
               u = new UnboundField();
               u.Row = intDatGridRow;
               u.Settings.CellValuePresenterStyle = styLine;
               if (intGroup == Const.MaxGroups)
               {
                  u.Label = LanguageOptions.Text("Page_SeasonPlaner/CyclistGrouping/NoGroup");
                  u.Settings.LabelPresenterStyle = styGroupLabel;
                  u.Settings.CellValuePresenterStyle = styGroupField;
               }
               this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(u);
               if (intGroup < Const.MaxGroups)
               {
                  u = new UnboundField();
                  u.Row = intDatGridRow;
                  u.Settings.CellValuePresenterStyle = styGroupField;
                  this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(u);
                  f = new Field();
                  f.Row = intDatGridRow;
                  f.Settings.EditAsType = typBoolean;
                  f.Settings.LabelPresenterStyle = styGroupLabel;
                  f.Label = LanguageOptions.Text("Page_SeasonPlaner/CyclistGrouping/Groups") + (intGroup + 1);
                  f.Settings.LabelClickAction = LabelClickAction.SelectField;
                  f.Name = string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + intGroup);
                  this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(f);
               }
               intDatGridRow += 1;
            }
            u = new UnboundField();
            u.Row = intDatGridRow;
            u.Settings.CellValuePresenterStyle = styLine;
            this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(u);
            f = new Field();
            f.Row = intDatGridRow;
            f.Settings.CellValuePresenterStyle = styFavRace;
            f.Label = string.Format("{0:00}", _DYN_cyclist[i]["RaceDaysPlanned"]).PadLeft(26);
            f.Name = string.Format("FavoriteRace_Cyclist{0:00}", _DYN_cyclist[i]["HelpIndex"]);
            this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(f);
            f = new Field();
            f.Row = intDatGridRow;
            f.Settings.EditAsType = typBoolean;
            f.Settings.LabelClickAction = LabelClickAction.SelectField;
            string s = ResizeString(_DYN_cyclist[i]["gene_sz_lastname"].ToString());
            f.Name = string.Format("Participation_Cyclist{0:00}", _DYN_cyclist[i]["HelpIndex"]);
            f.Label = string.Concat(s.PadRight(30), ",", _DYN_cyclist[i]["IDcyclist"], ",", _DYN_cyclist[i]["HelpIndex"]);
            this.xdgRaceCyclists.FieldLayouts[0].Fields.Add(f);
            intDatGridRow += 1;
         }
      }

      private string ResizeString(string strText) 
      {
         double dblWidth = 0;
         int intLength = strText.Length + 1;
         do
         {
            FormattedText ft = new FormattedText(strText.Substring(0, --intLength), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _tfDataGrid, this.xdgRace.FontSize, Brushes.Black);
            dblWidth = ft.Width;
         } while (dblWidth > 77);
         if (intLength == strText.Length)
            return strText;
         return strText.Substring(0, intLength-1) + "..";
      }

      private void xdgRaceCyclists_RecordsInViewChanged(object sender, Infragistics.Windows.DataPresenter.Events.RecordsInViewChangedEventArgs e)
      {
         this.xdgRace.ScrollInfo.SetHorizontalOffset(this.xdgRaceCyclists.ScrollInfo.HorizontalOffset);
         this.UpdateRaceTimeLine();
      }

      private void xdgRaceCyclists_CellChanged(object sender, Infragistics.Windows.DataPresenter.Events.CellChangedEventArgs e)
      {
         this.xdgRaceCyclists.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
         DBLoader.CalculatePlannedRacedays((Int16)e.Cell.Record.Cells["IDrace"].Value, (bool)e.Cell.Value, (string)e.Cell.Field.Name);
         this.xdgRaceCyclists_DisplayPlannedRacedays();
      }

      private void xdgRaceCyclists_DisplayPlannedRacedays()
      {
         for (int i = 0; i < _DYN_cyclist.Count; ++i)
            this.xdgRaceCyclists.FieldLayouts[0].Fields[string.Format("FavoriteRace_Cyclist{0:00}", _DYN_cyclist[i]["HelpIndex"])].Label = string.Format("{0:00}", _DYN_cyclist[i]["RaceDaysPlanned"]).PadLeft(26);
      }
      

      private void ckbShowActiveEventsOnlyPlan_Click(object sender, RoutedEventArgs e)
      {
         this.xdgRace_SetFilter();
      }

      private void ckbShowActiveEventsOnlyCyclist_Click(object sender, RoutedEventArgs e)
      {
         this.xdgRace_SetFilter();
      }

      private void xdgRace_SetFilter()
      {
         string strFilter = _strDefaultFilter;
         if ((bool)this.ckbShowActiveEventsOnlyPlan.IsChecked)
            strFilter = string.Concat(strFilter, " AND Participation_Team=True");
         if (_blnPopupIsOpen && (bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
            strFilter = string.Concat(strFilter, " AND ", string.Format("Participation_Cyclist{0:00}=true", _intPopupHelpIndex));
         if (strFilter != _strDefaultFilter)
            strFilter = '(' + strFilter + ") OR gene_i_min_cyclist_by_team=-1";
         _STA_race.RowFilter = strFilter;
         _STA_race.Sort = "StartDate";
      }


      private void cboRaceTimeLineSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         this.UpdateRaceTimeLine();
      }

      private void UpdateRaceTimeLine()
      {
         string strSize = _strDefaultRaceTimeLineSize;
         if (this.cboRaceTimeLineSize.SelectedIndex > -1)
         { 
            strSize = this.cboRaceTimeLineSize.SelectedValue.ToString();
            App.Current.Properties["RaceTimeLineSize"] = strSize;
         }
         Record[] l = this.xdgRaceCyclists.GetRecordsInView(false);
         DateTime d = (DateTime)((DataRecord)l[0]).Cells["StartDate"].Value;
         this.tlpRaceTimeline.StartDate = d;
         switch (strSize)
         {
            case "0":
               d = ((DateTime)((DataRecord)l[l.Length - 1]).Cells["StartDate"].Value).AddDays(1);
               break;
            default:
               d = d.AddMonths(Int16.Parse(strSize));
               break;
         }
         this.tlpRaceTimeline.EndDate = d;
      }

      #endregion


      #region Team-Participation

      private void ckbTeam_Click(object sender, RoutedEventArgs e)
      {
         this.EventParticipationChanged((bool)((CheckBox)sender).IsChecked);
      }

      private void EventParticipationChanged(bool blnParticipate)
      {
         if (this.xdgRace.ActiveRecord == null) return;
         string strID = ((DataRecord)this.xdgRace.ActiveRecord).Cells["IDrace"].Value.ToString();
         foreach (UIElement child in this.tlpRaceTimeline.Children)
         {
            string strChildID = child.GetValue(NameProperty).ToString().Substring(2);
            if (strChildID == strID)
            {
               child.SetValue(TimelinePanel.EventIsActiveProperty, blnParticipate);
               break; ;
            }
         }
         this.tlpRaceTimeline.Measure(new Size(this.ActualWidth, this.ActualHeight));
         DBLoader.CalculatePlannedRacedays();
         this.xdgRaceCyclists_DisplayPlannedRacedays();
      }

      #endregion


      #region Popup-Funktionen

      private void Label_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         Label lbl = (Label)sender;
         _intPopupHelpIndex = int.Parse(lbl.Content.ToString().Split(',')[2]);
         _cyclistDetails.RowFilter = "IDcyclist=" + lbl.Content.ToString().Split(',')[1];
         this.lblPopupTitle.Content = string.Concat(_cyclistDetails[0]["gene_sz_firstname"], " ", _cyclistDetails[0]["gene_sz_lastname"]);//lbl.Content.ToString().Split(',')[0].Trim();
         if (this.xdgOwnTeam.DataSource == null)
         {
            this.xdgOwnTeam.DataSource = _cyclistDetails;
            this.popCyclist.HorizontalOffset = 0;
            this.popCyclist.VerticalOffset = 0;
         }
         this.popCyclist.IsOpen = true;
         _blnPopupIsOpen = true;
         if ((bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
            this.xdgRace_SetFilter();
      }

      private void btnClose_Click(object sender, RoutedEventArgs e)
      {
         this.popCyclist.IsOpen = false;
         _blnPopupIsOpen = false;
         if ((bool)this.ckbShowActiveEventsOnlyCyclist.IsChecked)
            this.xdgRace_SetFilter();
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
         foreach (Field f in this.xdgOwnTeam.FieldLayouts[0].Fields)
            switch (f.Name)
            {
               case "SortIndex":
               case "gene_sz_lastname":
               case "gene_sz_firstname":
                  break;
               default:
                  f.Visibility = DBLoader.FieldIsVisible(string.Concat("Page_OwnTeam/", strCategory, "/", f.Name));
                  break;
            }
      }

      private void xdgOwnTeam_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgOwnTeam.FieldLayouts[0].Fields)
         {
            string s = LanguageOptions.Text(string.Concat("DYN_cyclist/", f.Name));
            if (!string.IsNullOrEmpty(s))
               f.Label = s;
         }
      }

      #endregion

   }
}
