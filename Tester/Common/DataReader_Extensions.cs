using System;
using System.Data;

namespace Tester.Common
{
    public static class DataReader_Extensions
    {
        /// <summary>
        /// This will return null if the field has a DBNull.Value, it will throw an exception for an invalid fieldName or if the operation otherwise fails
        /// </summary>
        public static string GetString(this IDataReader reader, string fieldName)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Null/empty fieldName specified");

            int fieldIndex;
            try
            {
                fieldIndex = reader.GetOrdinal(fieldName);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new ArgumentException("Invalid fieldName specified", e);
            }
            if (reader.IsDBNull(fieldIndex))
                return null;
            return reader[fieldIndex].ToString();
        }
    }
}
