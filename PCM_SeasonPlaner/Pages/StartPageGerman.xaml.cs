using PCM_SeasonPlaner.Languages;
using System.Windows.Controls;
using System.Windows;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für StartPage.xaml
   /// </summary>
   public partial class StartPageGerman : Page
   {
      public StartPageGerman()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_StartPage");
         InitializeComponent();
      }
   }
}
