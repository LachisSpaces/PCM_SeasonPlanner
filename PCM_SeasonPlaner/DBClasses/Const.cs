namespace PCM_SeasonPlaner
{
   public class Const
   {
      public const int MaxGroups = 9;
      public const int MaxCyclist = 40;

      public static readonly string ApplicationOptionFile = "Options.xml";

      public static readonly string ExporterApplication = "Exporter.exe";
      public static readonly string DumpOutFileName = "_DumpOut.cdbe";
      public static readonly string DumpInFileName = "_DumpIn.cdbe";

      public static readonly string ListCategoriesFileName = "ListCategories.xml";
      public static readonly string SettingsFileName = "Settings.xml";
      public static readonly string SettingsTopNode = "PCM_SeasonPlaner";
      public static readonly string SettingsTables = "Tables";
      public static readonly string SettingsTable = "Table";
      public static readonly string SettingsCyclistID = "IDcyclist";
      public static readonly string SettingsLastCDBPath = "LastCDBFolder";
      public static readonly string FitnessProgramTopNode = "root";
      public static readonly string FitnessProgramBar = "Bar_";
      public static readonly string FitnessProgramValue = "value";
      public static readonly string Index = "Index";
      //public static readonly string Settings;

      public static readonly string db = "CyanideDatabase";
      public static readonly string db_NumOTables = "NumOriginalTables";
      public static readonly string db_NumTables = "NumTables";
      public static readonly string table = "Table";
      public static readonly string table_name = "TableName";
      public static readonly string table_id = "TableID";
      public static readonly string table_NumRows = "NumRows";
      public static readonly string table_NumOCols = "NumOriginalColumns";
      public static readonly string table_NumCols = "NumColumns";
      public static readonly string column = "Column";
      public static readonly string column_name = "ColumnName";
      public static readonly string column_ID = "ColumnID";
      public static readonly string column_type = "ColumnType";
      public static readonly string cell = "Cell";
      public static readonly string list = "List";
      public static readonly string list_size = "ListSize";

      public static readonly string[] ImportTables = new string[] { "DYN_cyclist", "DYN_cyclist_objective", "DYN_cyclist_planning", "DYN_cyclist_satisfaction", "DYN_cyclist_training_plan", "DYN_injury", "DYN_objectif", "DYN_procyclist_fitness_data", "DYN_season_targets", "DYN_subsquad_config", "DYN_team", "DYN_team_race", "GAM_config", "GAM_user", "STA_country", "STA_division", "STA_invitation_state", "STA_objectif_type", "STA_race", "STA_stage", "STA_type_rider", "STA_type_tour", "STA_UCI_class" }; // "DYN_contract_cyclist",  , "DYN_sponsor", "STA_cyclist_statut"
      public static readonly string[] ExportTables = new string[] { "DYN_cyclist", "DYN_cyclist_objective", "DYN_cyclist_planning", "DYN_cyclist_satisfaction", "DYN_cyclist_training_plan", "DYN_procyclist_fitness_data", "DYN_season_targets", "DYN_team_race" }; //, "DYN_subsquad_config", "DYN_team", "DYN_sponsor"
   }
}
