namespace UnikraftScanner.Client.Symbols;

using System.Text.RegularExpressions;
using UnikraftScanner.Client.Helpers;
public partial class SymbolEngine
{

    public record ParseCoordsDTO(int StartLine, int StartLineEnd, int EndLine);
    private static ResultUnikraftScanner<ParseCoordsDTO> ParseBlockBounds(int startLine, int startLineEnd, int endLine, CompilationBlockTypes directiveType, string[] blockInfoLines, List<string> sourceLines)
    {
        // format is <ID NUMBER FOR ANY BLOCK ACCORDING TO CompilationBlockTypes.cs> IF/IFDEF/IFNDEF/ELIF/ELSE/ENDIF <start line> <start column>

        /*
        Rules:
        1. No multiline can split inside a directive name
        Ex:
        #if\
        def

        2. No multiline for #endif, if backslash appears just igonore it and further lines are counted as code lines since the plugin also cannot detect multiline endif

        3. multiline for if, ifdef, ifndef, else, elif can be done to split the expression or operands not directive name itself

        4. this method only finds the lines involved in the expressing the entire directive but advanced parsing is done in LexCondition
        */

        startLine = int.Parse(blockInfoLines[0].Split()[2]);


        // https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/14#issuecomment-3092359691
        // compiler may allow preprocessor directive names to be split on multiple lines, but I will not
        if (!Regex.Replace(sourceLines[startLine], @"#\s+", "#").Contains($"#{directiveType.ToString().ToLower()}"))

            return ResultUnikraftScanner<ParseCoordsDTO>.Failure(

                new ErrorUnikraftScanner<string>(
                    $"Full name of the directive needs to be placed at line {startLine} for directive {directiveType}",
                    ErrorTypes.WrongPreprocessorDirective
                )
            );

        if (directiveType == CompilationBlockTypes.IF)
        {

            startLineEnd = int.Parse(blockInfoLines[2].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }
        else if (new[] { CompilationBlockTypes.IFDEF, CompilationBlockTypes.IFNDEF }.Contains(directiveType))
        {
            // the plugin does not show the StartEndLine where the whole multiline expression ends thus we increment the line until we dont find any backslash
            // however keep in mind https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/14#issuecomment-3092380988
            // backslash cannot split through directive name or add multiple symbols for IFDEF and IFNDEF
            // further processing is done in LexCondition
            if (sourceLines[startLine].Contains('\\'))
            {
                int i;
                for (i = startLine + 1; sourceLines[i].Contains('\\'); i++) ;

                startLineEnd = i;
            }
            else
                startLineEnd = startLine;

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }

        // in order to solve https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/14#issuecomment-3094635526
        else if (directiveType == CompilationBlockTypes.ELSE)
        {
            startLineEnd = startLine;

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }

        else if (directiveType == CompilationBlockTypes.ELIF)
        {
            // very similar to #if
            startLineEnd = int.Parse(blockInfoLines[3].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }
        else if (directiveType == CompilationBlockTypes.ENDIF)
        {
            // start line and startLineEnd are populated by the first incomplete block from the stack that ends at this endif
            startLine = -1;
            startLineEnd = -1;

            // endif is not multiline
            // if backslash occurs just igore it and other lines after endif are counted as code line
            endLine = int.Parse(blockInfoLines[0].Split()[2]);

        }
        else
        {
            return ResultUnikraftScanner<ParseCoordsDTO>.Failure(

                new ErrorUnikraftScanner<string>(
                    $"Unknown directive type {directiveType} at start line {startLine}",
                    ErrorTypes.UnknowDirectiveName
                )
            );
        }

        return (ResultUnikraftScanner<ParseCoordsDTO>)new ParseCoordsDTO(startLine, startLineEnd, endLine);
    }
}
