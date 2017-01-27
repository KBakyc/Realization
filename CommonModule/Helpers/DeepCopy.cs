
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CommonModule.Helpers
{

    public static class DeepCopy {

        /// <summary>
        /// Makes a deep copy of the specified object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToCopy">Object to make a deep copy of.</param>
        /// <returns>Deep copy of the object</returns>
        public static T Make<T>(T objectToCopy) where T : class {

            //if (objectToCopy == null) return null;

            using (MemoryStream ms = new MemoryStream()) {

                var bf = new BinaryFormatter();
                bf.Serialize(ms, objectToCopy);
                ms.Position = 0;
                return (T)bf.Deserialize(ms);
            }
        }
    }
}
