using SnippetIOApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnippetIO;
using System;
using System.IO;
using System.Xml.Serialization;

public class CodeSnippet
{
    public required string Id { get; init; }

    [XmlElement(ElementName = "Code")]
    public required string Code { get; init; }
}


internal class SnippetIO : SnippetIOApi.ISnippetIO
{
    readonly static string Snippetsxml = "Snippets.xml";
    public void Create(CodeSnippet add)
    {
        var snippet = new CodeSnippet
        {
            Id = "1",
            Code = "int    i = 0;"  // Preserving the formatting
        };
        var xmlSerializer = new XmlSerializer(typeof(CodeSnippet));
        using (var writer = new StringWriter())
        {
            xmlSerializer.Serialize(writer, snippet);
            File.WriteAllText(Snippetsxml, writer.ToString());
        }


    }
    public CodeSnippet Read(string id)
    {
        // Read from XML
        string xmlRead = File.ReadAllText(Snippetsxml);
        using (var reader = new StringReader(xmlRead))
        {
            var deserializedSnippet = (CodeSnippet)xmlSerializer.Deserialize(reader);
            Console.WriteLine($"Id: {deserializedSnippet.Id}, Code: {deserializedSnippet.Code}");
        }
    }

    public void Update(CodeSnippet id)
    {
        throw new NotImplementedException();
    }

    public void Delete(string id)
    {
        throw new NotImplementedException();
    }

}
