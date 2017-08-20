using PCM_SeasonPlaner.Languages;
using Infragistics.Windows.DataPresenter;
using System.Windows.Controls;
using System.Windows;
using System.Data;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für CyclistOverview.xaml
   /// </summary>
   public partial class CyclistOverview : Page
   {
      DataView _DYN_cyclist;

      public CyclistOverview()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_CyclistOverview");
         InitializeComponent();
         _DYN_cyclist = DBLoader.GetDataView("DYN_cyclist", false);
         this.xdgCyclist.DataSource = _DYN_cyclist;
      }

      private void xdgCyclist_FieldLayoutInitialized(object sender, Infragistics.Windows.DataPresenter.Events.FieldLayoutInitializedEventArgs e)
      {
         foreach (Field f in this.xdgCyclist.FieldLayouts[0].Fields)
         {
            string s = LanguageOptions.Text(string.Concat("DYN_cyclist/", f.Name));
            if (!string.IsNullOrEmpty(s))
               f.Label = s;
         }
      }

   }
}
