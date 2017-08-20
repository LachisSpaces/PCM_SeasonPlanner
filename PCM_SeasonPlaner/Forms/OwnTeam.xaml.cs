using Infragistics.Windows.DataPresenter;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows;
using System.Linq;
using System.Data;
using System;
using Languages;

namespace PCM_SeasonPlaner
{
   /// <summary>
   /// Interaktionslogik für OwnTeam.xaml
   /// </summary>
   public partial class OwnTeam : Window
   {
      DataView _DYN_cyclist;

      public OwnTeam()
      {
         // Page_Unload wird nicht aufgerufen, falls User Applikation schliesst, z.B. via X
         Application.Current.MainWindow.Closing += new CancelEventHandler(Application_Closing);
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_OwnTeam");
         InitializeComponent();
         _DYN_cyclist = DBLoader.GetDataView("DYN_cyclist", true);
         this.xdgOwnTeam.DataSource = _DYN_cyclist;
         //OwnTeam/List/ID
      }


      private void Window_Unloaded(object sender, RoutedEventArgs e)
      {
         // Falls Page_Unload getriggert wurde, dann ist dieser Handler nicht mehr nötig 
         Application.Current.MainWindow.Closing -= new CancelEventHandler(Application_Closing);
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgOwnTeam.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }

      void Application_Closing(object sender, CancelEventArgs e)
      {
         //e.Cancel = true; // if you want to abort the closing
         // Speichern (falls Focus noch auf Feld der letzten Änderung war)
         this.xdgOwnTeam.ExecuteCommand(DataPresenterCommands.EndEditModeAndAcceptChanges);
      }


      private void xdgOwnTeam_InitializeRecord(object sender, Infragistics.Windows.DataPresenter.Events.InitializeRecordEventArgs e)
      {
         if (e.Record is DataRecord)
         {
            DataRecord dr = (DataRecord)e.Record;
            int[] intCharac = new int[5];
            intCharac[0] = int.Parse(dr.Cells["charac_f_timetrial"].Value.ToString());
            intCharac[1] = int.Parse(dr.Cells["charac_f_mountain"].Value.ToString());
            intCharac[2] = int.Parse(dr.Cells["charac_f_sprint"].Value.ToString());
            intCharac[3] = int.Parse(dr.Cells["charac_f_cobble"].Value.ToString());
            intCharac[4] = int.Parse(dr.Cells["charac_f_hill"].Value.ToString());
            Array.Sort(intCharac);
            intCharac[0] = int.Parse(dr.Cells["charac_f_endurance"].Value.ToString());
            intCharac[1] = int.Parse(dr.Cells["charac_f_resistance"].Value.ToString());
            intCharac[2] = int.Parse(dr.Cells["charac_f_recuperation"].Value.ToString());
            dr.Cells["AVG_charac"].Value = (Decimal)intCharac.Average();
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


      private void btnAcceptOrder_Click(object sender, RoutedEventArgs e)
      {
         Int16 intIndex = 0;
         Record[] l = this.xdgOwnTeam.GetRecordsInView(false);
         foreach (DataRecord dr in l)
            dr.Cells["SortIndex"].Value = intIndex++;
         this.xdgOwnTeam.ExecuteCommand(DataPresenterCommands.CommitChangesToAllRecords);
      }


      private void regLayoutSelection_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
      {
         //         MessageBox.Show("wheel");
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
               case "SortIndex":
               case "gene_sz_lastname":
               case "gene_sz_firstname":
                  break;
               default:
                  f.Visibility = DBLoader.FieldIsVisible(string.Concat("Page_OwnTeam/", strCategory, "/", f.Name));
                  break;
            }
      }
   }
}
