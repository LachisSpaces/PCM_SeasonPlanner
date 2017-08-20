using PCM_SeasonPlaner.Languages;
using Infragistics.Windows.DataPresenter;
using System.Windows.Controls;
using System.Windows;
using System.Data;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für TeamOverview.xaml
   /// </summary>
   public partial class TeamOverview : Page
   {
      DataView _DYN_team;

      public TeamOverview()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_TeamOverview");
         InitializeComponent();
         _DYN_team = DBLoader.GetDataView("DYN_team", false);
         this.xdgTeam.DataSource = _DYN_team;
      }

      private void xdgTeam_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgTeam.FieldLayouts[0].Fields)
         {
            string s = LanguageOptions.Text(string.Concat("DYN_team/", f.Name));
            if (!string.IsNullOrEmpty(s))
               f.Label = s;
         }
      }
   }
}
