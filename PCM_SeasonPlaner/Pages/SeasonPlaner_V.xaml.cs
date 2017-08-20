using Infragistics.Windows.DataPresenter;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Data;
using System;
using Languages;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Interaktionslogik für RaceList.xaml
   /// </summary>
   public partial class SeasonPlaner_V : Page
   {
      private DataView _STA_race;
      private string _strDefaultFilter = "fkIDnewUCI_class NOT IN (14,15,31,32,33)";


      public SeasonPlaner_V()
      {
         // Page_Unload wird nicht aufgerufen, falls User Applikation schliesst, z.B. via X
         Application.Current.MainWindow.Closing += new CancelEventHandler(Application_Closing);
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_SeasonPlaner");
         InitializeComponent();
         _STA_race = DBLoader.GetDataView("STA_Race", false);
         _STA_race.RowFilter = _strDefaultFilter;
         _STA_race.Sort = "StartDate";
         foreach (DataRowView drv in _STA_race)
         {
            Image imgRaceButton = new Image();
            imgRaceButton.Height = 10;
            imgRaceButton.MinWidth = 1;
            imgRaceButton.Stretch = Stretch.Fill;
            imgRaceButton.Margin = new Thickness(0, 0, 1, 0);
            imgRaceButton.StretchDirection = StretchDirection.Both;
            imgRaceButton.HorizontalAlignment = HorizontalAlignment.Center;
            imgRaceButton.VerticalAlignment = VerticalAlignment.Center;
            imgRaceButton.Source = BitmapFrame.Create(new Uri(DBLoader.UCIButton(drv["fkIDnewUCI_class"].ToString(), drv["gene_sz_race_name"].ToString())));
            imgRaceButton.Name = "ID" + drv["IDrace"].ToString();
            imgRaceButton.ToolTip = drv["gene_sz_race_name"].ToString();
            imgRaceButton.SetValue(TimelinePanel.EventEndDateProperty, drv["EndDate"]);
            imgRaceButton.SetValue(TimelinePanel.EventStartDateProperty, drv["StartDate"]);
            imgRaceButton.SetValue(TimelinePanel.EventIsActiveProperty, (bool)drv["Participation_Team"]);
            this.tlpRaceTimeline.StartDate = new DateTime(2008, 1, 1);
            this.tlpRaceTimeline.EndDate = new DateTime(2008, 12, 31);
            this.tlpRaceTimeline.ShowActiveEventsOnly = true;
            this.tlpRaceTimeline.Children.Add(imgRaceButton);
         }
         this.xdgRace.DataSource = _STA_race;
      }

      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         // Falls Page_Unload getriggert wurde, dann ist dieser Handler nicht mehr nötig 
         Application.Current.MainWindow.Closing -= new CancelEventHandler(Application_Closing);
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgRace.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }

      void Application_Closing(object sender, CancelEventArgs e)
      {
         //e.Cancel = true; // if you want to abort the closing
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgRace.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }


      private void ckbShowActiveEventsOnlyPlan_Click(object sender, RoutedEventArgs e)
      {
         string strFilter = _strDefaultFilter;
         if ((bool)this.ckbShowActiveEventsOnlyPlan.IsChecked)
            strFilter = string.Concat(strFilter, " AND Participation_Team=True");
         _STA_race.RowFilter = strFilter;
         _STA_race.Sort = "StartDate";
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
         DataView cyclists = DBLoader.GetDataView("DYN_cyclist", true);
         cyclists.Sort = "HelpIndex";
         for (int i = 0; i < cyclists.Count; ++i)
         {
            Field f = new Field();
            f.Name = string.Format("FavoriteRace_Cyclist{0:00}", cyclists[i]["HelpIndex"]);
            f.Settings.CellValuePresenterStyle = (Style)this.FindResource("styleFavoriteRace");
            f.Column = 6 + i;
            this.xdgRace.FieldLayouts[0].Fields.Add(f);
            f = new Field();
            f.Name = string.Format("Participation_Cyclist{0:00}", i);
            f.Label = cyclists[i]["gene_sz_lastname"];
            f.Settings.EditAsType = Type.GetType("System.Boolean");
            f.Column = 6 + i;
            this.xdgRace.FieldLayouts[0].Fields.Add(f);
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

      private void xdgRace_RecordsInViewChanged(object sender, Infragistics.Windows.DataPresenter.Events.RecordsInViewChangedEventArgs e)
      {
         Record[] l = this.xdgRace.GetRecordsInView(false);
         DateTime d = (DateTime)((DataRecord)l[0]).Cells["StartDate"].Value;
         this.tlpRaceTimeline.StartDate = d;
         this.tlpRaceTimeline.EndDate = d.AddMonths(1);
      }
      #endregion


      private void ckbTeam_Checked(object sender, RoutedEventArgs e)
      {
         this.EventParticipationChanged(true);
      }

      private void ckbTeam_Unchecked(object sender, RoutedEventArgs e)
      {
         this.EventParticipationChanged(false);
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
      }
   }
}
