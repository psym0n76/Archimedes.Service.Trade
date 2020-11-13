using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Archimedes.Service.Trade.Tests
{
    public class FileReader
    {
        public List<T> Reader<T>(string fileName)
        {
            try
            {
                var resource = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"Archimedes.Service.Trade.Tests.{fileName}.json");

                if (resource==null)
                {
                    Console.WriteLine($"Unable to find resource. Error: {fileName}");
                    return default;
                }

                using var reader = new StreamReader(resource);
                var result = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<T>>(result);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to read resource {fileName} {e.Message} {e.StackTrace}");
            }
        }
    }
}