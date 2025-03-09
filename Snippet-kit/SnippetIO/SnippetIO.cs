﻿using SnippetIOApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Net.Http.Headers;
using static System.Net.WebRequestMethods;
namespace SnippetIO;


public class CodeSnippet
{
    public required string Id { get; init; }
    public required string Code { get; init; }
    public required string Shortcut { get; init; }

    internal static CodeSnippet GetCodeSnippet(XElement Snippet)
    {
        return new CodeSnippet()
        {
            Id = Snippet.Element("Id")?.Value ?? throw new Exception("null Id"),
            Code = Snippet.Element("Code")?.Value ?? "",
            Shortcut = Snippet.Element("Code")?.Value ?? ""

        };
    }
    internal static IEnumerable<XElement> GetCodeSnippetElement(CodeSnippet Snippet)
    {
        return new XElement[]
        {
        new XElement("Id", Snippet.Id),
        new XElement("Code", Snippet.Code),
        new XElement("Shortcut", Snippet.Shortcut)
        };
    }
    public override string ToString()
    {
        return $"Id: {Id}\nCode: {Code}";
    }
}


internal class SnippetIO : SnippetIOApi.ISnippetIO
{
    readonly static string Snippetsxml = "Snippets.xml";
    internal static SnippetIOApi.ObserverManager Observers = new(); //stage 5

    public void Create(CodeSnippet newSnippet)
    {
        List<CodeSnippet> CodeSnippets = ReadAll().ToList();
        if(CodeSnippets.Any(CodeSnippet => CodeSnippet.Id == newSnippet.Id))
            throw new Exception($"A CodeSnippet with the ID: {newSnippet.Id} already exists!");
        XElement CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(Snippetsxml);
        // Add new CodeSnippet element to the root
        CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(newSnippet)));
        // Save the updated XML
        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, Snippetsxml);
        Observers.NotifyListUpdated();
        Observers.NotifyItemUpdated(newSnippet.Id);
    }

    public CodeSnippet Read(string id)
    {
        XElement? SnippetElem = XMLTools.LoadListFromXMLElement(Snippetsxml)
    .Elements().FirstOrDefault(item => item.Element("Id")!.Value == id);
        return SnippetElem is null ? throw new Exception($"null value from id '{id}'") : CodeSnippet.GetCodeSnippet(SnippetElem);
    }

    public IEnumerable<CodeSnippet> ReadAll()
    {
        XElement root = XMLTools.LoadListFromXMLElement(Snippetsxml);
        IEnumerable<CodeSnippet> CodeSnippets = root.Elements("CodeSnippet").Select(CodeSnippet.GetCodeSnippet);
        return CodeSnippets; 
    }


    public void Update(CodeSnippet update)
    {
        XElement CodeSnippetsRootElem = XMLTools.LoadListFromXMLElement(Snippetsxml);

        (CodeSnippetsRootElem.Elements().FirstOrDefault(item => item.Element("Id")!.Value == update.Id) ??
            throw new Exception($"A CodeSnippet with the ID: {update.Id} does not exist!")).Remove();

        CodeSnippetsRootElem.Add(new XElement("CodeSnippet", CodeSnippet.GetCodeSnippetElement(update)));
        XMLTools.SaveListToXMLElement(CodeSnippetsRootElem, Snippetsxml);
        Observers.NotifyItemUpdated(update.Id);
        Observers.NotifyListUpdated();

    }

    public void Delete(string delete)
    {
        XElement root = XMLTools.LoadListFromXMLElement(Snippetsxml);

        XElement? CodeSnippetElement = root.Elements("CodeSnippet")
            .FirstOrDefault(CodeSnippet => CodeSnippet.Element("Id")!.Value == delete);

        if (CodeSnippetElement == null)
            throw new Exception($" with ID {delete} does not exist.");

        CodeSnippetElement.Remove();
        XMLTools.SaveListToXMLElement(root, Snippetsxml);
        Observers.NotifyItemUpdated(delete);
        Observers.NotifyListUpdated();

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
