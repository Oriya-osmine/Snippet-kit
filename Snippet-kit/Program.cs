namespace Snippet_kit
{
    internal class Program
    {
        static readonly SnippetIOApi.ISnippetIO s_SnippetIO = SnippetIOApi.Factory.Get();

        static void Main(string[] args)
        {
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "1", Code = "Hello, World!", Shortcut = "" });
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "12", Code = "Hello, World!", Shortcut = "" });
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "13", Code = "Hello, Worlddsadadsadad!             ", Shortcut = "" });
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "14", Code = "Hello, Worlddsadadsadad!             ", Shortcut = "" });
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "15", Code = "Hello, Worlddsadadsadad!             ", Shortcut = "" });
            s_SnippetIO.Add(new SnippetIO.CodeSnippet { Id = "16", Code = "Hello, Worlddsadadsadad!             ", Shortcut = "" });


            Console.WriteLine(s_SnippetIO.Read("1"));
            Console.WriteLine(s_SnippetIO.Read("12"));
            Console.WriteLine(s_SnippetIO.Read("13"));
            s_SnippetIO.Delete("13");
            foreach (var snippet in s_SnippetIO.ReadAll())
            {
                Console.WriteLine(snippet.ToString());
            }

            s_SnippetIO.Update(new SnippetIO.CodeSnippet { Id = "1", Code = "blaa, World!", Shortcut = "" });
            foreach (var snippet in s_SnippetIO.ReadAll())
            {
                Console.WriteLine(snippet.ToString());
            }

        }
    }
}
