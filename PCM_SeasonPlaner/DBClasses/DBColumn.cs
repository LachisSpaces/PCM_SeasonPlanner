namespace PCM_SeasonPlaner
{
   public class DBColumn
   {

      public DBColumn(string strColumnName, string strDataType)
      {
         _strColumnName = strColumnName;
         _strDataType = strDataType;
      }

      private string _strColumnName = string.Empty;
      public string ColumnName
      {
         get { return _strColumnName; }
      }

      private string _strDataType = string.Empty;
      public string DataType
      {
         get { return _strDataType; }
      }

   }
}
