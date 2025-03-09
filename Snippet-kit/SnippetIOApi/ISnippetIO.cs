using SnippetIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnippetIOApi;

interface ISnippetIO
{
    void Create(CodeSnippet add);
    CodeSnippet Read(string id);
    void Update(CodeSnippet id);
    void Delete(string id);
}
