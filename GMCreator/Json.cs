using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GMCreator
{
    static class Json
    {
        public static T Parse<T>(string json)
        {
            JsonSerializerSettings sets = new JsonSerializerSettings();
            sets.CheckAdditionalContent = false;
            sets.MissingMemberHandling = MissingMemberHandling.Ignore;
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string Serialize<T>(T item)
        {
            return JsonConvert.SerializeObject(item);
        }
    }
}
