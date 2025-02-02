﻿using Rubics.Code.Syntax;
using Rubics.Code;

namespace Repl;

public static class Program {

    private static void Main() {
        var showTrees = false;
        var variables = new Dictionary<VariableSymbol, object>();

        while (true) {

            ColorPrint("> ", ConsoleColor.Yellow);
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (line == "!exit") 
                return;
            
            if (line == "!clear") { 
                Console.Clear(); 
                continue; 
            }

            if (line == "!showTrees") {
                showTrees = !showTrees;
                ColorPrint($"INFO: showTrees set to: {showTrees}\n", ConsoleColor.DarkGray);
                continue;
            }

            var syntaxTree = SyntaxTree.Parse(line);
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(variables);

            if (showTrees)
                syntaxTree.Root.WriteTo(Console.Out);

            if (!result.Diagnostics.Any()) {
                ColorPrint($"{result.Result}\n", ConsoleColor.Blue);
            }
            else {
                var sourceText = syntaxTree.SourceText;
                foreach (var diagnostic in result.Diagnostics) {

                    var lineIndex = sourceText.GetLineIndex(diagnostic.Span.Start);
                    var lineNumber = lineIndex + 1;
                    var character = diagnostic.Span.Start - sourceText.Lines[lineIndex].Start + 1;

                    Console.WriteLine();
                    ColorPrint($"ERROR (line {lineNumber}, col {character}): ", ConsoleColor.Red);
                    ColorPrint($"{diagnostic}\n", ConsoleColor.Gray);

                    var prefix = line[..diagnostic.Span.Start];
                    var error = line.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
                    var suffix = line[diagnostic.Span.End..];

                    Console.Write($"\t->{prefix}");
                    ColorPrint(error, ConsoleColor.Red);
                    Console.Write($"{suffix}\n");
                }

                Console.WriteLine();
            }
        }
    }

    static void ColorPrint(string value, ConsoleColor color) {
        Console.ForegroundColor = color;
        Console.Write(value);
        Console.ResetColor();
    }
}
