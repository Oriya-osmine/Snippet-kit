using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SnippetIOApi;

public static class Factory
{
    public static ISnippetIO Get() => new SnippetIO.SnippetIO();

}
