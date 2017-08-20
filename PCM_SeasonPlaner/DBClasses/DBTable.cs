using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Xml;
using System;

namespace PCM_SeasonPlaner
{
   public class DBTable : DataTable
   {

      #region DECLARATION

      private DBColumn[] _dbcColumns;

      #endregion


      #region CONSTRUCTORS


      public DBTable(string strTableName) : base(strTableName)
      {
      }


      public DBTable(string strTableName, DBColumn[] dbcColumns, string[,] strCells, int intTableId, int intNumOrigCols, int intNumCols, int intNumRows) : base(strTableName)
      {
         // Alle Spalten anlegen gemäss Definition
         this.DefineColumns(dbcColumns);

         //if (strTableName == "STA_country")
         //   System.Windows.MessageBox.Show(strTableName);

         // Daten einlesen
         NumberStyles styles;
         styles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;
         NumberFormatInfo nfiFloat = new NumberFormatInfo();
         nfiFloat.NumberDecimalSeparator = ".";
         nfiFloat.NumberGroupSeparator = ",";
         for (int r = 0; r < intNumRows; ++r)
         {
            DataRow dr = this.NewRow();
            for (int c = 0; c < intNumCols; ++c)
            {
               switch (_dbcColumns[c].DataType)
               {
                  case "Bool":
                     dr[c] = (strCells[r, c] == "1");
                     break;
                  case "Float":
                     Single f = 0; // Bisher Decimal
                     try { f = Single.Parse(strCells[r, c], styles, nfiFloat); }
                     catch 
                     { 
                     //   LanguageOptions.ShowMessage("DBLoader/ExtractDB_NumberInvalid", System.Windows.MessageBoxButton.OK, new string[] { this.TableName, _dbcColumns[0].ColumnName, strCells[r, 0], _dbcColumns[c].ColumnName, strCells[r, c] }); 
                     }
                     dr[c] = f;
                     break;
                  case "String":
                     dr[c] = strCells[r, c];
                     break;
                  default:
                     dr[c] = strCells[r, c];
                     break;
               }
            }
            this.Rows.Add(dr);
         }
         //// Hilfszeilen anfügen
         //switch (strTableName)
         //{
         //   case "STA_race":
         //      for (int x = 0; x < 40; x++)
         //      {
         //         DataRow dr = this.NewRow();
         //         dr["StartDate"] = new DateTime(DBLoader.Year, 12, 31);
         //         dr["EndDate"] = new DateTime(DBLoader.Year, 12, 31);
         //         dr["HlpMinRiders"] = -1;
         //         dr["HlpMaxRiders"] = 0;
         //         dr["Participation_Team"] = true;
         //         dr["HlpClassName"] = "Undefined";
         //         int intMax = Const.MaxCyclist + Const.MaxGroups;
         //         for (int i = 0; i < intMax; ++i)
         //         {
         //            dr[string.Format("Participation_Cyclist{0:00}", i)] = false;
         //            if (i < Const.MaxCyclist)
         //               dr[string.Format("FavoriteRace_Cyclist{0:00}", i)] = 0;
         //         }
         //         this.Rows.Add(dr);
         //      }
         //      break;
         //}
      }


      public DBTable(string strTableName, DBColumn[] dbcColumns) : base(strTableName)
      {
         // Alle Spalten anlegen gemäss Definition
         this.DefineColumns(dbcColumns);
      }

      #endregion


      #region PUBLIC METHODS


      public string DataType(int intCol)
      {
         return _dbcColumns[intCol].DataType;
      }

      #endregion


      #region PRIVATE METHODS


      private void DefineColumns(DBColumn[] dbcColumns)
      {
         _dbcColumns = dbcColumns;
         for (int intColIndex = 0; intColIndex < dbcColumns.Length; ++intColIndex)
            this.AddDataColumnFromDBColumn(intColIndex);
      }

      private bool AddDataColumnFromDBColumn(int intColIndex)
      {
         //bool blnIsPrimary = false;
         string strDataType = _dbcColumns[intColIndex].DataType;
         string strColumnName = _dbcColumns[intColIndex].ColumnName;
         if (strColumnName.StartsWith("fkID"))
            strDataType = "Int32";
         else if (strColumnName.StartsWith("ID"))
         {
            strDataType = "Int32";
            //blnIsPrimary = true;
         }

         if (strColumnName == "value_i_group_setting")
            strColumnName = "value_i_group_setting";

         DataColumn dc = new DataColumn(strColumnName, GetXMLType(strDataType));
         //if (!blnIsPrimary)
         //{
         //   switch (strDataType)
         //   {
         //      case "Float":
         //      case "Int32":
         //      case "Int16":
         //      case "Int8":
         //         dc.DefaultValue = 0;
         //         break;
         //      case "Bool":
         //         dc.DefaultValue = false;
         //         break;
         //      case "String":
         //      case "ListInt":
         //      case "ListFloat":
         //      case "Date": //Wird nicht in der DB verwendet, sondern nur für Hilfsfelder
         //      default:
         //         // kein Default-Wert
         //         break;
         //   }
         //}
         base.Columns.Add(dc);
         return true;
      }

      private Type GetXMLType(string strDataType)
      {
         switch (strDataType)
         {
            case "Float":
               return Type.GetType("System.Single"); // Bisher Decimal
            case "Int32":
               return Type.GetType("System.Int32");
            case "Int16":
               return Type.GetType("System.Int16");
            case "Int8":
               return Type.GetType("System.Int16"); //Byte
            case "Bool":
               return Type.GetType("System.Boolean");
            case "String":
            case "ListInt":
            case "ListFloat":
               return Type.GetType("System.String");
            case "Date": //Wird nicht in der DB verwendet, sondern nur für Hilfsfelder
               return Type.GetType("System.DateTime");
            default:
               return Type.GetType("System.String");
         }
      }

      #endregion

   }
}
