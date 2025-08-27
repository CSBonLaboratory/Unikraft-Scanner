namespace UnikraftScanner.Client.Symbols;
using UnikraftScanner.Client.Helpers;
using System.IO;
using System.Text.RegularExpressions;

public partial class SymbolEngine
{
    public record EngineDTO(
        List<CompilationBlock> Blocks, int UniversalLinesOfCode,
        List<int> debugUniveralLinesOfCodeIdxs,  // used only for debugging in testing the lines of code
        List<List<int>> debugLinesOfCodeBlocks   // used only for debugging in testing the lines of code, decouple it from Compilation Blocks class
    );
    public SymbolEngine(){}

    // cache used in GetLinesOfCodeForBlock() to traverse blocks that have parent counter -1
    // an entry is added in this cache anytime a compilation block in created 
    // (is not required to have been closed prior - so it's still in the opened blocks stack)

    // since we add opened blocks where we did not yet find its ending, the list is in ascending order based on block counter, thus in 
    // [StartLineEnd, EndLine] interval
    // these intervals are also non-overlaping so no confusion in sorting
    // you can compare them to root nodes of a forest data structure
    private List<int> orphanBlocksCounterCache;

    private string RemoveComments(string sourceFileRawContent)
    {

        // C-comment remover got from https://gist.github.com/ChunMinChang/88bfa5842396c1fbbc5b
        string ReplaceWithWhiteSpace(Match comment)
        {

            string oldComment = comment.Value;

            if (oldComment.StartsWith('/'))
                return new string('\n', oldComment.Split('\n').Length - 1);

            return oldComment;
        }

        Regex commentRegex = new Regex(@"//.*?$|/\*.*?\*/|\'(?:\\.|[^\\\'])*\'|""(?:\\.|[^\\""])*""", RegexOptions.Singleline | RegexOptions.Multiline);

        return commentRegex.Replace(sourceFileRawContent, new MatchEvaluator(ReplaceWithWhiteSpace));
    }

    private static string LexCondition(CompilationBlockTypes directiveType, List<string> rawConditionLines)
    {
        /*
        Symbol conditions of the preprocessor directives have some rules applied to them in this order:
        
        - 1. new line character (\) followed by ending whitespaces must eliminate them (this is also a warning raised by compiler)
        - 2. be put on a single line so we need to remove the last backslash
        - 3. replace multiple whitespaces with only 1 whitespace
        - 4. replace "#     ....." with "#....."
        - 5. replace "<whitespace>#<directive name>" to "#<directive name>" so remove begining whitespaces before #
        - 6. remove any whitespaces from the end of string

        */
        string tokens = "";


        // eliminate trailing whitespace after a backslash and eliminate the backslash for concatenating all condition lines into a single line for viewing
        foreach (string line in rawConditionLines)
        {
            int possibleBackSlashEndPos = line.LastIndexOf("\\");

            if (possibleBackSlashEndPos == -1)
                tokens += line;  // 1
            else
                tokens += line.Substring(0, possibleBackSlashEndPos + 1).Replace("\\", "");   // 1 and 2
        }

        string ans =
        Regex.Replace(
            Regex.Replace(
                Regex.Replace(
                    Regex.Replace(
                        tokens,
                        @"\s{2,}",    // 3
                        " "
                    ),
                    @"#\s+",    // 4
                    "#"
                ),
                @"\s+#",    // 5
                "#"
            ),
            @"\s+$",  // 6
            ""
        );


        // used to solve this https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/14#issuecomment-3092380988

        if (directiveType == CompilationBlockTypes.IFDEF || directiveType == CompilationBlockTypes.IFNDEF)
        {
            string[] directiveAndSymbol = Regex.Split(ans, @"\s+").Take(2).ToArray();

            return $"{directiveAndSymbol[0]} {directiveAndSymbol[1]}";
        }
        
        return ans;

    }

    private void CountLinesOfCodeInSourceFile(List<string> sourceLines, List<CompilationBlock> foundBlocks, ref int universalLinesOfCode, List<int> debugUniversalLineIdxs, List<List<int>> debugLinesOfCodeBlocks)
    {

        // count lines if current block also has embeded children
        // a line of code for the current block is either from start of the current block until the first child,
        // or between children (gaps) (so it is not contained in any child)
        // or from last child until the end of the current block

        // add a fake compilation block that represents the whole source file
        CompilationBlock fakeWholeSourceRootBlock = new CompilationBlock(
            type: CompilationBlockTypes.ROOT,
            symbolCondition: "",
            startLine: 0,
            blockCounter: -1,
            startLineEnd: 0,
            endLine: sourceLines.Count,
            parentCounter: -1,
            lines: 0,
            children: orphanBlocksCounterCache  // dont forget to use incremented elements since this fake block will also be added to the block list
        );

        // we remove it when we finish counting lines of code
        foundBlocks.Insert(0, fakeWholeSourceRootBlock);

        // lines of code are between gaps enclosed in a block with no nested children, or between blocks of same rank (siblings)
        foreach (Gap gap in new GapSourceFileCollection(foundBlocks, this, sourceLines.Count))
        {
            int loc = 0;
            for (int i = gap.StartLine; i <= gap.EndLine; i++)
            {
                bool foundC = false;
                foreach (Char c in sourceLines[i])
                {
                    if (Char.IsWhiteSpace(c) == false)
                    {
                        foundC = true;
                        break;
                    }
                }

                if (foundC)
                {
                    loc++;
                    // debug purposes during test running
                    #if TEST_SYMBOL_CODE_LINES
                        if (gap.EnclosingBlockIdx == -1)
                            debugUniversalLineIdxs.Add(i);
                        else{
                            debugLinesOfCodeBlocks[gap.EnclosingBlockIdx].Add(i);
                        }
                    #endif
                }
            }

            // dont forget that we increment 1 since we added that fake root block
            // add new LoC statistics 
            foundBlocks[gap.EnclosingBlockIdx + 1].Lines += loc;
        }
        universalLinesOfCode = foundBlocks[0].Lines;
        foundBlocks.RemoveAt(0);
    }

    // public int[]? TriggerCompilationBlocks(List<CompilationBlock> discoveredBlocksOrdered, PluginOptions opts, string targetCompilationCommand)
    // {
    //     List<int> activeBlocksIdxs = new();

    //     string[] rawBlocks = new CompilerPlugin(targetCompilationCommand, opts).ExecutePlugin();

    //     Dictionary<int, bool> incompleteTriggeredBlocksStartLine = new();

    //     foreach (string block in rawBlocks)
    //     {
    //         string[] blockInfoLines = block.Split("\n");

    //         CompilationBlockTypes directiveType = (CompilationBlockTypes)int.Parse(blockInfoLines[0].Split()[0]);

    //         if (directiveType == CompilationBlockTypes.ENDIF || directiveType == CompilationBlockTypes.ELSE)
    //             continue;

    //         int startLine = int.Parse(blockInfoLines[0].Split()[2]);

    //         bool evalType = bool.Parse(blockInfoLines[^1].Split(" ")[1]);

    //         if (evalType == true)
    //         {
    //             incompleteTriggeredBlocksStartLine.Add(startLine, true);
    //         }
    //     }

    //     Queue<CompilationBlock> traverseNodes = new(discoveredBlocksOrdered.Where(cb => cb.ParentCounter == -1).ToList());

    //     while (traverseNodes.Count > 0)
    //     {
    //         CompilationBlock current 
    //     }

    // }
    public ResultUnikraftScanner<EngineDTO> DiscoverCompilationBlocksAndLines(string sourceFileAbsPath, PluginOptions opts, string targetCompilationCommand)
    {
        if (opts.Stage_RetainExcludedBlocks_Internal_PluginParam != PluginStage.Discovery)
        {
            return null;
        }
        List<CompilationBlock> foundBlocks = new();

        // since this method is used only once on a source file we need to reset the cache
        orphanBlocksCounterCache = new List<int>();

        // remove comments before executing the compiler in order to not count them as code lines
        List<string> sourceLines = RemoveComments(File.ReadAllText(sourceFileAbsPath)).Split("\n").ToList();

        // the decision of using the compiler in Discovery stage (first passing) or in Triggering stage (second passing) is done outside SymbolEngine 
        // in the CompilerOptions
        var compilerResult = new CompilerPlugin(targetCompilationCommand, opts).ExecutePlugin();

        if (!compilerResult.IsSuccess)
        {
            return ResultUnikraftScanner<EngineDTO>.Failure(compilerResult.Error);
        }

        string[] rawBlocks = compilerResult.Value;

        // we just add a blank line so that line indexes start from 1 just to match line numbers counted by the plugin (most IDEs count lines starting from 1)
        sourceLines.Insert(0, "");

        Stack<CompilationBlock> openedBlocks = new();

        int startLine = -1, startLineEnd = -1, endLine = -1, parentCounter = -1, blockCounter = 0;

        foreach (string block in rawBlocks)
        {
            // each info line in each block is on a particular line
            string[] blockInfoLines = block.Split("\n");

            CompilationBlockTypes directiveType = (CompilationBlockTypes)int.Parse(blockInfoLines[0].Split()[0]);

            // reset parent counter
            parentCounter = -1;

            ResultUnikraftScanner<ParseCoordsDTO> res =
                ParseBlockBounds(startLine, startLineEnd, endLine, directiveType, blockInfoLines, sourceLines);
            
                if (res.IsSuccess)
                {
                    startLine = res.Value.StartLine;
                    startLineEnd = res.Value.StartLineEnd;
                    endLine = res.Value.EndLine;
                }
                else
                {
                    return ResultUnikraftScanner<EngineDTO>.Failure(res.Error);
                }

            if (directiveType == CompilationBlockTypes.ENDIF)
                {

                    CompilationBlock endingBlock = openedBlocks.Pop();

                    endingBlock.EndLine = endLine;

                    foundBlocks.Add(endingBlock);
                }
                else if (new[] { CompilationBlockTypes.IF, CompilationBlockTypes.IFDEF, CompilationBlockTypes.IFNDEF }.Contains(directiveType))
                {

                    // see if current block has a parent and then link each other
                    if (openedBlocks.Count > 0)
                    {
                        CompilationBlock parentBlock = openedBlocks.Peek();

                        if (parentBlock.Children == null)
                            parentBlock.Children = new List<int>() { blockCounter };
                        else
                            parentBlock.Children.Add(blockCounter);


                        parentCounter = parentBlock.BlockCounter;

                    }

                    CompilationBlock currentBlock = new CompilationBlock(
                        type: directiveType,
                        condition: LexCondition(directiveType, sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                        startLine: startLine,
                        blockCounter: blockCounter,
                        startLineEnd: startLineEnd,
                        // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                        endLine: -1,
                        parentCounter: parentCounter);

                    openedBlocks.Push(currentBlock);

                    // this block would be a child of a root "fake" compilation block that contains the entire source file
                    if (parentCounter == -1)
                    {
                        orphanBlocksCounterCache.Add(blockCounter);
                    }

                    blockCounter++;

                }
                else if (new[] { CompilationBlockTypes.ELIF, CompilationBlockTypes.ELSE }.Contains(directiveType))
                {

                    // finalize processing previous branch block (a previous elif, if, ifndef, ifdef)
                    CompilationBlock siblingBlock = openedBlocks.Pop();

                    siblingBlock.EndLine = startLine;
                    foundBlocks.Add(siblingBlock);

                    // see if current block has a parent and then link each other
                    if (openedBlocks.Count > 0)
                    {
                        CompilationBlock parentBlock = openedBlocks.Peek();

                        if (parentBlock.Children == null)
                            parentBlock.Children = new List<int>() { blockCounter };
                        else
                            parentBlock.Children.Add(blockCounter);

                        parentCounter = parentBlock.BlockCounter;
                    }

                    CompilationBlock currentBlock = new CompilationBlock(
                        type: directiveType,
                        LexCondition(directiveType, sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                        startLine: startLine,
                        blockCounter: blockCounter,
                        startLineEnd: startLineEnd,
                        // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                        endLine: -1,
                        parentCounter: parentCounter
                    );
                    openedBlocks.Push(currentBlock);

                    // this block would be a child of a root "fake" compilation block that contains the entire source file
                    if (parentCounter == -1)
                    {
                        orphanBlocksCounterCache.Add(blockCounter);
                    }

                    blockCounter++;

                }

        }

        // very important to sort ascending based on discovery order (blockCounter) which means also based on the inteval [StartLine/StartLineEnd, FakeEndLine/EndLine]
        foundBlocks.Sort((x, y) => x.BlockCounter.CompareTo(y.BlockCounter));
        int universalLinesOfCode = 0;

        List<int> debugUniversalLinesOfCodeIdxs = new();
        List<List<int>> debugLinesOfCodeBlocks = new(foundBlocks.Count);
        for (int i = 0; i < foundBlocks.Count; i++)
        {
            debugLinesOfCodeBlocks.Add(new List<int>());
        }

        CountLinesOfCodeInSourceFile(sourceLines, foundBlocks, ref universalLinesOfCode, debugUniversalLinesOfCodeIdxs, debugLinesOfCodeBlocks);

        return (ResultUnikraftScanner<EngineDTO>)new EngineDTO(foundBlocks, universalLinesOfCode, debugUniversalLinesOfCodeIdxs, debugLinesOfCodeBlocks);
    }


    public static void Main(string[] args)
    {
        // int[,] x = new int[4, 5];

        // string[] y = args[1..1];
        // Dictionary<int[,], bool> d = new();
    }


}
