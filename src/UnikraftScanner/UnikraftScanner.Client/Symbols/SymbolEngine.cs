namespace UnikraftScanner.Client.Symbols;
using UnikraftScanner.Client.Helpers;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


internal class CompilerPlugin
{
    private ProcessStartInfo _plugin { get; init; }

    private PluginOptions _opts { get; init; }
    public CompilerPlugin(string fullCompilationCommand, PluginOptions opts)
    {
        // given the normal compilation command of a C-family source file, enrich it with clang plugin flags and prepare plugin process

        _plugin = new ProcessStartInfo(opts.CompilerPath);

        _opts = opts;

        _plugin.Arguments = fullCompilationCommand;

        // // inform clang that it needs to execute our plugin
        // // https://clang.llvm.org/docs/ClangPlugins.html
        // // plugin name is hardcoded here and in the plugin source code (BlockInterceptorPlugin.cxx) at static FrontendPluginRegistry decclaration
        _plugin.Arguments += " -Xclang -load";
        _plugin.Arguments += $" -Xclang {opts.PluginPath}";
        _plugin.Arguments += " -Xclang -plugin";
        _plugin.Arguments += $" -Xclang ConditionalBlockFinder";

        // // pass where to put results about intercepted plugin blocks after plugin execution
        _plugin.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        _plugin.Arguments += $" -Xclang {opts.InterceptionResultsFilePath_PluginParam}";

        // // should plugin parse conditional blocks inside a block that was evaluated as false
        // // for now, do not exclude them since we need to find all conditional blocks
        // // we will exclude them at the second stage when we need to find which ones are compiled (evaluated to true)
        _plugin.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        _plugin.Arguments += $" -Xclang {opts.RetainExcludedBlocks_PluginParam}";


        _plugin.CreateNoWindow = true;
        _plugin.RedirectStandardError = true;
        _plugin.RedirectStandardOutput = true;

        _plugin.UseShellExecute = false;

    }
    
    public string[] ExecutePlugin()
    {
        var generalInterceptor = Process.Start(_plugin);

        string interceptOut = generalInterceptor.StandardOutput.ReadToEnd();
        Console.WriteLine(generalInterceptor.StandardError.ReadToEnd());

        generalInterceptor.WaitForExit();

        string pluginResults = File.ReadAllText(_opts.InterceptionResultsFilePath_PluginParam);

        // blocks are split by an empty line
        return pluginResults.Split("\n\n").Where(e => e != "").ToArray();
    }
}
public class SymbolEngine
{
    public record EngineResult(List<CompilationBlock> Blocks, int UniversalLinesOfCode, List<int> debugUniveralLinesOfCodeIdxs);
    private static SymbolEngine? instance = null;
    private SymbolEngine() { }

    // cache used in GetLinesOfCodeForBlock() to traverse blocks that have parent counter -1
    // an entry is added in this cache anytime a compilation block in created 
    // (is not required to have been closed prior - so it's still in the opened blocks stack)

    // since we add opened blocks where we did not yet find its ending, the list is in ascending order based on block counter, thus in 
    // [StartLineEnd, EndLine] interval
    // these intervals are also non-overlaping so no confusion in sorting
    // you can compare them to root nodes of a forest data structure
    private List<int> orphanBlocksCounterCache;

    public static SymbolEngine GetInstance()
    {
        if (instance == null)
        {
            instance = new SymbolEngine();
        }

        return instance;
    }

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

    private static string GetCondition(List<string> rawConditionLines)
    {
        /*
        Symbol conditions of the preprocessor directives have some rules applied to them in this order:
        
        - 1. new line character (\) followed by ending whitespaces must eliminate them (this is also a warning raised by compiler)
        - 2. be put on a single line so we need to remove the last backslash
        - 3. replace multiple whitespaces with only 1 whitespace
        - 4. replace "#     ....." with "#....."
        - 5. replace "#<whitespace><directive name>" to "#<directive name>"
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
        return ans;

    }

    private static void ParseResultsPluginBlock(ref int startLine, ref int startLineEnd, ref int endLine, ref int fakeEndLine, ConditionalBlockTypes directiveType, string[] blockInfoLines, List<string> sourceLines)
    {

        startLine = int.Parse(blockInfoLines[0].Split()[2]);

        // very important to also compare the space since without it IFNDEF, IFDEF and ELIF can be also accepted on this branch
        // format is IF <start line> <start column>
        if (directiveType == ConditionalBlockTypes.IF)
        {

            startLineEnd = int.Parse(blockInfoLines[2].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;

            // only used for multine #endif
            fakeEndLine = -1;
        }
        else if (new[] { ConditionalBlockTypes.IFDEF, ConditionalBlockTypes.IFNDEF }.Contains(directiveType))
        {

            startLineEnd = startLine;

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;

            // only used for multine #endif
            fakeEndLine = -1;
        }

        else if (directiveType == ConditionalBlockTypes.ELSE)
        {
            // #else can be split multiline but the compiler plugin does not show the begin and end lines, so we need to iterate through the lines and find the end
            // if #else was forbidden from being split in multiline then it whould be treated the same as IFDEF and IFNDEF like in the previous branch
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

            // only used for multine #endif
            fakeEndLine = -1;
        }
        else if (directiveType == ConditionalBlockTypes.ELIF)
        {

            startLineEnd = int.Parse(blockInfoLines[3].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;

            // only used for multine #endif
            fakeEndLine = -1;

        }
        else if (directiveType == ConditionalBlockTypes.ENDIF)
        {
            // start line and startLineEnd are populated by the first incomplete block from the stack that ends at this endif
            startLine = -1;
            startLineEnd = -1;


            fakeEndLine = int.Parse(blockInfoLines[0].Split()[2]);

            int i;
            for (i = fakeEndLine; sourceLines[i].Contains('\\'); i++) ;

            endLine = i;
        }
        else
        {
            throw new Exception($" UNKNOWN DIRECTIVE TYPE: {directiveType}");
        }

    }

    private void CountLinesOfCodeInSourceFile(List<string> sourceLines, List<CompilationBlock> foundBlocks, ref int universalLinesOfCode, List<int> universalLineIdxs)
    {

        if (this.orphanBlocksCounterCache.Count == 0)
        {
            // dont forget ! we start from 1 because we also add a new line so that startLine/endLine should start at 1 like many IDEs
            for (int i = 1; i < sourceLines.Count; i++)
                if (FastFindCodeLine(sourceLines[i]))
                {
                    universalLinesOfCode++;

                    universalLineIdxs.Add(i);
                }
            return;
        }

        bool FastFindCodeLine(string line)
        {
            bool foundC = false;
            foreach (Char c in line)
                if (Char.IsWhiteSpace(c) == false)
                {
                    foundC = true;
                    break;
                }

            return foundC;
        }

        CompilationBlock fakeWholeSourceRootBlock = new CompilationBlock(
            type: ConditionalBlockTypes.ROOT,
            symbolCondition: "",
            startLine: 0,
            blockCounter: -1,
            startLineEnd: 0,
            fakeEndLine: sourceLines.Count,
            endLine: sourceLines.Count,
            parentCounter: -1,
            lines: 0,
            children: orphanBlocksCounterCache  // dont forget to use incremented elements since this fake block will also be added to the block list
        );

        foundBlocks.Insert(0, fakeWholeSourceRootBlock);

        // now count lines of code for all blocks
        // a line of code is counted only once in the most specific/precise/small block that contains it
        foreach (CompilationBlock currentBlock in foundBlocks)
        {
            // if block does not have any embeded blocks just iterate through start-end
            if (currentBlock.Children == null || currentBlock.Children.Count == 0)
            {
                for (int i = currentBlock.StartLineEnd + 1; i < currentBlock.FakeEndLine; i++)
                {
                    if (FastFindCodeLine(sourceLines[i]))
                    {
                        currentBlock.Lines++;
                        if (currentBlock.Equals(fakeWholeSourceRootBlock))
                            universalLineIdxs.Add(i);
                    }
                }
            }
            else
            {
                // count lines if current block also has embeded children
                // a line of code for the current block is either from start of the current block until the first child,
                // or between children (gaps) (so it is not contained in any child)
                // or from last child until the end of the current block

                CompilationBlock firstChild = foundBlocks[currentBlock.Children[0] + 1];

                // lines before first child
                for (int begin = currentBlock.StartLineEnd + 1; begin < firstChild.StartLine; begin++)
                    if (FastFindCodeLine(sourceLines[begin]))
                    {
                        currentBlock.Lines++;
                        if (currentBlock.Equals(fakeWholeSourceRootBlock))
                            universalLineIdxs.Add(begin);
                    }


                // lines between children
                for (int i = 0; i < currentBlock.Children.Count - 1; i++)
                {
                    CompilationBlock childBlock = foundBlocks[currentBlock.Children[i] + 1]; // we increment since we added that fake root block that will be later removed
                    CompilationBlock childNextBlock = foundBlocks[currentBlock.Children[i + 1] + 1];
                    for (int j = childBlock.EndLine + 1; j < childNextBlock.StartLine; j++)
                    {
                        if (FastFindCodeLine(sourceLines[j]))
                        {
                            currentBlock.Lines++;
                            if (currentBlock.Equals(fakeWholeSourceRootBlock))
                                universalLineIdxs.Add(j);
                        }
                    }

                }

                CompilationBlock lastChild = foundBlocks[currentBlock.Children.Last() + 1];

                // lines after last child
                for (int i = lastChild.EndLine + 1; i < currentBlock.FakeEndLine; i++)
                {
                    if (FastFindCodeLine(sourceLines[i]))
                    {
                        currentBlock.Lines++;
                        if (currentBlock.Equals(fakeWholeSourceRootBlock))
                            universalLineIdxs.Add(i);
                    }
                }
            }
        }

        universalLinesOfCode = foundBlocks[0].Lines;

        foundBlocks.RemoveAt(0);
    }

    public EngineResult FindCompilationBlocksAndLines(string sourceFileAbsPath, PluginOptions opts, string targetCompilationCommand)
    {

        List<CompilationBlock> foundBlocks = new();

        // since this method is used only once on a source file we need to reset the cache
        orphanBlocksCounterCache = new List<int>();

        // remove comments before executing the compiler in order to not count them as code lines
        List<string> sourceLines = RemoveComments(File.ReadAllText(sourceFileAbsPath)).Split("\n").ToList();

        // the decision of using the compiler in Discovery stage (first passing) or in Triggering stage (second passing) is done outside SymbolEngine
        string[] rawBlocks = new CompilerPlugin(targetCompilationCommand, opts).ExecutePlugin();

        // we just add a blank line so that line indexes start from 1 just to match line numbers counted by the plugin (most IDEs count lines starting from 1)
        sourceLines.Insert(0, "");

        Stack<CompilationBlock> openedBlocks = new();

        int startLine = -1, startLineEnd = -1, endLine = -1, parentCounter = -1, blockCounter = 0, fakeEndLine = -1;

        foreach (string block in rawBlocks)
        {
            // each info line in each block is on a particular line
            string[] blockInfoLines = block.Split("\n");

            ConditionalBlockTypes directiveType = (ConditionalBlockTypes)int.Parse(blockInfoLines[0].Split()[0]);

            // reset parent counter
            parentCounter = -1;

            ParseResultsPluginBlock(ref startLine, ref startLineEnd, ref endLine, ref fakeEndLine, directiveType, blockInfoLines, sourceLines);

            if (directiveType == ConditionalBlockTypes.ENDIF)
            {

                CompilationBlock endingBlock = openedBlocks.Pop();

                endingBlock.FakeEndLine = fakeEndLine;
                endingBlock.EndLine = endLine;

                foundBlocks.Add(endingBlock);
            }
            else if (new[] { ConditionalBlockTypes.IF, ConditionalBlockTypes.IFDEF, ConditionalBlockTypes.IFNDEF }.Contains(directiveType))
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
                    condition: GetCondition(sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                    startLine: startLine,
                    blockCounter: blockCounter,
                    startLineEnd: startLineEnd,
                    // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                    fakeEndLine: -1,
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
            else if (new[] { ConditionalBlockTypes.ELIF, ConditionalBlockTypes.ELSE }.Contains(directiveType))
            {

                // finalize processing previous branch block (a previous elif, if, ifndef, ifdef)
                CompilationBlock siblingBlock = openedBlocks.Pop();
                // fake end line only used when closing a block with an #endif
                siblingBlock.FakeEndLine = startLine;
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
                    GetCondition(sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                    startLine: startLine,
                    blockCounter: blockCounter,
                    startLineEnd: startLineEnd,
                    // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                    fakeEndLine: -1,
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

        // very important to sort ascending based on discovery order (blockCounter) which means also based on the inteval [StartLine/StartLineEnd, EndLine]
        foundBlocks.Sort((x, y) => x.BlockCounter.CompareTo(y.BlockCounter));
        int universalLinesOfCode = 0;
        List<int> debugUniversalLinesOfCodeIdxs = new();
        CountLinesOfCodeInSourceFile(sourceLines, foundBlocks, ref universalLinesOfCode, debugUniversalLinesOfCodeIdxs);

        return new EngineResult(foundBlocks, universalLinesOfCode, debugUniversalLinesOfCodeIdxs);
    }

    public static void Main(string[] args)
    {

    }


}
