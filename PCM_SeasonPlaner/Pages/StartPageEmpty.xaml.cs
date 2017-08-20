using PCM_SeasonPlaner.Languages;
using System.Windows.Controls;
using System.Windows;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für StartPage.xaml
   /// </summary>
   public partial class StartPageEmpty : Page
   {
      public StartPageEmpty()
      {
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + LanguageOptions.Text("Page_StartPage");
         InitializeComponent();
      }
   }
}
