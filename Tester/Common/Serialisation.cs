using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tester.Common
{
    public static class Serialisation
    {
        /// <summary>
        /// This will never return null, it will throw an exception if unable to return data of the specified type (this includes any io issues).
        /// </summary>
        public static T ReadFromDisk<T>(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            using (var stream = File.Open(file.FullName, FileMode.Open))
            {
                var data = new BinaryFormatter().Deserialize(stream);
                if (data == null)
                    throw new Exception("Data represents null");
                if (!(data is T))
                    throw new Exception("Data does not represent required type: " + typeof(T) + " (" + data.GetType());
                return (T)data;
            }
        }

        /// <summary>
        /// This will throw an exception for null inputs or if unable to perform the specified action
        /// </summary>
        public static void WriteToDisk(FileInfo file, object data)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (data == null)
                throw new ArgumentNullException("data");

            using (var stream = File.Open(file.FullName, FileMode.OpenOrCreate))
            {
                new BinaryFormatter().Serialize(stream, data);
            }
        }
    }
}
