using PCM_SeasonPlaner.Languages;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Globalization;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Text;
using System.Data;
using System.Linq;
using System.Xml;
using System.IO;
using System;

namespace PCM_SeasonPlaner
{
   public class DBLoader : IProgressOperation
   {
      //public static SolidColorBrush cIsFavoriteRace = new SolidColorBrush(Color.FromArgb(127, 127, 255, 127));
      public static SolidColorBrush cIsFavoriteRace = new SolidColorBrush(Color.FromArgb(127, 127, 195, 255));
      public static SolidColorBrush cIsNotFavoriteRace = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

      //public static SolidColorBrush cManagement_Carac_VeryBad = new SolidColorBrush(Color.FromArgb(000, 127, 255, 127));
      //public static SolidColorBrush cManagement_Carac_Bad = new SolidColorBrush(Color.FromArgb(127, 195, 255, 195));
      //public static SolidColorBrush cManagement_Carac_Good = new SolidColorBrush(Color.FromArgb(127, 127, 255, 127));
      //public static SolidColorBrush cManagement_Carac_VeryGood = new SolidColorBrush(Color.FromArgb(127, 063, 195, 063));
      //public static SolidColorBrush cManagement_Carac_Huge = new SolidColorBrush(Color.FromArgb(127, 000, 127, 000));
      public static SolidColorBrush cManagement_Carac_Huge     = new SolidColorBrush(Color.FromArgb(180, 000, 100, 200));
      public static SolidColorBrush cManagement_Carac_VeryGood = new SolidColorBrush(Color.FromArgb(130, 000, 100, 200));
      public static SolidColorBrush cManagement_Carac_Good     = new SolidColorBrush(Color.FromArgb(080, 000, 100, 200));
      public static SolidColorBrush cManagement_Carac_Bad      = new SolidColorBrush(Color.FromArgb(040, 000, 100, 200));
      public static SolidColorBrush cManagement_Carac_VeryBad  = new SolidColorBrush(Color.FromArgb(010, 000, 100, 200));

      public static SolidColorBrush cManagement_Level1 = new SolidColorBrush(Color.FromRgb(214, 097, 014));
      public static SolidColorBrush cManagement_Level2 = new SolidColorBrush(Color.FromRgb(215, 165, 015));
      public static SolidColorBrush cManagement_Level3 = new SolidColorBrush(Color.FromRgb(216, 233, 016));
      public static SolidColorBrush cManagement_Level4 = new SolidColorBrush(Color.FromRgb(149, 233, 016));
      public static SolidColorBrush cManagement_Level5 = new SolidColorBrush(Color.FromRgb(083, 233, 016));
      public static SolidColorBrush cManagement_Level6 = new SolidColorBrush(Color.FromRgb(015, 232, 015));

      private int _intProgressMaximum;
      private int _intProgressCurrent;
      private string _strProgressStatus;
      private bool _blnProgressCancelRequested = false;

      private static int _intYear;
      private static int _intIDteam;
      private static DataSet _dsCyanideDB;
      private static bool _blnDataLoaded = false;
      private static int _intPCMVersion = -1;

      private static string _strApplicationFolder;
      private static string _strFitnessProgramFolder;
      private static string _strUCIButtonFolder;
      private static string _strSaveFolder;
      //private static bool _blnUseGrouping;
      private static string _strCDBPath;

      private static XmlNode _xListCagegories;
      
      private static NumberFormatInfo _nfiFloat = new NumberFormatInfo();


      public DBLoader()
      {
         _intProgressMaximum = 0;
         _intProgressCurrent = 0;
         _blnProgressCancelRequested = false;
         _nfiFloat.NumberDecimalSeparator = ".";
         _nfiFloat.NumberGroupSeparator = ",";

      }

      public static bool DataLoaded
      {
         get { return _blnDataLoaded; } 
      }

      public static int PCMVersion
      {
         get { return _intPCMVersion; }
      }

      public static int Year
      {
         get { return _intYear; }
      }

      public static int TeamID
      {
         get { return _intIDteam; }
      }

      public static string ApplicationFolder
      {
         get { return _strApplicationFolder; }
         set 
         {
            _strFitnessProgramFolder = value + "\\FitnessSchedules\\";
            _strUCIButtonFolder = value + "\\UCI_Buttons\\";
            _strApplicationFolder = value + "\\";
            _strSaveFolder = value + "\\Data\\";
            XmlDocument xdocCategories = new XmlDocument();
            xdocCategories.Load(string.Concat(_strApplicationFolder, Const.ListCategoriesFileName));
            _xListCagegories = xdocCategories.SelectSingleNode(string.Concat("/", Const.SettingsTopNode));
         }
      }

      public static SolidColorBrush ColorTrainingLevel(int intLevel)
      {
         switch (intLevel)
         {
            case 2:
               return cManagement_Level2;
            case 3:
               return cManagement_Level3;
            case 4:
               return cManagement_Level4;
            case 5:
               return cManagement_Level5;
            case 6:
               return cManagement_Level6;
            default:
               return cManagement_Level1;
         }
      }

      public static Visibility FieldIsVisible(string strXPath)
      {
         try
         {
            string s = _xListCagegories.SelectSingleNode(strXPath).Name;
            return Visibility.Visible;
         }
         catch
         {
            return Visibility.Collapsed;
         }
      }

      public static DataView GetDataView(string strTableName, bool blnUseTeamFilter)
      {
         DataView dv = new DataView(_dsCyanideDB.Tables[strTableName]);
         if (blnUseTeamFilter)
            dv.RowFilter = string.Format("fkIDteam={0}", _intIDteam);
         return dv;
      }

      public static string PathStageImage(string strType)
      {
         return string.Concat(_strUCIButtonFolder, strType, ".png");
      }


      public static void LoadSeasonPlan()
      {
         // Prüfen, ob DB Informationen noch vorhanden sind
         string strPathSettings = string.Concat(_strSaveFolder, Const.SettingsFileName);
         if (!File.Exists(strPathSettings))
         {
            LanguageOptions.ShowMessage("DBLoader/LoadPlanNoData", MessageBoxButton.OK); 
            return;
         }
         // DB Informationen einlesen
         XmlDocument xdocSettings = new XmlDocument();
         xdocSettings.Load(strPathSettings);
         XmlNode xnRoot = xdocSettings.SelectSingleNode(string.Concat("/", Const.SettingsTopNode));
         _strCDBPath = xnRoot.SelectSingleNode(Const.SettingsLastCDBPath).InnerText;
         XmlNode xnTables = xnRoot.SelectSingleNode(Const.SettingsTables);
         int intNumTables = int.Parse(xnTables.Attributes[Const.db_NumTables].Value);
         DBTable[] dbtTables = new DBTable[intNumTables];
         intNumTables = -1;
         foreach (XmlNode xnTable in xnTables.ChildNodes)
         {
            intNumTables++;
            string strTableName = xnTable.InnerText;
            dbtTables[intNumTables] = new DBTable(strTableName);
            dbtTables[intNumTables].ReadXmlSchema(string.Concat(_strSaveFolder, strTableName, ".xsd"));
            dbtTables[intNumTables].ReadXml(string.Concat(_strSaveFolder, strTableName, ".xml"));
         }
         if (BuildDataSet(dbtTables, false))
         {
            CalculateParticipatingCyclists(0);
            _blnDataLoaded = true;
         }
      }

      public static void SaveSeasonPlan()
      {
         if (!_blnDataLoaded) return;
         // Datenbank-Informationen und Daten speichern
         int intNumTables = 0;
         XmlDocument xdocSettings = new XmlDocument();
         XmlNode xRoot = xdocSettings.CreateElement(Const.SettingsTopNode);
         XmlNode xTables = xdocSettings.CreateElement(Const.SettingsTables);
         //Alle Tabellen speichern
         foreach (DBTable t in _dsCyanideDB.Tables)
         {
            if (t != null)
            {
               intNumTables++;
               t.WriteXml(string.Concat(_strSaveFolder, t.TableName, ".xml"));
               t.WriteXmlSchema(string.Concat(_strSaveFolder, t.TableName, ".xsd"));
               xTables.AppendChild(xdocSettings.CreateElement(Const.SettingsTable)).InnerText = t.TableName;
            }
         }
         // Anzahl der Tabellen speichern
         xTables.Attributes.Append(xdocSettings.CreateAttribute(Const.db_NumTables)).InnerText = intNumTables.ToString();
         xRoot.AppendChild(xTables);
         // Speicherort der zuletzt geladenen DB
         xRoot.AppendChild(xdocSettings.CreateElement(Const.SettingsLastCDBPath)).InnerText = _strCDBPath;
         // Speichern in Settings.xml
         xdocSettings.AppendChild(xRoot);
         xdocSettings.Save(string.Concat(_strSaveFolder, Const.SettingsFileName));
      }



      private void ImportDatabase(object sender, DoWorkEventArgs e)
      {
         // Export aus CDB
         int intProgressStep = 2;
         int intProgressCurrent = 20;
         this.ProgressMaximum = 1000000;
         this.ProgressStatus = "Extracting data";
         this.ProgressCurrent = intProgressCurrent;
         string strExportPath = string.Concat(_strSaveFolder, Const.DumpOutFileName);
         ProcessStartInfo pci = new ProcessStartInfo(string.Concat(_strApplicationFolder, Const.ExporterApplication));
         pci.Arguments = string.Format(" -input \"{0}\" -output \"{1}\" -ToXML", _strCDBPath, strExportPath);
         pci.WindowStyle = ProcessWindowStyle.Hidden;//hide console 
         Process proc = Process.Start(pci);
         while (!proc.HasExited)
         {
            this.ProgressCurrent = intProgressCurrent++;
            if (intProgressCurrent > 9000000)
               _blnProgressCancelRequested = true;
         }

         //Daten laden
         this.ProgressStatus = "Reading data";
         intProgressStep = 1000000 - intProgressCurrent;
         if (intProgressStep < 0)
            intProgressStep = 0;

         StringBuilder strData = new StringBuilder(100);
         XmlDocument xdocSource = new XmlDocument();
         xdocSource.Load(strExportPath);
         //testen, ob das File korrekt ist
         XmlNodeList xnlAllNodes = xdocSource.SelectNodes(Const.db);
         if (xnlAllNodes.Count != 1)
         {
            if (xnlAllNodes.Count == 0)
               MessageBox.Show("Wrong file");
            return;
         }
         //Basic-Infos laden
         XmlNode xDatabase = xnlAllNodes.Item(0);
         int intNumOrigTables = int.Parse(xDatabase.Attributes[Const.db_NumOTables].Value);
         int intNumTables = int.Parse(xDatabase.Attributes[Const.db_NumTables].Value);
         intProgressStep = intProgressStep / intNumTables;
         //Tabellen anlegen
         DBTable[] dbtTables;
         dbtTables = new DBTable[intNumTables];
         //Loop über alle Tabellen
         int intTableIndex = -1;
         foreach (XmlNode xnTable in xDatabase.ChildNodes)
         {
            string strTableName = xnTable.Attributes[Const.table_name].Value;
            if (((IList)Const.ImportTables).Contains(strTableName))
            {
               intTableIndex++;
               this.ProgressStatus = string.Concat("Reading ", strTableName);
               int intTableId = int.Parse(xnTable.Attributes[Const.table_id].Value);
               int intNumRows = int.Parse(xnTable.Attributes[Const.table_NumRows].Value);
               int intNumCols = int.Parse(xnTable.Attributes[Const.table_NumCols].Value);
               int intNumOrigCols = int.Parse(xnTable.Attributes[Const.table_NumOCols].Value);
               int intNumColsHlp = intNumCols;
               // Hilfspalten (oder -zeilen) noch dazuzählen
               switch (strTableName)
               {
                  case "STA_race":
                     intNumColsHlp += 102; // 2 für Datum (Sortierung) + 51 für Teilnahme (2 Team + 9 Gruppen + 40 Fahrer) + 40 für Wunschrennen + 2 für Sponsorziele + 1 für Stage-Info
                     break;
                  case "DYN_cyclist":
                     intNumColsHlp += 25; // Infos für Querverweis auf Teilnahme-Feld in STA_race (Participation_Cyclist{0} > Index: 0-30) + Sortierung der Fahrer im Race-Planer + diverse Hilfsfelder, welche im voraus berechnete Daten enthalten
                     break;
                  case "DYN_subsquad_config":
                     intNumColsHlp += 1; 
                     break;
                  case "STA_country":
                     intNumColsHlp += 1; 
                     intNumRows += 1;
                     break;
               }
               // Arrays für Spalten und Werte
               string[,] strCells = new String[intNumRows, intNumCols];
               DBColumn[] dbcColumns = new DBColumn[intNumColsHlp];
               //preprocess the columns, to know the DB structure 
               int intColIndex = -1;
               foreach (XmlNode xnColumn in xnTable.ChildNodes)
               {
                  intColIndex++;
                  string strDataType = xnColumn.Attributes[Const.column_type].Value;
                  string strColumnName = xnColumn.Attributes[Const.column_name].Value;
                  int intColumnId = int.Parse(xnColumn.Attributes[Const.column_ID].Value);
                  dbcColumns[intColIndex] = new DBColumn(strColumnName, strDataType);
                  int intRowIndex = -1;
                  switch (strDataType)
                  {
                     case "ListInt":
                     case "ListFloat":
                        foreach (XmlNode xnList in xnColumn.ChildNodes)
                        {
                           intRowIndex++;
                           if (int.Parse(xnList.Attributes[Const.list_size].Value) == 0)
                              strCells[intRowIndex, intColIndex] = "()";
                           else
                           {
                              strData.Length = 0;
                              foreach (XmlNode xItem in xnList.ChildNodes)
                                 strData.Append("," + xItem.InnerText);
                              if (strData.Length > 0)
                                 strCells[intRowIndex, intColIndex] = '(' + strData.Remove(0, 1).ToString() + ')';
                              else
                                 strCells[intRowIndex, intColIndex] = "()";
                           }
                        }
                        break;
                     default:
                        foreach (XmlNode xnCell in xnColumn.ChildNodes)
                           strCells[++intRowIndex, intColIndex] = xnCell.InnerText;
                        break;
                  }
               }
               // Hilfsspalten hinzufügen 
               switch (strTableName)
               {
                  case "STA_race":
                     dbcColumns[++intColIndex] = new DBColumn("EndDate", "Date");
                     dbcColumns[++intColIndex] = new DBColumn("StartDate", "Date");
                     dbcColumns[++intColIndex] = new DBColumn("SponsorObjectif", "String");
                     dbcColumns[++intColIndex] = new DBColumn("PicturePathSponsorObjectif", "String");
                     dbcColumns[++intColIndex] = new DBColumn("Participation_Allowed", "String");
                     dbcColumns[++intColIndex] = new DBColumn("Participation_Count", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Participation_Team", "Bool");
                     dbcColumns[++intColIndex] = new DBColumn("HlpClassColor", "String");
                     dbcColumns[++intColIndex] = new DBColumn("HlpClassName", "String");
                     dbcColumns[++intColIndex] = new DBColumn("HlpMinRiders", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("HlpMaxRiders", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("HlpMaxTeams", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("HlpTypeTour", "String");
                     for (int i = 0; i < Const.MaxCyclist; i++)
                     {
                        dbcColumns[++intColIndex] = new DBColumn(string.Format("Participation_Cyclist{0:00}", i), "Bool");
                        dbcColumns[++intColIndex] = new DBColumn(string.Format("FavoriteRace_Cyclist{0:00}", i), "Int16");
                     }
                     for (int i = 0; i < Const.MaxGroups; i++)
                        dbcColumns[++intColIndex] = new DBColumn(string.Format("Participation_Cyclist{0:00}", Const.MaxCyclist + i), "Bool");
                     break;
                  case "DYN_cyclist":
                     dbcColumns[++intColIndex] = new DBColumn("HelpIndex","Int16");
                     dbcColumns[++intColIndex] = new DBColumn("SortIndex", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("RiderType", "String");
                     dbcColumns[++intColIndex] = new DBColumn("BackFromInjury", "String");
                     dbcColumns[++intColIndex] = new DBColumn("FitnessProgramList", "String");
                     dbcColumns[++intColIndex] = new DBColumn("TrainingType", "String");
                     dbcColumns[++intColIndex] = new DBColumn("RaceDaysPlanned", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("SortGroup", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("RiderAge", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_plain", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_mountain", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_hill", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_downhilling", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_timetrial", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_endurance", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_resistance", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_recuperation", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_cobble", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_baroudeur", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_sprint", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_acceleration", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("Charac_prologue", "Int16");
                     dbcColumns[++intColIndex] = new DBColumn("ManagerP1", "String");
                     dbcColumns[++intColIndex] = new DBColumn("ManagerP2", "String");
                     dbcColumns[++intColIndex] = new DBColumn("ManagerP3", "String");
                     break;
                  case "DYN_subsquad_config":
                     dbcColumns[++intColIndex] = new DBColumn("SortGroup", "Int16");
                     break;
                  case "STA_country":
                     dbcColumns[++intColIndex] = new DBColumn("fkIDteam", "Int16");
                     int intRowIndex = strCells.GetLength(0) - 1;
                     int intColMax = strCells.GetLength(1);
                     strCells[intRowIndex, 0] = "-1";
                     strCells[intRowIndex, 1] = "---";
                     for (int i = 2; i < intColMax; i++)
                        strCells[intRowIndex, i] = "0";
                     break;
               }
               // Tabelle mit allen Daten erstellen 
               dbtTables[intTableIndex] = new DBTable(strTableName, dbcColumns, strCells, intTableId, intNumOrigCols, intNumCols, intNumRows);
               intProgressCurrent += intProgressStep; this.ProgressCurrent = intProgressCurrent;
            }
         }

         if (BuildDataSet(dbtTables, true))
         {
            FillHelpColumns();
            _blnDataLoaded = true;
         }
      }


      private static bool BuildDataSet(DBTable[] dbtTables, bool blnIsNewImport)
      {
         // Alle geladenen Tabellen ins Dataset übernehmen
         _dsCyanideDB = new DataSet();
         foreach (DBTable t in dbtTables)
            if (t != null)
               _dsCyanideDB.Tables.Add(t);
         // Prüfen, welche Datenbank geöffnet wurde 
         _intPCMVersion = -1;
         try
         {  // Tabelle existiert erst in PCM2017
            _dsCyanideDB.Tables["DYN_cyclist_training_plan"].Columns["IDcyclist_training_plan"].ToString();
            _intPCMVersion = 17;
            if (blnIsNewImport)
            {
               try
               {
                  DBTable t = new DBTable("DYN_subsquad_config");
                  t.ReadXmlSchema(string.Concat(ApplicationFolder, "DYN_subsquad_config.xsd"));
                  t.ReadXml(string.Concat(ApplicationFolder, "DYN_subsquad_config.xml"));
                  _dsCyanideDB.Tables.Add(t);
               }
               catch { }
            }
         }
         catch
         {
            try
            {  // Spalte existiert erst in PCM2016
               _dsCyanideDB.Tables["DYN_cyclist"].Columns["fkIDleader_target"].ToString();
               _intPCMVersion = 16;
            }
            catch
            {
               try
               {  // Spalte existiert erst in PCM2015
                  _dsCyanideDB.Tables["DYN_cyclist"].Columns["fkIDworkplan"].ToString();
                  _intPCMVersion = 15;
               }
               catch
               {
                  try
                  {  // Tabelle existiert erst in PCM2013
                     _dsCyanideDB.Tables["DYN_cyclist_objective"].Columns["IDcyclist_objective"].ToString();
                     _intPCMVersion = 13;
                  }
                  catch
                  {
                     LanguageOptions.ShowMessage("DBLoader/OldGame", MessageBoxButton.OK);
                     return false;
                  }
               }
            }
         }
         // Team vom Spieler auslesen
         try 
         {
            DataView dv = new DataView(_dsCyanideDB.Tables["GAM_user"]);
            if (_intPCMVersion >= 16)
               dv.RowFilter = "fkIDteam_duplicate > 0";
            else
               dv.RowFilter = "gene_b_host=true";
            _intIDteam = int.Parse(dv[0]["fkIDteam_duplicate"].ToString());
         }
         catch
         {
            LanguageOptions.ShowMessage("DBLoader/NoTeamIDfound", MessageBoxButton.OK);
            return false;
         }
         // Jahr der Saison auslesen
         try
         {
            DataView dv = new DataView(_dsCyanideDB.Tables["GAM_config"]);
            _intYear = UnixTimestampToDateTime(Convert.ToInt64(dv[0]["gene_i_date"].ToString())).Year;
         }
         catch
         {
            LanguageOptions.ShowMessage("DBLoader/NoYearfound", MessageBoxButton.OK);
            return false;
         }
         return true;
      }


      private void FillHelpColumns()
      {
         // Informationen zu den einzelnen Rennen aufbereiten

         int intProgressCurrent = 0;
         this.ProgressCurrent = 0;
         this.ProgressMaximum = 400;
         this.ProgressStatus = "Prepare data";

         DataView dvRace = GetDataView("STA_race", false);
         DataView dvTeam = GetDataView("DYN_team", false);
         DataView dvStage = GetDataView("STA_Stage", false);
         DataView dvCountry = GetDataView("STA_country", false);
         DataView dvDivision = GetDataView("STA_division", false);
         DataView dvUciClass = GetDataView("STA_UCI_class", false);
         DataView dvTypeTour = GetDataView("STA_type_tour", false);
         DataView dvTeamRace = GetDataView("DYN_team_race", false);

         Int32 intSubscribed = 0;
         DataView dvInvitationState = GetDataView("STA_invitation_state", false);
         dvInvitationState.RowFilter = "CONSTANT='SUBSCRIBED'";
         if (dvInvitationState.Count > 0)
            intSubscribed = (Int32)dvInvitationState[0]["IDinvitation_state"]; 

         // Infos zum Team suchen
         dvTeam.RowFilter = string.Format("IDteam={0}", _intIDteam);
         Int32 intTeamCountry = (Int32)dvTeam[0]["fkIDcountry"];
         dvDivision.RowFilter = string.Concat("IDdivision=", dvTeam[0]["fkIDdivision"]);
         string strTeamDivision = dvDivision[0]["CONSTANT"].ToString();

         // Loop über alle Rennen
         dvStage.Sort = "gene_i_stage_number";
         foreach (DataRowView drvRace in dvRace)
         {
            Int16 intMaxTeams = 22;
            Int16 intMinRiders = 1;
            Int16 intMaxRiders = 9;
            string strClassColor = "FFFFFF";
            string strClassName = "Undefined";
            string strParticipationAllowed = "ERROR";
            Int32 intRaceCountry = (Int32)drvRace["fkIDcountry"];
            dvUciClass.RowFilter = string.Concat("IDUCI_class=", drvRace["fkIDUCI_class"]);
            if (dvUciClass.Count > 0)
            {
               strClassName = dvUciClass[0]["CONSTANT"].ToString();
               // Anzahl erlaubter Teams + Fahrer pro Team
               intMaxTeams = (Int16)dvUciClass[0]["gene_i_max_team"];
               intMinRiders = (Int16)dvUciClass[0]["gene_i_min_riders"];
               intMaxRiders = (Int16)dvUciClass[0]["gene_i_max_riders"];
               // UCI-Klasse für Filter + Farbe der Balken
               switch (strClassName)
               {
                  case "WorldChampionship":
                  case "WorldChampionshipITT":
                     strClassColor = "FFFFFF";
                     break;
                  case "FranceGrandTour":
                     strClassColor = "EED712";
                     break;
                  case "AutreGrandTour":
                     switch (drvRace["CONSTANT"].ToString())
                     {
                        case "giro":
                           strClassColor = "F095BD";
                           break;
                        case "vuelta":
                           strClassColor = "D2722A";
                           break;
                     }
                     break;
                  default:
                     strClassColor = dvUciClass[0]["gene_sz_calendar_color"].ToString();
                     break;
               }
               // Prüfen, an welchen Rennen das Team gemäss der UCI Regeln teilnehmen darf  
               switch (strClassName)
               {
                  case "WorldChampionship":
                  case "WorldChampionshipITT":
                  case "Cya_Cup":
                     strParticipationAllowed = "YES";
                     break;
                  case "FranceGrandTour":
                  case "AutreGrandTour":
                  case "AutresTours":
                  case "ClassiqueMajeure":
                  case "AutresClassiques":
                     switch (strTeamDivision)
                     {
                        case "GS1":
                        case "GS2":
                           strParticipationAllowed = "YES";
                           break;
                        default:
                           strParticipationAllowed = "UCI";
                           break;
                     }
                     break;
                  case "Cont2HC":
                  case "Cont1HC":
                     switch (strTeamDivision)
                     {
                        case "GS1":
                        case "GS2":
                           strParticipationAllowed = "YES";
                           break;
                        case "GS3":
                           if (intTeamCountry == intRaceCountry)
                              strParticipationAllowed = "YES";
                           break;
                        default:
                           strParticipationAllowed = "UCI";
                           break;
                     }
                     break;
                  case "Cont21":
                  case "Cont11":
                     switch (strTeamDivision)
                     {
                        case "GS1":
                        case "GS2":
                        case "GS3":
                           strParticipationAllowed = "YES";
                           break;
                        default:
                           strParticipationAllowed = "UCI";
                           break;
                     }
                     break;
                  case "Cont22":
                  case "Cont12":
                  case "Cont22U":
                  case "Cont12U":
                     switch (strTeamDivision)
                     {
                        case "GS2":
                           if (intTeamCountry == intRaceCountry)
                              strParticipationAllowed = "YES";
                           break;
                        case "GS3":
                           strParticipationAllowed = "YES";
                           break;
                        default:
                           strParticipationAllowed = "UCI";
                           break;
                     }
                     break;
                  default:
                     strParticipationAllowed = "UCI";
                     break;
               }
               // Prüfen, ob weitere Teams teilnehmen dürfen
               if (strParticipationAllowed == "YES")
               {
                  Int32 intIDrace = (Int32)drvRace["IDrace"];
                  dvTeamRace.RowFilter = string.Format("fkIDrace={0} AND fkIDteam={1}", intIDrace, _intIDteam);
                  if (dvTeamRace.Count == 0)
                     strParticipationAllowed = "ERROR"; // Das Team hat keinen Eintrag in DYN_team_race für dieses Rennen
                  else
                  {
                     dvTeamRace.RowFilter = string.Format("fkIDrace={0} AND fkIDteam<>{1} AND fkIDinvitation_state={2}", intIDrace, _intIDteam, intSubscribed);
                     if (dvTeamRace.Count >= intMaxTeams)
                        strParticipationAllowed = "TEAMCOUNT=" + intMaxTeams.ToString();
                  }
               }
            }
            drvRace["Participation_Allowed"] = strParticipationAllowed;
            drvRace["HlpClassColor"] = "#FF" + strClassColor;
            drvRace["HlpClassName"] = strClassName;
            drvRace["HlpMinRiders"] = intMinRiders;
            drvRace["HlpMaxRiders"] = intMaxRiders;
            drvRace["HlpMaxTeams"] = intMaxTeams;
            // Profil des Rennens
            string strTypeTour = "error";
            dvTypeTour.RowFilter = string.Concat("IDtype_tour=", drvRace["fkIDtype_tour"]);
            if (dvTypeTour.Count > 0)
            { 
               switch (dvTypeTour[0]["CONSTANT"].ToString())
               {
                  case "GRAND_TOUR":
                     strTypeTour = "empty";
                     break;
                  case "PETITS_TOURS_MO_TT":
                     strTypeTour = "MO_TT";
                     break;
                  case "PETITS_TOURS_MO_NOTT":
                  case "CLAS_MO":
                     strTypeTour = "MO";
                     break;
                  case "PETITS_TOURS_VAL_TT":
                     strTypeTour = "VAL_TT";
                     break;
                  case "PETITS_TOURS_VAL_NOTT":
                  case "CLAS_VAL":
                     strTypeTour = "VAL";
                     break;
                  case "PETITS_TOURS_NORD":
                     strTypeTour = "RVV";
                     break;
                  case "PETITS_TOURS_PL_VAL":
                  case "CLAS_PLVAL":
                     strTypeTour = "PL_VAL";
                     break;
                  case "PETITS_TOURS_PL_TT":
                  case "CLAS_TTT":
                     strTypeTour = "PL_TT";
                     break;
                  case "PETITS_TOURS_PL_NOTT":
                  case "CLAS_PLONLY":
                     strTypeTour = "PL";
                     break;
                  case "CLAS_ROUBAIX":
                     strTypeTour = "Roubaix";
                     break;
                  case "CLAS_RVV":
                     strTypeTour = "RVV";
                     break;
                  case "CLAS_PLPAV":
                     strTypeTour = "PL_PAV";
                     break;
                  default:
                     strTypeTour = "empty";
                     break;
               }
            }
            drvRace["HlpTypeTour"] = strTypeTour; 
            // Countries markieren, in denen Rennen stattfinden (für Filter)
            dvCountry.RowFilter = string.Format("IDcountry={0}", intRaceCountry);
            if (dvCountry.Count > 0)
               dvCountry[0]["fkIDteam"] = _intIDteam; // Das ist wohl der dümmste Ansatz, aber so muss kein Filter programmiert werden >> GetDataView("STA_country", true);
         }

         // Auch die leere Auswahl muss 'markiert' werden
         dvCountry.RowFilter = "IDcountry=-1"; // siehe ImportDatabase
         dvCountry[0]["fkIDteam"] = _intIDteam; 

         // Sponsoren Ziele
         DataView dvObjectif = GetDataView("DYN_objectif", false);
         foreach (DataRowView drvObjectif in dvObjectif)
         {
            dvRace.RowFilter = string.Concat("IDrace=", drvObjectif["fkIDrace"]);
            try
            {
               dvRace[0]["SponsorObjectif"] = LanguageOptions.Text(string.Format("CONSTANT/STA_objectif_type/ID{0:00}", drvObjectif["fkIDobjectif_type"]));
               dvRace[0]["PicturePathSponsorObjectif"] = string.Concat(_strApplicationFolder, "ClassLogos\\Important.png");
            }
            catch { }
         }

         // Angaben zu den einzelnen Fahrern aufbereiten + Reihenfolge für Anzeige bei Race-Teilnahme
         DataView dvSeasonTargets = new DataView();
         DataView dvCyclistPlanning = new DataView();
         DataView dvCyclistObjective = new DataView();
         DataView dvInjury = GetDataView("DYN_injury", false);
         DataView dvCyclist = GetDataView("DYN_cyclist", true);
         DataView dvRiderType = GetDataView("STA_type_rider", false);
         DataView dvSubsquadConfig = GetDataView("DYN_subsquad_config", false);
         if (_intPCMVersion >= 17)
         {
            dvCyclistPlanning = GetDataView("DYN_cyclist_planning", false);
         }
         else if (_intPCMVersion == 16)
         {
            dvSeasonTargets = GetDataView("DYN_season_targets", false); // Ziele 
            dvCyclistPlanning = GetDataView("DYN_cyclist_planning", false); // Fitness-Planung
         }
         else
         {
            dvCyclistObjective = GetDataView("DYN_cyclist_objective", false); // Fitness-Planung
         }
         // Prüfen ob es Fahrer-Gruppen in der DB gibt
         int intGroupIndex = 0;
         bool blnHasGroups = false;
         if (dvSubsquadConfig.Count == 0)
         {
            for (intGroupIndex = 1; intGroupIndex < Const.MaxGroups; intGroupIndex++)
            {
               DataRowView drv = dvSubsquadConfig.AddNew();
               drv["SortGroup"] = (int)Math.Pow(2, intGroupIndex);
               drv["gene_sz_name"] = $"Group {intGroupIndex}";
               drv.EndEdit();
            }
         }
         else
         {
            blnHasGroups = true;
            dvSubsquadConfig.Sort = "IDsubsquad";
            foreach (DataRowView drvGroup in dvSubsquadConfig)
               drvGroup["SortGroup"] = (int)Math.Pow(2, intGroupIndex++);
         }
         // Loop über alle Fahrer
         dvCyclist.Sort = "gene_sz_lastname, gene_sz_firstname";
         int[] intID = new int[Const.MaxCyclist];
         int intCyclistIndex = 0;
         foreach (DataRowView drvCyclist in dvCyclist)
         {
            if (intCyclistIndex >= Const.MaxCyclist)
            {
               LanguageOptions.ShowMessage("DBLoader/ToManyCyclists", MessageBoxButton.OK);
               _blnProgressCancelRequested = true;
               break;
            }
            intID[intCyclistIndex] = (Int32)drvCyclist["IDcyclist"];
            drvCyclist["HelpIndex"] = intCyclistIndex;
            // Fahrer genau einer Gruppe zuweisen (In PCM2017 gibt es keine Gruppen mehr >> try/catch)
            try   { intGroupIndex = LeastBitSet((Int16)drvCyclist["value_i_group_setting"]); }
            catch { intGroupIndex = -1; }
            if (blnHasGroups)
            {
               if (intGroupIndex == -1)
                  drvCyclist["SortGroup"] = (int)Math.Pow(2, Const.MaxGroups);
               else
                  drvCyclist["SortGroup"] = (int)Math.Pow(2, intGroupIndex);
            }
            else
            {
               drvCyclist["SortGroup"] = (int)Math.Pow(2, 1);
            }
            // Detaillierte Infos zum Fahrer
            dvRiderType.RowFilter = string.Concat("IDtype_rider=", drvCyclist["fkIDtype_rider"]);
            try { drvCyclist["RiderType"] = LanguageOptions.Text(string.Concat("CONSTANT/STA_type_rider/", dvRiderType[0]["CONSTANT"])); }
            catch { }
            if (_intPCMVersion >= 15)
               drvCyclist["TrainingType"] = LanguageOptions.Text(string.Format("CONSTANT/STA_training_exercise/ID{0:00}", drvCyclist["fkIDworkplan"]));
            else
               drvCyclist["TrainingType"] = LanguageOptions.Text(string.Format("CONSTANT/STA_training_exercise/ID{0:00}", drvCyclist["gene_i_workplan"]));
            dvInjury.RowFilter = string.Concat("IDinjury=", drvCyclist["fkIDinjury"]);
            try { drvCyclist["BackFromInjury"] = string.Format("{0} {1}", dvInjury[0]["gene_f_timeleft"], LanguageOptions.Text(string.Format("CONSTANT/STA_injury/ID{0:000}", dvInjury[0]["fkIDinjury"]))); } 
            catch { }
            //Felder, welche nicht in allen Versionen gleich heissen, gelöscht oder hinzugefügt wurden
            try
            {
               drvCyclist["Charac_plain"] = drvCyclist["charac_i_plain"];
               drvCyclist["Charac_mountain"] = drvCyclist["charac_i_mountain"];
               drvCyclist["Charac_hill"] = drvCyclist["charac_i_hill"];
               drvCyclist["Charac_downhilling"] = drvCyclist["charac_i_downhilling"];
               drvCyclist["Charac_timetrial"] = drvCyclist["charac_i_timetrial"];
               drvCyclist["Charac_endurance"] = drvCyclist["charac_i_endurance"];
               drvCyclist["Charac_resistance"] = drvCyclist["charac_i_resistance"];
               drvCyclist["Charac_recuperation"] = drvCyclist["charac_i_recuperation"];
               drvCyclist["Charac_cobble"] = drvCyclist["charac_i_cobble"];
               drvCyclist["Charac_baroudeur"] = drvCyclist["charac_i_baroudeur"];
               drvCyclist["Charac_sprint"] = drvCyclist["charac_i_sprint"];
               drvCyclist["Charac_acceleration"] = drvCyclist["charac_i_acceleration"];
               drvCyclist["Charac_prologue"] = drvCyclist["charac_i_prologue"];
               drvCyclist["RiderAge"] = DateTime.Now.Year - (Int32)drvCyclist["gene_i_birthdate"] / 10000;
            }
            catch { }
            // Fitness Program und Ziele
            if (_intPCMVersion >= 16)
            {
               dvCyclistPlanning.RowFilter = string.Concat("fkIDcyclist=", drvCyclist["IDcyclist"]);
               try
               {
                  string strList = dvCyclistPlanning[0]["ilist_peaks"].ToString().Replace(")", "").Replace("(", "");
                  drvCyclist["FitnessProgramList"] = strList;
               }
               catch { }
            }
            if (_intPCMVersion == 16)
            {
               dvSeasonTargets.RowFilter = string.Concat("fkIDcyclist=", drvCyclist["IDcyclist"]);
               try 
               {
                  drvCyclist["ManagerP1"] = dvSeasonTargets[0]["ManagerP1"];
                  drvCyclist["ManagerP2"] = dvSeasonTargets[0]["ManagerP2"];
                  drvCyclist["ManagerP3"] = dvSeasonTargets[0]["ManagerP3"];
               }
               catch { }
            }
            else
            {
               dvCyclistObjective.RowFilter = string.Concat("IDcyclist_objective=", drvCyclist["IDcyclist"]);
               try
               {
                  string strList = dvCyclistObjective[0]["gene_ilist_prio"].ToString().Replace(")", "");
                  int intInstr = strList.IndexOf(',');
                  drvCyclist["FitnessProgramList"] = strList.Substring(intInstr + 1);
               }
               catch { }
            }
            // Renn-Teilnahme / Lieblingsrennen
            dvRace.RowFilter = string.Concat("IDrace=", drvCyclist["fkIDrace"]);
            string[] strRaceListID = new string[0];
            string strRaceList = drvCyclist["gene_ilist_fkIDfavorite_races"].ToString();
            strRaceListID = strRaceList.Substring(1, strRaceList.Length - 2).Split(',');//skip parenthesis, then split
            foreach (string s in strRaceListID)
            {
               if (!string.IsNullOrEmpty(s))
               {
                  dvRace.RowFilter = string.Concat("IDrace=", s);
                  try { dvRace[0][string.Format("FavoriteRace_Cyclist{0:00}", intCyclistIndex)] = 1; }// string.Concat(_strApplicationFolder, "ClassLogos\\Important.png"); }
                  catch { }
               }
            }
            this.ProgressCurrent = ++intProgressCurrent;
            ++intCyclistIndex;
         }

         // Default-Sortierung einstellen
         int intSortIndex = 0;
         dvCyclist.Sort = "SortGroup, value_f_current_evaluation DESC";
         foreach (DataRowView drvCyclist in dvCyclist)
            drvCyclist["SortIndex"] = intSortIndex++;

         // Datum beim Race eintragen (und weitere Hilfsfelder ausfüllen)
         // qryStage = Liste aller Stages gruppiert auf Race -> Datum der ersten/letzten Etappe 
         var qryStage = from s in _dsCyanideDB.Tables["STA_stage"].AsEnumerable()
                        group s by s.Field<Int32>("fkIDrace") into g
                        select new { IDrace = g.Key, StartDate = g.Min(s => new DateTime(_intYear, s.Field<Int16>("gene_i_month"), s.Field<Int16>("gene_i_day"))), EndDate = g.Max(s => new DateTime(_intYear, s.Field<Int16>("gene_i_month"), s.Field<Int16>("gene_i_day"))) };
         foreach (var varStage in qryStage)
         {
            bool blnRaceExists;
            dvRace.RowFilter = string.Concat("IDrace=", varStage.IDrace);
            try 
            {
               dvRace[0]["StartDate"] = varStage.StartDate;
               dvRace[0]["EndDate"] = varStage.EndDate;
               blnRaceExists = true;
            }
            catch { blnRaceExists = false; }
            if (blnRaceExists)
            {
               // Anmelde-Status und geplante Fahrer
               string strInvitationState = string.Empty;
               string[] strRosterListID = new string[0];
               dvTeamRace.RowFilter = string.Format("fkIDteam={0} AND fkIDrace={1}", _intIDteam, varStage.IDrace);
               if (dvTeamRace.Count > 0)
               {
                  string strRosterList = dvTeamRace[0]["gene_ilist_roster"].ToString();
                  if (strRosterList.Length > 2)
                     strRosterListID = strRosterList.Substring(1, strRosterList.Length - 2).Split(',');
                  dvInvitationState.RowFilter = string.Concat("IDinvitation_state=", dvTeamRace[0]["fkIDinvitation_state"]);
                  try { strInvitationState = dvInvitationState[0]["CONSTANT"].ToString(); }
                  catch { }
               }
               dvRace[0]["Participation_Count"] = strRosterListID.Length;
               switch (strInvitationState)
               {
                  case "FORCED": 
                  case "SUBSCRIBED":
                  case "ASKING_INVITATION":
                  case "WILDCARD_GIVEN":
                     dvRace[0]["Participation_Team"] = true;
                     break;
                  default: // ASKING_WILDCARD // NOT_INVITED // NOT_INTERESTED // REJECTED // NATIONAL
                     dvRace[0]["Participation_Team"] = false;
                     break;
               }
               int intMax = Const.MaxCyclist + Const.MaxGroups;
               for (int i = 0; i < intMax; ++i)
               {
                  bool blnDoesParticipate = false;
                  try { blnDoesParticipate = strRosterListID.Contains(intID[i].ToString());  }
                  catch { }
                  dvRace[0][string.Format("Participation_Cyclist{0:00}", i)] = blnDoesParticipate;
                  if (i < Const.MaxCyclist)
                     if (dvRace[0][string.Format("FavoriteRace_Cyclist{0:00}", i)] == null)
                        dvRace[0][string.Format("FavoriteRace_Cyclist{0:00}", i)] = 0;
               }
            }
            this.ProgressCurrent = ++intProgressCurrent;
         }
         // Anzahl teilnehmender Fahrer + Renntage berechnen
         CalculateParticipatingCyclists(0);
         CalculatePlannedRacedays();
      }

      private static Int16 LeastBitSet(Int32 intBitArray)
      {
         if (intBitArray == 0)
            return -1;

         Int16 intBitPosition = 0;
         while (!((intBitArray & 1) == 1))
         {
            intBitArray >>= 1;
            ++intBitPosition;
         }
         return intBitPosition;
      }

      public static bool RaceIsOverlapping(int intIndex, DateTime dtStart, DateTime dtEnd)
      {
         var count = (from r in _dsCyanideDB.Tables["STA_race"].AsEnumerable() where r.Field<DateTime>("StartDate") <= dtEnd && r.Field<DateTime>("EndDate") >= dtStart && r.Field<Boolean>(string.Format("Participation_Cyclist{0:00}", intIndex)) select r).Count();
         return count > 0;
      }

      public static void CalculatePlannedRacedays()
      {
         DataView dvCyclist = GetDataView("DYN_cyclist", true);
         DataTable dtRace = _dsCyanideDB.Tables["STA_race"];
         foreach (DataRowView drv in dvCyclist)
         {
            var count = (from r in dtRace.AsEnumerable() where r.Field<Boolean>("Participation_Team") && r.Field<Boolean>(string.Format("Participation_Cyclist{0:00}", drv["HelpIndex"])) select (Int32)r.Field<Int16>("gene_i_number_stages")).Sum();
            drv["RaceDaysPlanned"] = count;
         }
      }

      public static void CalculatePlannedRacedays(Int32 intIDrace, int intCyclistIndex, bool blnParticipate)
      {
         DataView dvCyclist = GetDataView("DYN_cyclist", true);
         DataTable dtRace = _dsCyanideDB.Tables["STA_race"];
         DataView dvRace = new DataView(dtRace);
         dvRace.RowFilter = string.Format("IDrace={0}", intIDrace);
         if (intCyclistIndex >= Const.MaxCyclist)
         {
            dvCyclist.RowFilter = string.Format("SortGroup={0}", Math.Pow(2, (intCyclistIndex - Const.MaxCyclist)));
            foreach (DataRowView drv in dvCyclist)
            {
               dvRace[0][string.Format("Participation_Cyclist{0:00}", drv["HelpIndex"])] = blnParticipate;
               var count = (from r in dtRace.AsEnumerable() where r.Field<Boolean>("Participation_Team") && r.Field<Boolean>(string.Format("Participation_Cyclist{0:00}", drv["HelpIndex"])) select (Int32)r.Field<Int16>("gene_i_number_stages")).Sum();
               drv["RaceDaysPlanned"] = count;
            }
            CalculateParticipatingCyclists(intIDrace);
            if ((Int16)dvRace[0]["Participation_Count"] > (Int16)dvRace[0]["HlpMaxRiders"])
               if ((bool)App.Current.Properties["NotifySelection"])
                  LanguageOptions.ShowMessage("DBLoader/ParticipationToManyCyclists", MessageBoxButton.OK);
         }
         else
         {
            dvCyclist.RowFilter = string.Format("HelpIndex={0}", intCyclistIndex);
            var count = (from r in dtRace.AsEnumerable() where r.Field<Boolean>("Participation_Team") && r.Field<Boolean>(string.Format("Participation_Cyclist{0:00}", intCyclistIndex)) select (Int32)r.Field<Int16>("gene_i_number_stages")).Sum();
            dvCyclist[0]["RaceDaysPlanned"] = count;
            if (blnParticipate)
            {
               dvRace[0]["Participation_Count"] = (Int16)dvRace[0]["Participation_Count"] + 1;
               if ((Int16)dvRace[0]["Participation_Count"] > (Int16)dvRace[0]["HlpMaxRiders"])
                  if ((bool)App.Current.Properties["NotifySelection"])
                     LanguageOptions.ShowMessage("DBLoader/ParticipationToManyCyclists", MessageBoxButton.OK);
            }
            else
               dvRace[0]["Participation_Count"] = (Int16)dvRace[0]["Participation_Count"] - 1;
         }
      }

      private static void CalculateParticipatingCyclists(Int32 intIDrace)
      {
         DataView dvRace = GetDataView("STA_race", false);
         if (intIDrace > 0)
            dvRace.RowFilter = string.Format("IDrace={0}", intIDrace);
         foreach (DataRowView drv in dvRace)
         {
            int intCount = 0;
            for (int i = 0; i < Const.MaxCyclist; ++i)
            {
               string s = drv["IDrace"].ToString();
               if (s == "0")
                  s = "";
               if ((bool)drv[string.Format("Participation_Cyclist{0:00}", i)])
                  ++intCount;
            }
            drv["Participation_Count"] = intCount;
         }
      }


      //This function simply rewrites the export.xml file 
      //It loads the former export.xml to copy the structure and add the data 
      private void ExportDatabase(object sender, DoWorkEventArgs e)
      {
         //
         this.CalculateParticipation();
         this.UpdateFitnessPrograms();
         //
         this.ProgressCurrent = 0;
         char[] chrTrim = { '(', ')' };
         this.ProgressStatus = "Prepare file";
         XmlDocument xdocDumpOut = new XmlDocument();
         xdocDumpOut.Load(string.Concat(_strSaveFolder, Const.DumpOutFileName));
         XmlDocument xdocDumpIn = new XmlDocument();
         xdocDumpIn.AppendChild(xdocDumpIn.ImportNode(xdocDumpOut.FirstChild, false));
         XmlNode xDumpOutRoot = xdocDumpOut.FirstChild;
         XmlNode DBdest = xdocDumpIn.FirstChild;
         this.ProgressMaximum = xDumpOutRoot.ChildNodes.Count;

         for (int intTableIndex = 0; intTableIndex < xDumpOutRoot.ChildNodes.Count; ++intTableIndex)
         {
            //
            XmlNode xDumpOutTable = xDumpOutRoot.ChildNodes.Item(intTableIndex);
            string strTableName = xDumpOutTable.Attributes[Const.table_name].Value;
            //
            XmlElement xDumpInTable;
            if (!((IList)Const.ExportTables).Contains(strTableName))
            {
               xDumpInTable = (XmlElement)xdocDumpIn.ImportNode(xDumpOutTable, true);
            }
            else
            {
               DataTable dtTable = _dsCyanideDB.Tables[strTableName];
               int intNumRows = dtTable.Rows.Count;
               xDumpInTable = xdocDumpIn.CreateElement(Const.table);
               XmlAttribute xAttribute;
               xAttribute = xdocDumpIn.CreateAttribute(Const.table_name);
               xAttribute.Value = strTableName;
               xDumpInTable.SetAttributeNode(xAttribute);
               xAttribute = xdocDumpIn.CreateAttribute(Const.table_id);
               xAttribute.Value = xDumpOutTable.Attributes[Const.table_id].Value;
               xDumpInTable.SetAttributeNode(xAttribute);
               xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumOCols);
               xAttribute.Value = xDumpOutTable.Attributes[Const.table_NumOCols].Value;
               xDumpInTable.SetAttributeNode(xAttribute);
               xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumCols);
               xAttribute.Value = xDumpOutTable.Attributes[Const.table_NumCols].Value;
               xDumpInTable.SetAttributeNode(xAttribute);
               xAttribute = xdocDumpIn.CreateAttribute(Const.table_NumRows);
               xAttribute.Value = intNumRows.ToString();
               xDumpInTable.SetAttributeNode(xAttribute);

               foreach (XmlNode xDumpOutColumn in xDumpOutTable.ChildNodes)
               {
                  XmlElement xDumpInColumn = xdocDumpIn.CreateElement(Const.column);
                  string strColumnName = xDumpOutColumn.Attributes[Const.column_name].Value;
                  string strColumnType = xDumpOutColumn.Attributes[Const.column_type].Value;
                  xAttribute = xdocDumpIn.CreateAttribute(Const.column_name);
                  xAttribute.Value = strColumnName;
                  xDumpInColumn.SetAttributeNode(xAttribute);
                  xAttribute = xdocDumpIn.CreateAttribute(Const.column_ID);
                  xAttribute.Value = xDumpOutColumn.Attributes[Const.column_ID].Value; 
                  xDumpInColumn.SetAttributeNode(xAttribute);
                  xAttribute = xdocDumpIn.CreateAttribute(Const.column_type);
                  xAttribute.Value = strColumnType;
                  xDumpInColumn.SetAttributeNode(xAttribute);

                  this.ProgressStatus = string.Format("{0}_{1}", strTableName, strColumnName);

                  for (int i = 0; i < intNumRows; ++i)
                  {
                     XmlElement xnCell;
                     string strValue = dtTable.Rows[i][strColumnName].ToString();
                     switch (strColumnType)
                     {
                        case "Bool":
                           xnCell = xdocDumpIn.CreateElement(Const.cell);
                           if (string.IsNullOrEmpty(strValue))
                              xnCell.InnerText = "0"; // Wert ist NULL, d.h. noch gar kein Wert eingegeben --> False
                           else
                              xnCell.InnerText = Convert.ToInt16(dtTable.Rows[i][strColumnName]).ToString(); 
                           break;
                        case "Float":
                           xnCell = xdocDumpIn.CreateElement(Const.cell);
                           if (string.IsNullOrEmpty(strValue))
                              xnCell.InnerText = "0"; // Wert ist NULL, d.h. noch gar kein Wert eingegeben --> 0
                           else
                              xnCell.InnerText = XmlConvert.ToString((Single)dtTable.Rows[i][strColumnName]);
                           break;
                        case "ListInt":
                        case "ListFloat":
                           xnCell = xdocDumpIn.CreateElement(Const.list);
                           strValue = strValue.Trim(chrTrim); // Dieser Code ist besser, denn es kann ja sein, dass gar keine Klammern vorhanden sind
                           if (string.IsNullOrEmpty(strValue))
                           {
                              XmlAttribute xListSize = xdocDumpIn.CreateAttribute(Const.list_size);
                              xListSize.Value = "0";
                              xnCell.SetAttributeNode(xListSize);
                           }
                           else
                           {
                              string[] items = strValue.Split(',');
                              foreach (string item in items)
                              {
                                 XmlElement xItem = xdocDumpIn.CreateElement(Const.cell);
                                 if (!string.IsNullOrEmpty(item))
                                    xItem.InnerText = item;
                                 xnCell.InsertAfter(xItem, xnCell.LastChild);
                              }
                              XmlAttribute xListSize = xdocDumpIn.CreateAttribute(Const.list_size);
                              xListSize.Value = items.Length.ToString();
                              xnCell.SetAttributeNode(xListSize);
                           }
                           break;
                        default: // normal cell, just copy the data 
                           xnCell = xdocDumpIn.CreateElement(Const.cell);
                           if (!string.IsNullOrEmpty(strValue))
                              xnCell.InnerText = strValue; //SecurityElement.Escape(strValue)
                           break;
                     }
                     xDumpInColumn.InsertAfter(xnCell, xDumpInColumn.LastChild);
                  }
                  xDumpInTable.InsertAfter(xDumpInColumn, xDumpInTable.LastChild);
               }
            }
            DBdest.InsertAfter(xDumpInTable, DBdest.LastChild);
            this.ProgressCurrent = intTableIndex;
         }
         string strExportPath = string.Concat(_strSaveFolder, Const.DumpInFileName);
         xdocDumpIn.Save(strExportPath);
         // Import in die CDB
         this.ProgressStatus = "Building database";
         ProcessStartInfo pci = new ProcessStartInfo(string.Concat(_strApplicationFolder, Const.ExporterApplication));
         pci.Arguments = string.Format(" -input \"{0}\" -output \"{1}\" -FromXML", strExportPath, _strCDBPath);
         pci.WindowStyle = ProcessWindowStyle.Hidden;//hide console 
         this.ProgressMaximum = 1000000;
         this.ProgressCurrent = 0;
         int intHlp = 0;
         Process proc = Process.Start(pci);
         while (!proc.HasExited) { this.ProgressCurrent = intHlp++; }
      }

      private void CalculateParticipation()
      {
         DataView dvCyclist = GetDataView("DYN_cyclist", true);
         dvCyclist.Sort = "HelpIndex";
         int intMaxIndex = dvCyclist.Count;
         int[] intID = new int[intMaxIndex];
         for (int i = 0; i < intMaxIndex; ++i)
         {
            intID[i] = (Int32)dvCyclist[i]["IDcyclist"];
            // Gruppe kann nicht in die DB geschrieben werden, denn das Spiel verwendet einen andere Logik
            //dvCyclist[i]["value_i_group_setting"] = dvCyclist[i]["SortGroup"];
         }
         string[] strObjective = new string[intMaxIndex];
         DataView dvFitnessData = new DataView();
         DataView dvRace = GetDataView("STA_race", false);
         DataView dvTeamRace = GetDataView("DYN_team_race", true);
         DataView dvTeamRaceAll = GetDataView("DYN_team_race", false);
         DataView dvInvitationState = GetDataView("STA_invitation_state", false);
         if (_intPCMVersion == 16)
            dvFitnessData = GetDataView("DYN_procyclist_fitness_data", false);
         dvTeamRaceAll.Sort = "fkIDteam DESC";
         // Konstanten für Renn-Anmeldung
         int intNotInterested = 0;
         dvInvitationState.RowFilter = "CONSTANT='NOT_INTERESTED'";
         try { intNotInterested = (Int32)dvInvitationState[0]["IDinvitation_state"]; }
         catch { }
         int intSubscribed = 0;
         dvInvitationState.RowFilter = "CONSTANT='SUBSCRIBED'";
         try { intSubscribed = (Int32)dvInvitationState[0]["IDinvitation_state"]; }
         catch { }
         int intRejected = 0;
         dvInvitationState.RowFilter = "CONSTANT='REJECTED'";
         try { intRejected = (Int32)dvInvitationState[0]["IDinvitation_state"]; }
         catch { }
         // Loop über alle Rennen, an denen das Team teilnehmen darf/kann
         foreach (DataRowView drvTeamRace in dvTeamRace)
         {
            // Anmelde-Status des Teams anpassen (falls nötig)
            //bool blnCheckTeamCount = false;
            string strInvitationState = string.Empty;
            dvInvitationState.RowFilter = string.Concat("IDinvitation_state=", drvTeamRace["fkIDinvitation_state"]);
            try { strInvitationState = dvInvitationState[0]["CONSTANT"].ToString(); }
            catch { }
            dvRace.RowFilter = string.Concat("IDrace=", drvTeamRace["fkIDrace"]);
            switch (strInvitationState)
            {
               case "FORCED":
               case "NATIONAL":
               case "NOT_INVITED":
               case "ASKING_WILDCARD":
                  break;
               default:
                  if ((Boolean)dvRace[0]["Participation_Team"])
                  {
                     if ((Int32)drvTeamRace["fkIDinvitation_state"] != intSubscribed)
                     {
                        //blnCheckTeamCount = true;
                        drvTeamRace["fkIDinvitation_state"] = intSubscribed;
                        // Wird eigentlich nur für Pry Cyclist Modus benötigt
                        switch (drvTeamRace["fkIDpro_cyclist_state"].ToString())
                        {
                           case "3":
                           case "7":
                              break;
                           default:
                              drvTeamRace["fkIDpro_cyclist_state"] = 6;
                              break;
                        }
                     }
                  }
                  else
                  {
                     drvTeamRace["fkIDinvitation_state"] = intNotInterested;
                     // Wird eigentlich nur für Pry Cyclist Modus benötigt
                     drvTeamRace["fkIDpro_cyclist_state"] = 0;
                     if (_intPCMVersion == 16)
                     {
                        dvFitnessData.RowFilter = string.Concat("value_i_workload=", drvTeamRace["fkIDrace"]);
                        foreach (DataRowView drv in dvFitnessData)
                           drv["value_i_workload"] = -3;
                     }
                  }
                  break;
            }
            //if (blnCheckTeamCount)
            //{
            //   dvTeamRaceAll.RowFilter = string.Format("fkIDrace={0} AND fkIDinvitation_state={1}", drvTeamRace["fkIDrace"], intSubscribed);
            //   if (dvTeamRaceAll.Count > 26)
            //   {
            //      dvTeamRaceAll.RowFilter = string.Format("fkIDrace={0} AND fkIDinvitation_state={1} AND fkIDteam<>", drvTeamRace["fkIDrace"], intSubscribed, _intIDteam);
            //      dvTeamRaceAll[0]["fkIDinvitation_state"] = intRejected;
            //   }
            //}
            // Ausgewählte Fahrer 
            string strList = string.Empty;
            //bool blnSponsorObjective = (!String.IsNullOrEmpty(dvRace[0]["SponsorObjectif"].ToString()));
            bool blnSponsorObjective = (dvRace[0]["SponsorObjectif"] != DBNull.Value);
            for (int i = 0; i < intMaxIndex; ++i)
               if ((bool)dvRace[0][string.Format("Participation_Cyclist{0:00}", i)])
               {
                  strList = string.Concat(strList, ",", intID[i]);
                  if (blnSponsorObjective)
                     strObjective[i] = string.Concat(strObjective[i], ",", drvTeamRace["fkIDrace"]);
               }
            if (strList.Length > 0)
            {
               if ((bool)App.Current.Properties["SelectionPlaceholder"])
               {  // Auf Wunsch wird die Liste aufgefüllt mit Duplikaten, damit das Spiel nicht automatisch weitere Fahrer hinzufügt am Renntag
                  string[] strHlp = strList.Substring(1).Split(',');
                  int intHlp = 0, intCount = strHlp.Count();
                  for (int i = intCount; i < 9; ++i)
                  {
                     strList = string.Concat(strList, ",", strHlp[intHlp++]);
                     if (intHlp >= intCount) intHlp = 0;
                  }
               }
               drvTeamRace["gene_ilist_roster"] = String.Format("({0})", strList.Substring(1));
            }
            else
               drvTeamRace["gene_ilist_roster"] = "()";
         }
         if (_intPCMVersion < 16)
         {  // In früheren Version konnte man die Rennplanung automatisieren, wenn man nur die Ziele eingestellt hatte
            DataView dvCyclistObjective = GetDataView("DYN_cyclist_objective", false);
            for (int i = 0; i < intMaxIndex; ++i)
            {
               dvCyclistObjective.RowFilter = string.Format("IDcyclist_objective={0}", intID[i]);
               if (dvCyclistObjective.Count == 1)
               {
                  if (!string.IsNullOrEmpty(strObjective[i]))
                     dvCyclistObjective[0]["gene_ilist_race"] = String.Format("({0})", strObjective[i].Substring(1));
                  else
                     dvCyclistObjective[0]["gene_ilist_race"] = "()";
               }
            }
         }
      }

      private void UpdateFitnessPrograms()
      {  // Fitnessplanung und Saison-Ziele
         DataView dvSeasonTargets = new DataView();
         DataView dvCyclistPlanning = new DataView();
         DataView dvCyclistObjective = new DataView();
         DataView dvCyclist = GetDataView("DYN_cyclist", true);
         if (_intPCMVersion >= 17)
         {
            dvCyclistPlanning = GetDataView("DYN_cyclist_planning", false);
         }
         else if (_intPCMVersion == 16)
         {
            dvSeasonTargets = GetDataView("DYN_season_targets", false);
            dvCyclistPlanning = GetDataView("DYN_cyclist_planning", false);
         }
         else
            dvCyclistObjective = GetDataView("DYN_cyclist_objective", false);
         DataView dvCyclistSatisfaction = GetDataView("DYN_cyclist_satisfaction", false);
         string strListFallenLeader = string.Empty;
         foreach (DataRowView drvC in dvCyclist)
         {
            // Fitnessplanung 
            if (_intPCMVersion >= 16)
            {
               dvCyclistPlanning.RowFilter = string.Concat("fkIDcyclist=", drvC["IDcyclist"]);
               if (dvCyclistPlanning.Count != 0)
               {
                  try { dvCyclistPlanning[0]["ilist_peaks"] = String.Format("({0})", drvC["FitnessProgramList"]); }
                  catch { }
               }
               else if (drvC["FitnessProgramList"].ToString().Length != 0)
               {
                  dvCyclistPlanning.RowFilter = "";
                  dvCyclistPlanning.Sort = "IDcyclist_planning DESC";
                  int intIDmax = 1;
                  try { intIDmax = (int)dvCyclistPlanning[0]["IDcyclist_planning"] + 1; }
                  catch { }
                  DataRowView drv = dvCyclistPlanning.AddNew();
                  drv["IDcyclist_planning"] = intIDmax;
                  drv["fkIDcyclist"] = drvC["IDcyclist"];
                  drv["ilist_peaks"] = String.Format("({0})", drvC["FitnessProgramList"]);
                  drv.EndEdit();
               }
            }
            if (_intPCMVersion == 16)
            {
               // Ziele
               dvSeasonTargets.RowFilter = string.Concat("fkIDcyclist=", drvC["IDcyclist"]);
               if (dvSeasonTargets.Count != 0)
               {
                  if (drvC["ManagerP1"].ToString().Length == 0 && drvC["ManagerP2"].ToString().Length == 0 && drvC["ManagerP3"].ToString().Length == 0)
                  {
                     strListFallenLeader = string.Concat(strListFallenLeader, ",", dvSeasonTargets[0]["IDseason_target"]);
                     dvSeasonTargets[0].Delete();
                  }
                  else
                  {
                     try { dvSeasonTargets[0]["ManagerP1"] = Int16.Parse(drvC["ManagerP1"].ToString()); }
                     catch { }
                     try { dvSeasonTargets[0]["ManagerP2"] = Int16.Parse(drvC["ManagerP2"].ToString()); }
                     catch { }
                     try { dvSeasonTargets[0]["ManagerP3"] = Int16.Parse(drvC["ManagerP3"].ToString()); }
                     catch { }
                  }
               }
               else if (drvC["ManagerP1"].ToString().Length != 0 && drvC["ManagerP2"].ToString().Length != 0 && drvC["ManagerP3"].ToString().Length != 0)
               {
                  dvSeasonTargets.RowFilter = "";
                  dvSeasonTargets.Sort = "IDseason_target DESC";
                  try 
                  { 
                     int intIDmax = (int)dvSeasonTargets[0]["IDseason_target"] + 1;
                     DataRowView drv = dvSeasonTargets.AddNew();
                     drv["IDseason_target"] = intIDmax;
                     drv["fkIDcyclist"] = drvC["IDcyclist"];
                     drv["Year"] = dvSeasonTargets[0]["Year"];
                     drv["SeasonTarget"] = Int16.Parse(drvC["ManagerP1"].ToString());
                     drv["AltObjective1"] = Int16.Parse(drvC["ManagerP2"].ToString());
                     drv["Coeff1"] = 0;
                     drv["AltObjective2"] = Int16.Parse(drvC["ManagerP3"].ToString());
                     drv["Coeff2"] = 0;
                     drv["ManagerP1"] = Int16.Parse(drvC["ManagerP1"].ToString());
                     drv["ManagerP2"] = Int16.Parse(drvC["ManagerP2"].ToString());
                     drv["ManagerP3"] = Int16.Parse(drvC["ManagerP3"].ToString());
                     drv.EndEdit();
                     drvC["fkIDleader_target"] = intIDmax;
                  }
                  catch { }
               }
            }
            else
            {
               dvCyclistObjective.RowFilter = string.Concat("IDcyclist_objective=", drvC["IDcyclist"]);
               try { dvCyclistObjective[0]["gene_ilist_prio"] = string.Concat("(0,", drvC["FitnessProgramList"], ")"); }
               catch { }
            }
            // Geplante Renntage
            dvCyclistSatisfaction.RowFilter = string.Concat("IDcyclist_satisfaction=", drvC["IDcyclist"]);
            try { dvCyclistSatisfaction[0]["value_i_racescheduled"] = drvC["RaceDaysPlanned"]; }
            catch { }
         }
         // Falls ein Fahrer kein Leader mehr ist (keine Ziele mehr hat) dann müssen er und alle seine Helfer einem anderen Leader zugewiesen werden
         if (strListFallenLeader.Length > 0)
         {
            strListFallenLeader = strListFallenLeader.Substring(1);
            dvCyclist.RowFilter = $"fkIDteam={_intIDteam} AND fkIDleader_target NOT IN ({strListFallenLeader})";
            string strListLeader = string.Empty;
            foreach (DataRowView drvC in dvCyclist)
               if (!strListLeader.Contains(string.Concat(',', drvC["fkIDleader_target"].ToString())))
                  strListLeader = string.Concat(strListLeader, ',', drvC["fkIDleader_target"]);
            string[] strFallenLeader = strListFallenLeader.Split(',');
            string[] strLeader = strListLeader.Substring(1).Split(',');
            foreach (string s in strFallenLeader)
            {
               int intIndex = 0;
               dvCyclist.RowFilter = $"fkIDteam={_intIDteam} AND fkIDleader_target={s})"; 
               foreach (DataRowView drv in dvCyclist)
               {
                  if (strLeader.Length > 0)
                  {
                     drv["fkIDleader_target"] = strLeader[intIndex++];
                     if (intIndex >= strLeader.Length)
                        intIndex = 0;
                  }
                  else
                     drv["fkIDleader_target"] = 0;
               }
            }
         }
      }

      private static DateTime UnixTimestampToDateTime(long lngUnixTimeStamp)
      {
         return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(lngUnixTimeStamp);
      }

      #region IProgressOperation Members

      /// <summary>
      /// Starts the background operation that will export the event logs
      /// </summary>
      public void ProgressStart(string strAction, string strCDBPath)
      {
         _strCDBPath = strCDBPath;
         BackgroundWorker worker = new BackgroundWorker();
         switch (strAction)
         {
            case "ImportDatabase":
               worker.DoWork += new DoWorkEventHandler(ImportDatabase);
               break;
            case "ExportDatabase":
               worker.DoWork += new DoWorkEventHandler(ExportDatabase);
               break;
            default:
               return;
         }
         worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
         worker.RunWorkerAsync();
      }

      /// <summary>
      /// Requests cancelation of the event log exporting
      /// </summary>
      public void ProgressCancelAsync()
      {
         this._blnProgressCancelRequested = true;
      }

      public string ProgressStatus
      {
         get
         {
            return _strProgressStatus;
         }
         private set
         {
            _strProgressStatus = value;
            OnProgressStatusChanged(EventArgs.Empty);
         }
      }

      public int ProgressMaximum
      {
         get
         {
            return _intProgressMaximum;
         }
         private set
         {
            _intProgressMaximum = value;
            OnProgressMaximumChanged(EventArgs.Empty);
         }
      }

      public int ProgressCurrent
      {
         get
         {
            return _intProgressCurrent;
         }
         private set
         {
            _intProgressCurrent = value;
            OnProgressCurrentChanged(EventArgs.Empty);
         }
      }

      public event EventHandler ProgressStatusChanged;
      public event EventHandler ProgressCurrentChanged;
      public event EventHandler ProgressMaximumChanged;
      public event EventHandler ProgressCompleted;

      #endregion


      #region IProgressOperation Helper

      void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
      {
         OnComplete(e);
      }

      protected virtual void OnProgressStatusChanged(EventArgs e)
      {
         if (this.ProgressStatusChanged != null)
            this.ProgressStatusChanged(this, e);
      }

      protected virtual void OnProgressMaximumChanged(EventArgs e)
      {
         if (this.ProgressMaximumChanged != null)
            this.ProgressMaximumChanged(this, e);
      }

      protected virtual void OnProgressCurrentChanged(EventArgs e)
      {
         if (this.ProgressCurrentChanged != null)
            this.ProgressCurrentChanged(this, e);
      }

      protected virtual void OnComplete(RunWorkerCompletedEventArgs e)
      {
         if (e.Error != null)
            ShowUnhandledException(e);
         if (this.ProgressCompleted != null)
            this.ProgressCompleted(this, e);
      }
      #endregion


      private void ShowUnhandledException(RunWorkerCompletedEventArgs e)
      {
         // new line: \r\n or  Environment.NewLine
         string strLastExceptionMessage = string.Empty;
         string strExceptionStackTrace = e.Error.StackTrace;
         Exception exception = e.Error.InnerException;
         System.Text.StringBuilder msg = new System.Text.StringBuilder("----An error occured----\r\n");
         msg.Append(e.Error.Message);
         strLastExceptionMessage = e.Error.Message;
         while (exception != null)
         {
            if (strLastExceptionMessage != exception.Message)
            {
               msg.AppendFormat("\r\n\r\n----Inner error----\r\n{0}", exception.Message);
               strLastExceptionMessage = exception.Message;
            }
            strExceptionStackTrace = exception.StackTrace;
            exception = exception.InnerException;
         }
         MessageBox.Show(msg.ToString(), "Error");
         msg.AppendFormat("\r\n\r\n----Stacktrace----\r\n{0}", strExceptionStackTrace);
         StreamWriter sw = new StreamWriter(_strApplicationFolder + "DBLoader_Error.txt");
         sw.Write(msg.ToString());
         sw.Close();
         sw.Dispose();
      }
   }

   public class CyclistGrouping
   {
      public int Group;
      public int Sort;
      public int ID;
      public CyclistGrouping() { }
      public CyclistGrouping(int intID, int intSort, int intGroup)
      {
         Group = intGroup;
         Sort = intSort;
         ID = intID;
      }
   }
}
