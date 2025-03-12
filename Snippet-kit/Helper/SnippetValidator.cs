using SnippetIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper;

public class SnippetValidator
{
    public static string ValidateNewSnippet(List<CodeSnippet> CodeSnippets, CodeSnippet newSnippet)
    {
        if (CodeSnippets.Any(CodeSnippet => CodeSnippet.Id == newSnippet.Id))
            throw new Exception($"A CodeSnippet with the ID: {newSnippet.Id} already exists!");

        // Ensure KeyShortcut has the correct format
        newSnippet.KeyShortcut = ValidateUniqueShortcuts(CodeSnippets, newSnippet);

        return newSnippet.KeyShortcut;
    }
    public static string ValidateUniqueShortcuts(List<CodeSnippet> CodeSnippets, CodeSnippet newSnippet)
    {
        // Ensure KeyShortcut has the correct format
        newSnippet.KeyShortcut = Helper.SnippetIOUtils.NormalizeKeyShortcut(newSnippet.KeyShortcut);

        var existingSnippet = CodeSnippets.FirstOrDefault(codesnippet =>
            codesnippet.KeyShortcut == newSnippet.KeyShortcut &&
            codesnippet.WordShortcut == newSnippet.WordShortcut);

        if (existingSnippet != null)
        {
            throw new Exception($"KeyShortcut and WordShortcut '{existingSnippet.KeyShortcut}' " +
                                $"'{existingSnippet.WordShortcut}' already exists. Matching snippet ID: {existingSnippet.Id}");
        }

        return newSnippet.KeyShortcut;
    }    
}
