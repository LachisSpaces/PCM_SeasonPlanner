using PCM_SeasonPlaner.Languages;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ioPath = System.IO.Path;
using System.Windows.Interop;
using System.ComponentModel;
using System.Windows.Data;
using Microsoft.Win32;
using System.Windows;
using System.Xml;
using System;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Interaktionslogik für MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private string _strInitialDirectoryFolder;
      private bool _blnOperationIsRunning = false;
      private Page _pagSeasonPlanner, _pagFitnessPlanner;

      public MainWindow()
      {
         XmlDocument doc = new XmlDocument();
         doc.Load(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
         XmlElement root = doc.DocumentElement;
         _strInitialDirectoryFolder = root.SelectSingleNode("./FolderSaveGames").InnerText;
         DBLoader.ApplicationFolder = ioPath.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
         App.Current.Properties["SuppressReadMe"] = root.SelectSingleNode("./SuppressReadMe").InnerText == "1";
         App.Current.Properties["CalendarSize"] = root.SelectSingleNode("./CalendarSize").InnerText;
         App.Current.Properties["FitnessPlanSize"] = root.SelectSingleNode("./FitnessPlanSize").InnerText;
         App.Current.Properties["NotifySelection"] = root.SelectSingleNode("./NotifySelection").InnerText == "1";
         App.Current.Properties["SelectionPlaceholder"] = root.SelectSingleNode("./SelectionPlaceholder").InnerText == "1";
         App.Current.Properties["RaceCalendarExpanded"] = root.SelectSingleNode("./RaceCalendarExpanded").InnerText == "1";
         App.Current.Properties["FitnessProgramExpanded"] = root.SelectSingleNode("./FitnessProgramExpanded").InnerText == "1";
         App.Current.Properties["ShowActiveEventsOnlyPlan"] = root.SelectSingleNode("./ShowActiveEventsOnlyPlan").InnerText == "1";
         App.Current.Properties["ShowActiveEventsOnlyCalendar"] = root.SelectSingleNode("./ShowActiveEventsOnlyCalendar").InnerText == "1";
         InitializeComponent();
         this.SetLanguage(null);
         this.Menu_SetAvailableLanguages();
         this.mnuOptionNotifySelection.IsChecked = (bool)App.Current.Properties["NotifySelection"];
         this.mnuOptionSelectionPlaceholder.IsChecked = (bool)App.Current.Properties["SelectionPlaceholder"];
         this.NavigateToStartpage();
      }

      private void Window_Closing(object sender, CancelEventArgs e)
      {
         switch (this.MainFrame.Content.ToString())
         {
            case "PCM_SeasonPlaner.Pages.FitnessPlanner":
               ((Pages.FitnessPlanner)this.MainFrame.Content).SaveBeforeClosing();
               break;
            case "PCM_SeasonPlaner.Pages.AdvancedPlanner":
               ((Pages.AdvancedPlanner)this.MainFrame.Content).SaveBeforeClosing();
               break;
         }
         XmlDocument doc = new XmlDocument();
         doc.Load(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
         XmlElement root = doc.DocumentElement;
         root.SelectSingleNode("./FolderSaveGames").InnerText = _strInitialDirectoryFolder;
         root.SelectSingleNode("./CalendarSize").InnerText = (string)App.Current.Properties["CalendarSize"];
         root.SelectSingleNode("./FitnessPlanSize").InnerText = (string)App.Current.Properties["FitnessPlanSize"];
         root.SelectSingleNode("./NotifySelection").InnerText = (bool)App.Current.Properties["NotifySelection"] ? "1" : "0";
         root.SelectSingleNode("./SelectionPlaceholder").InnerText = (bool)App.Current.Properties["SelectionPlaceholder"] ? "1" : "0";
         root.SelectSingleNode("./RaceCalendarExpanded").InnerText = (bool)App.Current.Properties["RaceCalendarExpanded"] ? "1" : "0";
         root.SelectSingleNode("./FitnessProgramExpanded").InnerText = (bool)App.Current.Properties["FitnessProgramExpanded"] ? "1" : "0";
         root.SelectSingleNode("./ShowActiveEventsOnlyPlan").InnerText = (bool)App.Current.Properties["ShowActiveEventsOnlyPlan"] ? "1" : "0";
         root.SelectSingleNode("./ShowActiveEventsOnlyCalendar").InnerText = (bool)App.Current.Properties["ShowActiveEventsOnlyCalendar"] ? "1" : "0";
         doc.Save(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
         e.Cancel = !this.SecurityCheckUnsavedData();
      }


      private void rbtStartPage_Click(object sender, RoutedEventArgs e)
      {
         this.NavigateToStartpage();
      }

      private void rbtOwnTeam_Click(object sender, RoutedEventArgs e)
      {
         if (!this.IsNavigationAllowed()) 
            return;
         if (_pagFitnessPlanner == null)
            _pagFitnessPlanner = new Pages.FitnessPlanner();
         this.MainFrame.Navigate(_pagFitnessPlanner);
      }

      private void rbtSeasonPlaner_Click(object sender, RoutedEventArgs e)
      {
         if (!this.IsNavigationAllowed())
            return;
         if (_pagSeasonPlanner == null)
            _pagSeasonPlanner = new Pages.AdvancedPlanner(); 
         this.MainFrame.Navigate(_pagSeasonPlanner);
      }

      private bool IsNavigationAllowed()
      {
         if (_blnOperationIsRunning)
         {
            LanguageOptions.ShowMessage("MainWindow/Messages/LoadingWaiting", MessageBoxButton.OK);
            return false;
         }
         else if (!DBLoader.DataLoaded)
         {
            LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
            return false;
         }
         return true;
      }

      private void NavigateToStartpage()
      {
         string strPage = "StartPage";
         if ((bool)App.Current.Properties["SuppressReadMe"])
            strPage += "Empty";
         else
            switch (LanguageOptions.SelectedLanguage)
            {
               case "german":
                  strPage += "German";
                  break;
            }
         Uri uri = new Uri("pack://application:,,,/Pages/" + strPage + ".xaml");
         try { this.MainFrame.Source = uri; }
         catch { }
      }

      public bool ShowForm(Window window, bool blnModal)
      {
         WindowInteropHelper helper = new WindowInteropHelper(window);
         helper.Owner = new WindowInteropHelper(Application.Current.MainWindow).Handle;
         window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
         if (blnModal)
         {
            bool? dialogResult = window.ShowDialog();
            return dialogResult.HasValue ? dialogResult.Value : false;
         }
         else
         {
            window.Show();
            return true;
         }
      }


      private void rbtPlanerNew_Click(object sender, RoutedEventArgs e)
      {
         this.NavigateToStartpage();
         if (this.SecurityCheckUnsavedData())
         {
            this.ResetInterface();
            string strPath = string.Empty;
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "*.cdb (cyanide database)|*.cdb";
            dlg.InitialDirectory = _strInitialDirectoryFolder;
            if ((bool)dlg.ShowDialog())
               strPath = dlg.FileName;
            if (!string.IsNullOrEmpty(strPath))
            {
               if (ioPath.GetFileName(strPath).Contains(" "))
                  LanguageOptions.ShowMessage("MainWindow/Messages/FileNameInvalid", MessageBoxButton.OK);
               else
               {
                  _strInitialDirectoryFolder = ioPath.GetDirectoryName(strPath);
                  this.StartLongJob("ImportDatabase", strPath);
               }
            }
         }
      }

      private void rbtDBExport_Click(object sender, RoutedEventArgs e)
      {
         this.NavigateToStartpage();
         if (!DBLoader.DataLoaded)
            LanguageOptions.ShowMessage("MainWindow/Messages/NoDataLoaded", MessageBoxButton.OK);
         else if (this.SecurityCheckUnsavedData())
         {
            string strPath = string.Empty;
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "*.cdb (cyanide database)|*.cdb";
            dlg.InitialDirectory = _strInitialDirectoryFolder;
            if ((bool)dlg.ShowDialog())
               strPath = dlg.FileName;
            if (!string.IsNullOrEmpty(strPath))
            {
               if (ioPath.GetFileName(strPath).Contains(" "))
                  LanguageOptions.ShowMessage("MainWindow/Messages/FileNameInvalid", MessageBoxButton.OK);
               else
               {
                  _strInitialDirectoryFolder = ioPath.GetDirectoryName(strPath);
                  this.StartLongJob("ExportDatabase", strPath);
               }
            }
         }
      }

      private void rbtPlanerOpen_Click(object sender, RoutedEventArgs e)
      {
         this.NavigateToStartpage();
         if (this.SecurityCheckUnsavedData())
            DBLoader.LoadSeasonPlan();
      }

      private void rbtPlanerSave_Click(object sender, RoutedEventArgs e)
      {
         DBLoader.SaveSeasonPlan();
      }

      private void rbtExit_Click(object sender, RoutedEventArgs e)
      {
         Application.Current.MainWindow.Close();
      }

      private bool SecurityCheckUnsavedData()
      {
         if (DBLoader.DataLoaded)
         {
            switch (LanguageOptions.ShowMessage("MainWindow/Messages/SecurityCheckUnsavedData", MessageBoxButton.YesNoCancel))
            {
               case MessageBoxResult.Yes:
                  DBLoader.SaveSeasonPlan();
                  return true;
               case MessageBoxResult.No:
                  return true;
               case MessageBoxResult.Cancel:
                  return false;
            }
         }
         return true;
      }

      private void ResetInterface()
      { 
         _pagSeasonPlanner = null;
         _pagFitnessPlanner = null;
      }


      private void Menu_SetAvailableLanguages()
      {
         foreach (string s in LanguageOptions.AvailableLanguages)
         {
            MenuItem mi = new MenuItem();
            mi.Click += new RoutedEventHandler(this.rbtLanguage_OnClick);
            //            mi.IsChecked = (s == LanguageOptions.SelectedLanguage);
            mi.Header = s;
            mi.Tag = s;
            try
            {
               Image img = new Image();
               img.Width = 32; img.Height = 32;
               img.Source = new BitmapImage(new Uri(string.Concat(LanguageOptions.LanguageFolder, s, ".jpg")));
               mi.Icon = img;
            }
            catch { }
            this.mnuLanguage.Items.Add(mi);
         }
      }

      public void rbtLanguage_OnClick(object sender, RoutedEventArgs e)
      {
         this.SetLanguage(((MenuItem)sender).Tag.ToString());
         this.NavigateToStartpage();
      }

      private void SetLanguage(string strLanguage)
      {
         if (strLanguage != null)
            LanguageOptions.SelectedLanguage = strLanguage;
         ((XmlDataProvider)(this.FindResource("Lang"))).Document = LanguageOptions.XmlLanguage;
      }


      private void rcbOptionNotifySelection_Click(object sender, RoutedEventArgs e)
      {
         App.Current.Properties["NotifySelection"] = ((MenuItem)sender).IsChecked;
      }

      private void rcbOptionSelectionPlaceholder_Click(object sender, RoutedEventArgs e)
      {
         App.Current.Properties["SelectionPlaceholder"] = ((MenuItem)sender).IsChecked;
      }


      private void StartLongJob(string strJob, string strPath)
      {
         _blnOperationIsRunning = true;
         ProgressWindow w = new ProgressWindow();
         w.StartLongJob(strJob, strPath);
         _blnOperationIsRunning = false;
         w = null;
      }

      private void MenuItem_Click(object sender, RoutedEventArgs e)
      {

      }

   }
}
