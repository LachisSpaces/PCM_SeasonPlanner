using PCM_SeasonPlaner.Languages;
using System.Windows.Controls;
using System.Windows;
using System.Data;
using System.Xml;
using System.IO;
using System;

namespace PCM_SeasonPlaner.Pages
{
   /// <summary>
   /// Interaktionslogik für SponsorScenario.xaml
   /// </summary>
   public partial class SponsorScenario : Page
   {
      private String _strScenarioFolder, _strSelectedScenario;
      private DataView _DYN_team, _DYN_manager, _DYN_sponsor;
      private int _intBudgetActual;
      private XmlNode _xRoot;


      public SponsorScenario()
      {
         _strScenarioFolder = string.Concat(DBLoader.ApplicationFolder, "SponsorScenarios\\");
         InitializeComponent();
      }

      private void Page_Loaded(object sender, RoutedEventArgs e)
      {
         // Funktioniert nur bei PCM2011 oder später
         if (DBLoader.PCMVersion < 11)
         {
            LanguageOptions.ShowMessage("Page_SponsorScenario/PCMVersionNotSupported", MessageBoxButton.OK);
            return;
         }
         //
         string strPageName = LanguageOptions.Text("Page_OwnTeam");
         Application.Current.MainWindow.Title = LanguageOptions.Text("MainWindow") + " - " + strPageName;
         this.Title = strPageName;
         //
         FindAvailableScenarios();
         //
         _DYN_manager = DBLoader.GetDataView("DYN_manager", false);
         _DYN_team = DBLoader.GetDataView("DYN_team", false);
         _DYN_team.RowFilter = "IDteam = " + DBLoader.TeamID;
         int intIDSponsor = int.Parse(_DYN_team[0]["fkIDsponsor_principal"].ToString());
         _DYN_sponsor = DBLoader.GetDataView("DYN_sponsor", false);
         _DYN_sponsor.RowFilter = "IDsponsor=" + intIDSponsor;
      }

      private void Page_Unloaded(object sender, RoutedEventArgs e)
      {
         this.SaveBeforeClosing();
         this.ResetScenario();
         _DYN_manager.Dispose();
         _DYN_sponsor.Dispose();
         _DYN_sponsor = null;
         _DYN_manager = null;
         _DYN_team.Dispose();
         _DYN_team = null;
      }

      public void SaveBeforeClosing()
      {
      }

      private void ResetScenario()
      {
         _xRoot = null;
         _strSelectedScenario = null;
         this.DisplayValues(0,0, 0, 0, 0, 0, false, false);
      }
      

      private void lboSelectScenario_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         if (this.lboSelectScenario.SelectedItem == null) return;
         // 
         this.SelectedScenario = ((FileInfo)this.lboSelectScenario.SelectedItem).Name;
         this.CalculateValues();
      }


      private void btnApply_Click(object sender, RoutedEventArgs e)
      {
         if (this.SelectedScenario == null)
         {
            LanguageOptions.ShowMessage("Page_SponsorScenario/NoScenarioSelected", MessageBoxButton.OK);
            return;
         }
         if (MessageBoxResult.Yes == LanguageOptions.ShowMessage("Page_SponsorScenario/ConfirmApplyScenario", MessageBoxButton.YesNo))
         {
            // Sponsor anpassen
            _DYN_sponsor[0]["gene_i_contract_end"] = this.txtContractEndSuggested.Text;
            _DYN_sponsor[0]["value_b_cosponsoring"] = this.ckbSecondaryNext.IsChecked;
            // Zukünftiges Budget des Hauptsponsors anpassen
            int intBudgetFuture = int.Parse(_DYN_team[0]["value_i_sponsor_future"].ToString());
            //int intTeamId = intBudgetFuture >> 16;
            //MessageBox.Show(intTeamId.ToString());
            int intBudget = (int.Parse(this.txtBudgetNext.Text) * 12) - _intBudgetActual;
            _DYN_team[0]["value_i_sponsor_future"] = intBudgetFuture + (intBudget / 1000);
            this.ResetScenario();
            this.txtDescriptionFull.Text = LanguageOptions.Text("Page_SponsorScenario/ApplyScenarioSuccess");
         }
      }


      private String SelectedScenario
      {
         set
         {
            if (_strSelectedScenario == value)
               return;
            _strSelectedScenario = value;
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(string.Concat(_strScenarioFolder, _strSelectedScenario));
            _xRoot = xdoc.SelectSingleNode(string.Concat("/", Const.SettingsTopNode));
         }
         get
         {
            return _strSelectedScenario;
         }
      }

      private void CalculateValues()
      {
         // Aktuelle Werte erfassen und berechnen
         _intBudgetActual = int.Parse(_DYN_sponsor[0]["value_i_budget"].ToString());
         int intFinanSoldeActual = int.Parse(_DYN_manager[0]["finan_i_solde"].ToString());
         int intContractEndActual = int.Parse(_DYN_sponsor[0]["gene_i_contract_end"].ToString());
         bool blnSecondaryActual = (bool)_DYN_sponsor[0]["value_b_cosponsoring"];
         int intBudgetActual = _intBudgetActual / 12;
         // Szenario Werte erfassen und berechnen 
         int intFinanSoldeSuggested = intFinanSoldeActual + this.GetValue("OneTimePayment");
         int intContractEndSuggested = intContractEndActual + this.GetValue("ContractDurationIncrease");
         bool blnSecondaryNext = ("YES" == this.GetText("AllowSecondarySponsor").Trim().ToUpper());
         int intBudgetNext = intBudgetActual + this.GetValue("MonthlyBudgetIncrease");
         //
         this.DisplayValues(intBudgetActual, intBudgetNext, intFinanSoldeActual, intFinanSoldeSuggested, intContractEndActual, intContractEndSuggested, blnSecondaryActual, blnSecondaryNext);
      }

      private void DisplayValues(int intBudgetActual, int intBudgetNext, int intFinanSoldeActual, int intFinanSoldeSuggested, int intContractEndActual, int intContractEndSuggested, bool blnSecondaryActual, bool blnSecondaryNext)
      {
         this.txtFinanSoldeActual.Text = intFinanSoldeActual.ToString();
         this.txtFinanSoldeSuggested.Text = intFinanSoldeSuggested.ToString();
         this.txtContractEndSuggested.Text = intContractEndSuggested.ToString();
         this.txtContractEndActual.Text = intContractEndActual.ToString();
         this.txtDescriptionShort.Text = this.GetText("DescriptionShort");
         this.txtDescriptionFull.Text = this.GetText("DescriptionFull");
         this.txtBudgetActual.Text = intBudgetActual.ToString();
         this.txtBudgetNext.Text = intBudgetNext.ToString();
         this.ckbSecondaryActual.IsChecked = blnSecondaryActual;
         this.ckbSecondaryNext.IsChecked = blnSecondaryNext;
      }

      public String GetText(string strXPath)
      {
         string strText = "";
         try { strText = _xRoot.SelectSingleNode(string.Concat(strXPath, "/", LanguageOptions.SelectedLanguage)).InnerText.ToString(); }
         catch
         {
            try { strText = _xRoot.SelectSingleNode(string.Concat(strXPath, "/english")).InnerText.ToString(); }
            catch
            {
               try { strText = _xRoot.SelectSingleNode(strXPath).InnerText.ToString(); }
               catch { }
            }
         }
         return strText.Trim();
      }

      public int GetValue(string strXPath)
      {
         try { return int.Parse(_xRoot.SelectSingleNode(strXPath).InnerText); }
         catch { return 0; }
      }


      private void FindAvailableScenarios()
      {
         this.lboSelectScenario.Items.Clear();
         DirectoryInfo di = new DirectoryInfo(_strScenarioFolder);
         FileInfo[] files = di.GetFiles("*.xml");
         for (int i = 0; i < files.Length; i++)
            this.lboSelectScenario.Items.Add(files[i]);
      }
   }
}
