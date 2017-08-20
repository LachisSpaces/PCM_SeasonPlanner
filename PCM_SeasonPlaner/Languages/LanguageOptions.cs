using System.Collections.Generic;
using PCM_SeasonPlaner;
using System.Windows;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System;

namespace PCM_SeasonPlaner.Languages
{
   public static class LanguageOptions
   {
      private static String _strLanguageFolder;
      private static String _strSelectedLanguage;
      private static String[] _strAvailableLanguages;
      private static XmlDocument _xdocLanguage;
      private static XmlNode _xRoot;


      static LanguageOptions()
      {
         _strLanguageFolder = string.Concat(DBLoader.ApplicationFolder, "Languages\\");
         FindAvailableLanguages();
      }


      public static String Text(string strXPath)
      {
         try { return _xRoot.SelectSingleNode(strXPath).Attributes["Text"].InnerText.ToString(); }
         catch { return ""; }
      }

      public static MessageBoxResult ShowMessage(string strXPath, MessageBoxButton buttons)
      {
         XmlNode n = _xRoot.SelectSingleNode(strXPath);
         string m, t;
         try   { m = _xRoot.SelectSingleNode(strXPath).Attributes["Text"].InnerText.ToString(); }
         catch { return MessageBoxResult.Cancel; }
         try   { t = _xRoot.SelectSingleNode(strXPath).Attributes["Title"].InnerText.ToString(); }
         catch { t = string.Empty; }
         return MessageBox.Show(m.Replace("\\r\\n", Environment.NewLine), t, buttons);
      }

      public static MessageBoxResult ShowMessage(string strXPath, string strText, MessageBoxButton buttons)
      {
         XmlNode n = _xRoot.SelectSingleNode(strXPath);
         string m, t;
         try { m = _xRoot.SelectSingleNode(strXPath).Attributes["Text"].InnerText.ToString(); }
         catch { return MessageBoxResult.Cancel; }
         try { t = _xRoot.SelectSingleNode(strXPath).Attributes["Title"].InnerText.ToString(); }
         catch { t = string.Empty; }
         return MessageBox.Show(m.Replace("\\r\\n", Environment.NewLine).Replace("[PLACEHOLDER]", strText), t, buttons);
      }


      public static String SelectedLanguage
      {
         set
         {
            _strSelectedLanguage = value;
            XmlDocument doc = new XmlDocument();
            doc.Load(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
            XmlElement root = doc.DocumentElement;
            XmlNode node = root.SelectSingleNode("./Language");
            node.InnerText = _strSelectedLanguage;
            doc.Save(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
         }
         get
         {
            if (_strSelectedLanguage == null)
            {
               XmlDocument doc = new XmlDocument();
               doc.Load(DBLoader.ApplicationFolder + Const.ApplicationOptionFile);
               XmlElement root = doc.DocumentElement;
               _strSelectedLanguage = root.SelectSingleNode("./Language").InnerText;
            }
            return _strSelectedLanguage;
         }
      }


      public static XmlDocument XmlLanguage
      { 
         get 
         {
            _xdocLanguage = new XmlDocument();
            _xdocLanguage.Load(string.Concat(_strLanguageFolder, SelectedLanguage, ".xml"));
            _xRoot = _xdocLanguage.SelectSingleNode(string.Concat("/", Const.SettingsTopNode));
            return _xdocLanguage;
         }
      }


      public static String[] AvailableLanguages
      {
         get { return _strAvailableLanguages; }
      }

      public static string LanguageFolder
      {
         get { return _strLanguageFolder; }
      }


      private static void FindAvailableLanguages()
      {
         DirectoryInfo di = new DirectoryInfo(_strLanguageFolder);
         FileInfo[] files = di.GetFiles("*.xml");
         _strAvailableLanguages = new String[files.Length];
         for (int i = 0; i < files.Length; i++)
         {
            _strAvailableLanguages[i] = files[i].Name;
            _strAvailableLanguages[i] = _strAvailableLanguages[i].Substring(0, _strAvailableLanguages[i].IndexOf(".xml"));
         }
      }

   }
}
