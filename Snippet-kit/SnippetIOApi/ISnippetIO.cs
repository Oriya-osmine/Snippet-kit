using SnippetIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnippetIOApi;

public interface ISnippetIO
{
    void Create(CodeSnippet add);
    CodeSnippet Read(string id);
    IEnumerable<CodeSnippet> ReadAll();
    void Update(CodeSnippet id);
    void Delete(string id);
}
