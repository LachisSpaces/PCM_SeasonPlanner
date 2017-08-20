using PCM_SeasonPlaner.Languages;
using Infragistics.Windows.DataPresenter;
using System.Windows.Controls;
using System.Windows;
using System.Data;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für RaceOverview.xaml
   /// </summary>
   public partial class RaceOverview : Page
   {
      DataView _STA_race;

      public RaceOverview()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_RaceOverview");
         InitializeComponent();
         _STA_race = DBLoader.GetDataView("STA_Race", false);
         this.xdgRace.DataSource = _STA_race;
      }

      private void xdgRace_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgRace.FieldLayouts[0].Fields)
         {
            string s = LanguageOptions.Text(string.Concat("STA_Race/", f.Name));
            if (!string.IsNullOrEmpty(s))
               f.Label = s;
         }
         //foreach (Field f in this.xdgRace.FieldLayouts[1].Fields)
         //{
         //   string s = LanguageOptions.Text(string.Concat("STA_stage/", f.Name));
         //   if (!string.IsNullOrEmpty(s))
         //      f.Label = s;
         //}
      }

   }
}
