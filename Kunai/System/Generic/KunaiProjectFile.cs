using Kunai.Generic;
using Newtonsoft.Json;
using SharpNeedle.Framework.Ninja.Csd;
using System.Collections.Generic;
using System.IO;

namespace Kunai
{
    public class KunaiProjectFile
    {     
        public string Name;
        public TextureFormat TextureFormat;
        public uint Field08;
        public uint Field0C;
        public List<KunSceneNode> Nodes = new List<KunSceneNode>();
        public List<KunFont> Fonts = new List<KunFont>();
        public List<KunTexture> Texture = new List<KunTexture>();
       
        public void Write(string in_Path)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            for (int i = 0; i < Texture.Count; i++)
            {
                if (Texture[i].TextureData.Length > 0)
                {
                    if (string.IsNullOrEmpty(Texture[i].Name))
                    {
                        var tex = Texture[i];
                        tex.Name = $"tex_{i}.dds";
                        Texture[i] = tex;
                    }
                }
            }
            string output = JsonConvert.SerializeObject(this, settings);
            File.WriteAllText(in_Path, output);
            //XmlSerializer xsSubmit = new XmlSerializer(typeof(KunaiProjectFile));
            //var xml = "";
            //var settings = new XmlWriterSettings();
            //settings.Indent = true;
            //using (var sww = new StringWriter())
            //{
            //    using (XmlWriter writer = XmlWriter.Create(sww, settings))
            //    {
            //        xsSubmit.Serialize(writer, this);
            //        xml = sww.ToString(); // Your XML
            //    }
            //}

        }
    }
}
