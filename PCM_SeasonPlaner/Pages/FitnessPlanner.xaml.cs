using PCM_SeasonPlaner.Languages;
using Microsoft.Windows.Controls;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows;
using System.Data;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für FitnessPlanner.xaml
   /// </summary>
   public partial class FitnessPlanner : Page
   {
      private const int _intLevelHeight = 15;

      private string[] _strColumnSort = new string[43];

      private DataView _DYN_subsquad_config;
      private DataView _DYN_cyclist;


      public FitnessPlanner()
      {
         _strColumnSort[0] = "SortIndex ASC";
         InitializeComponent();
      }

      private void Page_Loaded(object sender, RoutedEventArgs e)
      {
         string strPageName = LanguageOptions.Text("Page_OwnTeam");
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + strPageName;
         this.Title = strPageName;
         // Die Liste der Fahrer
         this.dgOwnTeam_SetSorting();
         _DYN_cyclist = DBLoader.GetDataView("DYN_cyclist", true);
         this.dgOwnTeam.ItemsSource = _DYN_cyclist;
         this.dgOwnTeam_TranslateColumnHeaders();
         //
         _DYN_subsquad_config = DBLoader.GetDataView("DYN_subsquad_config", false);
         this.dgGroups.ItemsSource = _DYN_subsquad_config;
      }

      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
         this.dgOwnTeam.ItemsSource = null;
         this.dgGroups.ItemsSource = null;
         _DYN_subsquad_config.Dispose();
         _DYN_subsquad_config = null;
         _DYN_cyclist.Dispose();
         _DYN_cyclist = null;
      }

      public void SaveBeforeClosing()
      {
         this.dgOwnTeam_GetSorting();
      }


      private void dgOwnTeam_SetSorting()
      {
         foreach (string s in _strColumnSort)
            if (s != null)
            {
               string strColumn = s.Split(' ')[0];
               ListSortDirection sortDirection;
               switch (s.Split(' ')[1])
               {
                  case "DESC":
                     sortDirection = ListSortDirection.Descending;
                     break;
                  default:
                     sortDirection = ListSortDirection.Ascending;
                     break;
               }
               this.dgOwnTeam.Items.SortDescriptions.Add(new SortDescription(strColumn, sortDirection));
               foreach (DataGridColumn c in this.dgOwnTeam.Columns)
                  if (c.SortMemberPath == strColumn)
                     c.SortDirection = sortDirection;
            }
      }

      private void dgOwnTeam_GetSorting()
      {
         int intIndex = -1;
         foreach (DataGridColumn c in this.dgOwnTeam.Columns)
         {
            switch (c.SortDirection)
            {
               case ListSortDirection.Descending:
                  _strColumnSort[++intIndex] = c.SortMemberPath + " DESC";
                  break;
               case ListSortDirection.Ascending:
                  _strColumnSort[++intIndex] = c.SortMemberPath + " ASC";
                  break;
            }
         }
         ++intIndex;
         for (int i = intIndex; i < _strColumnSort.Length; ++i)
            _strColumnSort[i] = null;
      }


      private void btnAcceptOrder_Click(object sender, RoutedEventArgs e)
      {
         Int16 intIndex = 0;
         foreach (DataRowView drv in this.dgOwnTeam.Items)
            drv["SortIndex"] = intIndex++;
         this.dgOwnTeam.CommitEdit();
      }


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
               case "ManagerP1":
               case "ManagerP2":
               case "ManagerP3":
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


      private void cboSortGroup_Loaded(object sender, RoutedEventArgs e)
      {
         ((ComboBox)sender).ItemsSource = DBLoader.GetDataView("DYN_subsquad_config", false); 
      }

      private void cboManagerP_Loaded(object sender, RoutedEventArgs e)
      {
         DataView dv = DBLoader.GetDataView("STA_race", false);
         dv.Sort = "StartDate, gene_sz_race_name";
         ((ComboBox)sender).ItemsSource = dv;
      }
   }
}
