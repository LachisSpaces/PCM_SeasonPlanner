using PCM_SeasonPlaner.Languages;
using Infragistics.Windows.DataPresenter;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Data;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für TeamOverview.xaml
   /// </summary>
   public partial class OwnTeam : Page
   {
      private const int _intLevelHeight = 15;

      private int _intPCMVersion;
      private DataView _STA_type_rider;
      private DataView _DYN_cyclist;
      private DataView _STA_stage;
      private DataView _STA_race;
      private int _intIDcyclist = 0;
      private string _strCyclistName;
      private bool _blnFitnessProgramDirty = false;
      private DataRecord _drActiveRecord;
      private bool _blnSelectedInCode = false;
      private DoubleAnimation _animActive = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(.4)));
      private DoubleAnimation _animInactive = new DoubleAnimation();

      public OwnTeam()
      {
         _animActive.AutoReverse = true;
         _animActive.RepeatBehavior = RepeatBehavior.Forever;
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_OwnTeam");
         _intPCMVersion = DBLoader.PCMVersion;
         InitializeComponent();
         this.expFitnessProgram.IsExpanded = (bool)App.Current.Properties["FitnessProgramExpanded"];
         this.lblFitnessProgramHeader.Content = LanguageOptions.Text("Page_OwnTeam/FitnessProgram/HeaderLabel") + LanguageOptions.Text("Page_OwnTeam/FitnessProgram/HeaderLabelEmpty");
         DBLoader.FitnessProgram_GetFiles(this.cboFitnessProgramFileList, _intIDcyclist);
         _STA_type_rider = DBLoader.GetDataView("STA_type_rider", false);
         _STA_type_rider.RowFilter = "CONSTANT in ('tour', 'sprint', 'ardennaises', 'flandriennes')";
         _DYN_cyclist = DBLoader.GetDataView("DYN_cyclist", true);
         this.xdgOwnTeam.DataSource = _DYN_cyclist;
         _STA_race = DBLoader.GetDataView("STA_Race", false);
         _STA_race.Sort = "StartDate";
         _STA_stage = DBLoader.GetDataView("STA_Stage", false);
         _STA_stage.Sort = "gene_i_stage_number";
         this.tlpRaceTimeline.StartDate = new DateTime(2008, 1, 1);
         this.tlpRaceTimeline.EndDate = this.tlpRaceTimeline.StartDate.AddDays(7*43);
         this.tlpRaceTimeline.SeparatorTimeSpan = "Month";
         this.tlpFitnessProgram.StartDate = tlpRaceTimeline.StartDate;
         this.tlpFitnessProgram.EndDate = tlpRaceTimeline.EndDate;
         this.tlpFitnessProgram.SeparatorTimeSpan = "Month";
         this.tlpFitnessProgram.DisplayType = "Plan";
         DateTime dtmSeparator = this.tlpFitnessProgram.StartDate.AddDays(1);
         DateTime dtmEndDate = this.tlpFitnessProgram.EndDate;
         TimeSpan tsInterval = new TimeSpan(7, 0, 0, 0);
         //int intWeek = 0;
         while (dtmSeparator < dtmEndDate)
         {
            Rectangle r = new Rectangle();
            r.Height = _intLevelHeight;
            r.Fill = DBLoader.ColorTrainingLevel(1);
            //r.Name = string.Format("ID{0:00}", intWeek);
            r.SetValue(TimelinePanel.EventStartDateProperty, dtmSeparator);
            r.SetValue(TimelinePanel.EventEndDateProperty, dtmSeparator.AddDays(5));
            this.tlpFitnessProgram.Children.Add(r);
            dtmSeparator = dtmSeparator.Add(tsInterval);
         }
      }


      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
      }

      public void SaveBeforeClosing()
      {
         if (!this.tlpFitnessProgram_ChangesSaved())
            this.tlpFitnessProgram_Save("SavedWhileClosing");
         this.xdgOwnTeam.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges); // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         App.Current.Properties["FitnessProgramExpanded"] = this.expFitnessProgram.IsExpanded;
      }


      private void btnAcceptOrder_Click(object sender, RoutedEventArgs e)
      {
         Int16 intIndex = 0;
         Record[] l = this.xdgOwnTeam.GetRecordsInView(false);
         foreach (DataRecord dr in l)
            dr.Cells["SortIndex"].Value = intIndex++;
         this.xdgOwnTeam.ExecuteCommand(DataPresenterCommands.CommitChangesToAllRecords);
      }


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
               case "SortGroup":
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


      private void xdgOwnTeam_RecordActivated(object sender, Infragistics.Windows.DataPresenter.Events.RecordActivatedEventArgs e)
      {
         if (_blnSelectedInCode)
            return;
         DataRecord dr = this.xdgOwnTeam.ActiveRecord as DataRecord;
         if (dr != null)
         {
            if (!this.tlpFitnessProgram_ChangesSaved())
               return;
            _drActiveRecord = dr;
            _intIDcyclist = (Int16)dr.Cells["IDcyclist"].Value;
            _strCyclistName = string.Concat(dr.Cells["gene_sz_firstname"].Value.ToString().Substring(0, 1), "_", dr.Cells["gene_sz_lastname"].Value);
            //DBLoader.FitnessProgram_GetFiles(this.cboFitnessProgramFileList, _intIDcyclist);
            this.lblFitnessProgramHeader.Content = LanguageOptions.Text("Page_OwnTeam/FitnessProgram/HeaderLabel") + _strCyclistName.Replace("_",". ");
            this.tlpRaceTimeline.Children.Clear();
            _STA_race.RowFilter = string.Format("Participation_Cyclist{0:00}=true", (Int16)dr.Cells["HelpIndex"].Value);
            foreach (DataRowView drv in _STA_race)
            {
               Grid grdRace = new Grid();
               TextBlock txbRaceLabel = new TextBlock();
               if (string.IsNullOrEmpty(drv["SponsorObjectif"].ToString()))
               {
                  txbRaceLabel.ToolTip = drv["gene_sz_race_name"].ToString();
                  grdRace.ToolTip = drv["gene_sz_race_name"].ToString();
               }
               else
               {
                  grdRace.ToolTip = string.Concat(drv["gene_sz_race_name"].ToString(), " - ", drv["SponsorObjectif"].ToString());
                  txbRaceLabel.ToolTip = string.Concat(drv["gene_sz_race_name"].ToString(), " - ", drv["SponsorObjectif"].ToString());
                  txbRaceLabel.FontWeight = FontWeights.Bold;
               }
               grdRace.Height = 10;
               grdRace.Margin = new Thickness(0, 0, 1, 0);
               grdRace.Name = "ID" + drv["IDrace"].ToString();
               grdRace.ToolTip = drv["gene_sz_race_name"].ToString();
               grdRace.SetValue(TimelinePanel.EventEndDateProperty, drv["EndDate"]);
               grdRace.SetValue(TimelinePanel.EventStartDateProperty, drv["StartDate"]);
               grdRace.SetValue(TimelinePanel.EventIsActiveProperty, (bool)drv["Participation_Team"]);
               _STA_stage.RowFilter = "fkIDrace = " + drv["IDrace"];
               int i = 0;
               foreach (DataRowView drvS in _STA_stage)
               {
                  ColumnDefinition colDef = new ColumnDefinition();
                  grdRace.ColumnDefinitions.Add(colDef);
                  Image img = new Image();
                  img.Stretch = Stretch.Fill;
                  img.StretchDirection = StretchDirection.Both;
                  img.Source = BitmapFrame.Create(new Uri(DBLoader.GetIconStage(drv["fkIDnewUCI_class"].ToString(), "1", "1", false, drv["CONSTANT"].ToString())));
                  Grid.SetColumn(img, i++);
                  grdRace.Children.Add(img);
               }
               this.tlpRaceTimeline.Children.Add(grdRace);
               txbRaceLabel.Text = drv["gene_sz_race_name"].ToString();
               txbRaceLabel.RenderTransform = new RotateTransform(90, 5, 10);
               txbRaceLabel.Height = 150;
               txbRaceLabel.SetValue(TimelinePanel.IsLabelProperty, true);
               txbRaceLabel.SetValue(TimelinePanel.EventStartDateProperty, drv["StartDate"]);
               this.tlpRaceTimeline.Children.Add(txbRaceLabel);
            }
            string strFile = dr.Cells["FitnessProgramFile"].Value.ToString();
            _blnSelectedInCode = true;
            this.cboFitnessProgramFileList.SelectedValue = strFile;
            _blnSelectedInCode = false;
            this.tlpFitnessProgram_Load(strFile);
         }
      }


      private void cboFitnessProgramFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (!_blnSelectedInCode)
            this.tlpFitnessProgram_Load(this.cboFitnessProgramFileList.SelectedValue.ToString());
      }

      private void cmdFitnessProgramReset_Click(object sender, RoutedEventArgs e)
      {
         this.tlpFitnessProgram_Load(this.cboFitnessProgramFileList.SelectedValue.ToString());
      }

      private void cmdFitnessProgramValidate_Click(object sender, RoutedEventArgs e)
      {
         this.tlpFitnessProgram_IsValid();
      }

      private void cmdFitnessProgramFileSave_Click(object sender, RoutedEventArgs e)
      {
         this.tlpFitnessProgram_Save(this.txtFitnessProgramSaveAs.Text);
      }

      private void tlpFitnessProgram_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
      {
         int intDirection = 0;
         int intWeek = (int)(((e.GetPosition(this).X - 3) / this.tlpFitnessProgram.ActualWidth) * 43);
         switch (e.ChangedButton)
         {
            case MouseButton.Left:
               intDirection = 1;
               break;
            case MouseButton.Right:
               intDirection = -1;
               break;
         }
         this.tlpFitnessProgram_ChangeWeek(intWeek, intDirection);
      }

      private void tlpFitnessProgram_MouseWheel(object sender, MouseWheelEventArgs e)
      {
         int intDirection = Math.Sign(e.Delta);
         int intWeek = (int)(((e.GetPosition(this).X - 3) / this.tlpFitnessProgram.ActualWidth) * 43);
         this.tlpFitnessProgram_ChangeWeek(intWeek, intDirection);
      }

      private void tlpFitnessProgram_ChangeWeek(int intWeek, int intDirection)
      {
         this.tlpFitnessProgram_ChangeWeek(intWeek, intDirection, "", new List<int>());
      }
      private void tlpFitnessProgram_ChangeWeek(int intWeek, int intDirection, string strAction, List<int> intValue)
      {
         if (intWeek > 42 || intWeek < 0)
            return;
         _blnFitnessProgramDirty = true;
         Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[intWeek];
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
                     intNext = new List<int> { intLevel+1 };
                     strAction = "Increment";
                     break;
               }
               break;
         }
         r.BeginAnimation(OpacityProperty, _animInactive);
         r.Fill = DBLoader.ColorTrainingLevel(intLevel);
         r.Height = intLevel * _intLevelHeight;
         if (intPrevious.Count() > 0)
            this.tlpFitnessProgram_ChangeWeek(intWeek - 1, -1, strAction, intPrevious);
         if (intNext.Count() > 0)
            this.tlpFitnessProgram_ChangeWeek(intWeek + 1, 1, strAction, intNext);
      }

      private void tlpFitnessProgram_Load(string strFile)
      {
         if (_drActiveRecord == null) return;
         if (!this.tlpFitnessProgram_ChangesSaved()) return;
         this.txtFitnessProgramSaveAs.Text = strFile;//((DataRowView)this.cboFitnessProgramFileList.SelectedItem)["ProgramName"].ToString();
         _drActiveRecord.Cells["FitnessProgramFile"].Value = strFile;
         int[] intWeeks = DBLoader.FitnessProgram_Load(strFile);
         for (int i = 0; i < 43; ++i)
         {
            Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[i];
            r.Fill = DBLoader.ColorTrainingLevel(intWeeks[i]);
            r.BeginAnimation(OpacityProperty, _animInactive);
            r.Height = intWeeks[i] * _intLevelHeight;
         }
         _blnFitnessProgramDirty = false;
      }

      private void tlpFitnessProgram_Save(string strName)
      {
         if (_drActiveRecord == null) return;
         if (!this.tlpFitnessProgram_IsValid())
         {
            LanguageOptions.ShowMessage("Page_OwnTeam/FitnessProgram/ProgramInvalid", MessageBoxButton.OK);
            return;
         }
         if (string.IsNullOrEmpty(strName))
         {
            LanguageOptions.ShowMessage("Page_OwnTeam/FitnessProgram/InvalidName", MessageBoxButton.OK);
            return;
         }
         //string strFile = string.Concat(_intIDcyclist, "_", _strCyclistName, "_", Regex.Replace(strName, @"[?:\/*""<>|]_", ""), ".xml");
         string strFile = strName;
         if (!strFile.EndsWith(".xml"))
            strFile = strFile + ".xml";
         DBLoader.FitnessProgram_Save(strFile, tlpFitnessProgram_GetProgram());
         _drActiveRecord.Cells["FitnessProgramFile"].Value = strFile;
         _blnFitnessProgramDirty = false;
         //Refresh Combobox
         _blnSelectedInCode = true;
         DBLoader.FitnessProgram_GetFiles(this.cboFitnessProgramFileList, _intIDcyclist);
         this.cboFitnessProgramFileList.SelectedValue = strFile;
         _blnSelectedInCode = false;
      }

      private int[] tlpFitnessProgram_GetProgram()
      {
         int[] intWeeks = new int[43];
         for (int i = 0; i < 43; ++i)
         {
            Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[i];
            intWeeks[i] = (int)r.Height / _intLevelHeight;
         }
         return intWeeks;
      }

      private bool tlpFitnessProgram_ChangesSaved()
      {
         if (_blnFitnessProgramDirty)
         {
            switch (LanguageOptions.ShowMessage("Page_OwnTeam/FitnessProgram/SaveQuestion", MessageBoxButton.YesNo))
            {
               case MessageBoxResult.Yes:
                  _blnSelectedInCode = true;
                  this.xdgOwnTeam.ActiveRecord = _drActiveRecord;
                  _blnSelectedInCode = false;
                  return false;
               case MessageBoxResult.No:
                  _blnFitnessProgramDirty = false;
                  break;
            }
         }
         return true;
      }

      private bool tlpFitnessProgram_IsValid()
      {
         int[] intChecks = new int[] { 2, 2, 3, 3, 4, 4, 4, 5, 5, 5, 6, 5, 4, 3, 2 };
         int[] intWeeks = this.tlpFitnessProgram_GetProgram();
         bool blnValid = true;
         for (int i = 0; i < 43; ++i)
            this.tlpFitnessProgram.Children[i].BeginAnimation(OpacityProperty, _animInactive);
         // Zu intensiver Beginn?
         for (int i = 0; i < 11; ++i)
         {
            if (intWeeks[i] > intChecks[i])
            {
               Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[i];
               r.BeginAnimation(OpacityProperty, _animActive);
               blnValid = false;
            }
         }
         for (int intWeek = 0; intWeek < 43; ++intWeek)
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
                           Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[intCheck];
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
                        if (intCheck < 42 && intWeeks[++intCheck] < intChecks[s])
                        {
                           Rectangle r = (Rectangle)this.tlpFitnessProgram.Children[intCheck];
                           r.BeginAnimation(OpacityProperty, _animActive);
                           blnValid = false;
                        }
                     break;
                  }
            }
         return blnValid;
      }
   }
}
