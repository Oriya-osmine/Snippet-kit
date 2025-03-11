using Helper;
using System.Xml.Linq;
namespace SnippetIO;


public class CodeSnippet
{
    public required string Id { get; set; }
    public required string Code { get; set; }
    public required string KeyShortcut { get; set; }

    public required string WordShortcut { get; set; }

    internal static CodeSnippet GetCodeSnippet(XElement Snippet)
    {
        return new CodeSnippet()
        {
            Id = Snippet.Element("Id")?.Value ?? throw new Exception("null Id"),
            Code = Snippet.Element("Code")?.Value ?? "",
            KeyShortcut = Snippet.Element("KeyShortcut")?.Value ?? "",
            WordShortcut = Snippet.Element("WordShortcut")?.Value ?? ""
        };
    }
    internal static IEnumerable<XElement> GetCodeSnippetElement(CodeSnippet Snippet)
    {
        return new XElement[]
        {
        new("Id", Snippet.Id),
        new("Code", Snippet.Code),
        new("KeyShortcut", Snippet.KeyShortcut),
        new("WordShortcut", Snippet.WordShortcut)
        };
    }
    public override string ToString()
    {
        return $"Id: {Id}\nCode: {Code}";
    }
}


internal class SnippetIO : SnippetIOApi.ISnippetIO
{
    readonly static string s_Snippetsxml = "Snippets.xml";
    internal static SnippetIOApi.ObserverManager Observers = new(); //stage 5

    public void Add(CodeSnippet newSnippet)
    {
        if(string.IsNullOrEmpty(newSnippet.Id))
            throw new Exception("Id cannot be null or empty");
        List<CodeSnippet> CodeSnippets = ReadAll().ToList();

        if (CodeSnippets.Any(CodeSnippet => CodeSnippet.Id == newSnippet.Id))
            throw new Exception($"A CodeSnippet with the ID: {newSnippet.Id} already exists!");

        Helper.SnippetIOUtil.ValidKeyShortcuts(newSnippet.KeyShortcut);
        XElement CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(s_Snippetsxml);

        CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(newSnippet)));

        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, s_Snippetsxml);

        Observers.NotifyListUpdated();
        Observers.NotifyItemUpdated(newSnippet.Id);
    }
    public void AddList(IEnumerable<CodeSnippet> addList)
    {
        XElement? CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(s_Snippetsxml);

        List<CodeSnippet> existingSnippets = ReadAll().ToList();

        foreach (var newSnippet in addList)
        {
            Helper.SnippetIOUtil.ValidKeyShortcuts(newSnippet.KeyShortcut);
            if (string.IsNullOrEmpty(newSnippet.Id))
                throw new Exception("Id cannot be null or empty");
            var existingSnippetElement = CodeSnippetsRootElem
                .Elements("CodeSnippet")
                .FirstOrDefault(x => x.Element("Id")?.Value == newSnippet.Id.ToString());

            // If the snippet does not already exist, add it
            if (existingSnippetElement == null)
            {
                CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(newSnippet)));
            }
        }

        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, s_Snippetsxml);

        Observers.NotifyListUpdated();
    }



    public CodeSnippet Read(string id)
    {
        XElement? SnippetElem = XMLTools.LoadListFromXMLElement(s_Snippetsxml)
    .Elements().FirstOrDefault(item => item.Element("Id")!.Value == id);
        return SnippetElem is null ? throw new Exception($"null value from id '{id}'") : CodeSnippet.GetCodeSnippet(SnippetElem);
    }

    public IEnumerable<CodeSnippet> ReadAll()
    {
        XElement root = XMLTools.LoadListFromXMLElement(s_Snippetsxml);
        IEnumerable<CodeSnippet> CodeSnippets = root.Elements("CodeSnippet").Select(CodeSnippet.GetCodeSnippet);
        if(!CodeSnippets.Any())
            return new List<CodeSnippet> { new() { Id = "New snippet", Code = "", KeyShortcut = "", WordShortcut = "" } };
        return CodeSnippets;
    }


    public void Update(CodeSnippet update)
    {
        Helper.SnippetIOUtil.ValidKeyShortcuts(update.KeyShortcut);

        XElement CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(s_Snippetsxml);

        (CodeSnippetsRootElem.Elements().FirstOrDefault(item => item.Element("Id")!.Value == update.Id) ??
            throw new Exception($"A CodeSnippet with the ID: {update.Id} does not exist!")).Remove();

        CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(update)));
        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, s_Snippetsxml);

    }
    public void UpdateAll(IEnumerable<CodeSnippet> updateList)
    {
        XElement? CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(s_Snippetsxml);

        List<CodeSnippet> existingSnippets = ReadAll().ToList();

        foreach (var newSnippet in updateList)
        {
            Helper.SnippetIOUtil.ValidKeyShortcuts(newSnippet.KeyShortcut);

            var existingSnippetElement = CodeSnippetsRootElem
                .Elements("CodeSnippet")
                .FirstOrDefault(x => x.Element("Id")?.Value == newSnippet.Id.ToString());

            if (existingSnippetElement != null)
            {
                existingSnippetElement.Remove();
            }

            CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(newSnippet)));
        }

        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, s_Snippetsxml);
    }

    public void Delete(string delete)
    {
        XElement root = XMLTools.LoadListFromXMLElement(s_Snippetsxml);

        XElement? CodeSnippetElement = root.Elements("CodeSnippet")
            .FirstOrDefault(CodeSnippet => CodeSnippet.Element("Id")!.Value == delete) ?? throw new Exception($" with ID {delete} does not exist.");
        CodeSnippetElement.Remove();
        XMLTools.SaveListToXMLElement(root, s_Snippetsxml);
        Observers.NotifyItemUpdated(delete);
        Observers.NotifyListUpdated();

    }
    public void DeleteAll()
    {
        XMLTools.SaveListToXMLSerializer(new List<CodeSnippet>(), s_Snippetsxml);
    }
    #region Observers
    public void AddObserver(Action listObserver) =>
        Observers.AddListObserver(listObserver);
    public void AddObserver(string id, Action observer) =>
        Observers.AddObserver(id, observer);
    public void RemoveObserver(Action listObserver) =>
        Observers.RemoveListObserver(listObserver);
    public void RemoveObserver(string id, Action observer) =>
        Observers.RemoveObserver(id, observer);
    #endregion Observers
}
