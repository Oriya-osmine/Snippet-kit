using SnippetIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnippetIOApi;

public interface ISnippetIO : IObservable
{
    void Add(CodeSnippet add);
    void AddList(IEnumerable<CodeSnippet> addList);
    CodeSnippet Read(string id);
    IEnumerable<CodeSnippet> ReadAll();
    void Update(CodeSnippet id);
    void Delete(string id);
}
