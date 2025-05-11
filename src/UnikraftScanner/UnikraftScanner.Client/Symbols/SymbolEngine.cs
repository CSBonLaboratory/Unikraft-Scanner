namespace UnikraftScanner.Client.Symbols;
using UnikraftScanner.Client.Helpers;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class SymbolEngine
{
    private static SymbolEngine? instance = null;
    private SymbolEngine(){}

    // cache used in GetLinesOfCodeForBlock() to traverse blocks that have parent counter -1
    // an entry is added in this cache anytime a compilation block in created 
    // (is not required to have been closed prior - so it's still in the opened blocks stack)

    // since we add opened blocks where we did not yet find its ending, the list is in ascending order based on block counter, thus in 
    // [StartLineEnd, EndLine] interval
    // these intervals are also non-overlaping so no confusion in sorting
    // you can compare them to root nodes of a forest data structure
    private List<int> orphanBlocksCounterCache;

    public static SymbolEngine GetInstance(){
        if(instance == null){
            instance = new SymbolEngine();
        }
        
        return instance;
    }
    
    private string RemoveComments(string AbsSourcePath){
        
        // C-comment remover got from https://gist.github.com/ChunMinChang/88bfa5842396c1fbbc5b
        string ReplaceWithWhiteSpace(Match comment){

            string oldComment = comment.Value;

            if(oldComment.StartsWith('/'))
                return new string('\n', oldComment.Split('\n').Length - 1);

            return oldComment;
        }

        string rawContent = File.ReadAllText(AbsSourcePath);

        Regex commentRegex = new Regex(@"//.*?$|/\*.*?\*/|\'(?:\\.|[^\\\'])*\'|""(?:\\.|[^\\""])*""", RegexOptions.Singleline | RegexOptions.Multiline);

        return commentRegex.Replace(rawContent, new MatchEvaluator(ReplaceWithWhiteSpace));
    }

    private static string GetCondition(List<string> rawConditionLines){
        
        List<string> tokens = new();

        foreach(string line in rawConditionLines){
            tokens = tokens.Concat(line.Replace("\\", "").Split().Where(e => e != "").ToList()).ToList();
        }
        return String.Join(" ", tokens);
    }

    private int GetLinesOfCodeForBlock(int startCodeLineIdx, List<string> rawCodeContentLinesSlice, List<CompilationBlock> orderedBlocks){
        /*
            Must have the list of blocks in ascending order by block counter since this function will traverse the vector of blocks
            like an interval tree in order to find the best block (or not) the current line from the source file is placed into

            We can omit building the tree, apply binary search on children instead of classic iteration (we gain log^2(N) instead of N in time complexity)
        */
        int ans = 0;

        // iterate through every line in the slice of a source file inside the block
        /// some lines may be code lines, whitespace lines or lines part of other compilation blocks representing children of the current one 
        for(int currentLineIdx = startCodeLineIdx; currentLineIdx < startCodeLineIdx + rawCodeContentLinesSlice.Count - 1; currentLineIdx++){
            
            int currentBlockIdx = -1;
            bool end = false;
            
            // traverse the tree of compilation blocks until you find the smalles interval [StartLineEnd + 1, EndLine - 1] that contains the line's index
            while(!end){
                
                List<int> searchDomain;
                // the line is obviously in the source file, meaning in the "fake" root compilation block that contains the whole source file
                if(currentBlockIdx == -1){
                    searchDomain = orphanBlocksCounterCache;
                }
                else{
                    searchDomain = orderedBlocks[currentBlockIdx].Children;
                }

                if(searchDomain == null || searchDomain.Count == 0){
                    end = true;
                    break;
                }
                
                // we do a binary search,
                //  if the line index based on location in the source file is maller than the lower bound of the middle child than go left
                // if the line index is inside the middle child interval than we go deeper in the tree
                // if the line index is higher than the upper bound we go right
                // if we did not find any, then stop search, this means that the line is not part of any child of the current node
                // so this line is part of the current node, we just need to check if it contains code or only whitespaces

                int leftChildIdx = 0;
                int rightChildIdx = searchDomain.Count;

                while(leftChildIdx < rightChildIdx){

                    int midChildIdx = (leftChildIdx + rightChildIdx) / 2;
                    
                    // some blocks might not have any code in them for example an #if and then on the next line #endif
                    if(currentLineIdx < orderedBlocks[searchDomain[midChildIdx]].StartLineEnd + 1){
                        rightChildIdx = midChildIdx;
                    }
                    else if(currentLineIdx == orderedBlocks[searchDomain[midChildIdx]].StartLineEnd + 1){
                        throw new Exception("Code line squized between non-existent interval");
                    }
                }
            }
        }
        
        // another reason to replace comments with whitespaces
        // if we persist comments these will be considered lines of code which is false
        foreach(string line in rawCodeContentLines){
            if(line.Split().Where(e => e != "").ToArray().Length != 0)
                ans++;
        }

        return ans;
    }

    private static void ParsePluginBlock(ref int startLine, ref int startLineEnd, ref int endLine, ConditionalBlockTypes directiveType, string[] blockInfoLines){
        
        startLine = int.Parse(blockInfoLines[0].Split()[2]);

        // very important to also compare the space since without it IFNDEF, IFDEF and ELIF can be also accepted on this branch
        // format is IF <start line> <start column>
        if(directiveType == ConditionalBlockTypes.IF){
            
            startLineEnd = int.Parse(blockInfoLines[2].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }
        else if(new[]{ConditionalBlockTypes.IFDEF, ConditionalBlockTypes.IFNDEF, ConditionalBlockTypes.ELSE}.Contains(directiveType)){
            
            startLineEnd = startLine;

            // same as IF
            endLine = -1;
        }
        else if(directiveType == ConditionalBlockTypes.ELIF){

            startLineEnd = int.Parse(blockInfoLines[3].Split()[1]);

        }
        else if(directiveType == ConditionalBlockTypes.ENDIF){
            startLine = -1;
            startLineEnd = -1;
            endLine = int.Parse(blockInfoLines[0].Split()[2]);
        }
        else{
            throw new Exception("UNKNOWN DIRECTIVE TYPE");
        }

    }

    private string[] ExecutePlugin(string sourceFileAbsPath, InterceptorOptions opts){
        ProcessStartInfo startInfo  = new ProcessStartInfo(opts.CompilerPath);
        //startInfo.FileName = opts.CompilerPath;

        // trigger plugin execution on tthe source file
        startInfo.Arguments = $" -I/usr/include {sourceFileAbsPath}";

        // // compilation flags used to ignore #include, #define, #line directives that may interfere with the exact locations of the conditional blocks
        startInfo.Arguments += " -Wextra -E -fdirectives-only -dD -P";

        // // inform clang that it needs to execute our plugin
        // // https://clang.llvm.org/docs/ClangPlugins.html
        // // plugin name is hardcoded here and in the plugin source code (BlockInterceptorPlugin.cxx)
        startInfo.Arguments += " -Xclang -load";
        startInfo.Arguments += $" -Xclang {opts.PluginPath}";
        startInfo.Arguments += " -Xclang -plugin";
        startInfo.Arguments += $" -Xclang ConditionalBlockFinder";

        // // pass where to put results about intercepted plugin blocks after plugin execution
        startInfo.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        startInfo.Arguments += $" -Xclang {opts.InterceptionResultsFilePath}";

        // // should plugin parse conditional blocks inside a block that was evaluated as false
        // // for now, do not exclude them since we need to find all conditional blocks
        // // we will exclude them at the second stage when we need to find which ones are compiled (evaluated to true)
        startInfo.Arguments += $" -Xclang -plugin-arg-ConditionalBlockFinder";
        startInfo.Arguments += $" -Xclang TRUE";
            

        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;

        startInfo.UseShellExecute = false;

        var generalInterceptor = Process.Start(startInfo);

        string interceptOut = generalInterceptor.StandardOutput.ReadToEnd();
        Console.WriteLine(generalInterceptor.StandardError.ReadToEnd());

        generalInterceptor.WaitForExit();

        string pluginResults = File.ReadAllText(opts.InterceptionResultsFilePath);
        
        // blocks are split by an empty line
        return pluginResults.Split("\n\n").Where(e => e != "").ToArray();
    }


    public (List<CompilationBlock>, CBTree) FindCompilationBlocksAndLines(string sourceFileAbsPath, InterceptorOptions opts){
        
        List<CompilationBlock> ans = new();

        // since this method is used only once on a source file we need to reset the cache
        orphanBlocksCounterCache = new List<int>();

        string[] rawBlocks = ExecutePlugin(sourceFileAbsPath, opts);

        List<string> sourceLines = File.ReadAllText(sourceFileAbsPath).Split("\n").ToList();
        // we just add a blank line so that line indexes start from 1 just as the plugin info when it comes to line numbers
        sourceLines.Insert(0, "");

        Stack<CompilationBlock> openedBlocks = new();

        int startLine = -1, startLineEnd = -1, endLine = -1, parentCounter = -1, blockCounter = 0;

        foreach(string block in rawBlocks){
            
            // each info line in each block is on a particular line
            string[] blockInfoLines = block.Split("\n");
            
            ConditionalBlockTypes directiveType = (ConditionalBlockTypes)int.Parse(blockInfoLines[0].Split()[0]);
            
            // reset parent counter
            parentCounter = -1;

            ParsePluginBlock(ref startLine, ref startLineEnd, ref endLine, directiveType, blockInfoLines);

            Console.WriteLine($"{startLine} {startLineEnd} {endLine}");

            if(directiveType == ConditionalBlockTypes.ENDIF){
                
                CompilationBlock endingBlock = openedBlocks.Pop();

                endingBlock.EndLine = endLine;
                endingBlock.Lines = GetLinesOfCodeForBlock(sourceLines.Slice(endingBlock.StartLineEnd + 1, endingBlock.EndLine - endingBlock.StartLineEnd - 1));

                ans.Add(endingBlock);
            }
            else if(new[]{ConditionalBlockTypes.IF, ConditionalBlockTypes.IFDEF, ConditionalBlockTypes.IFNDEF}.Contains(directiveType)){
                    
                // see if current block has a parent and then link each other
                if(openedBlocks.Count > 0){
                    CompilationBlock parentBlock = openedBlocks.Peek();

                    if(parentBlock.Children == null)
                        parentBlock.Children = new List<int>(){blockCounter};
                    else
                        parentBlock.Children.Add(blockCounter);


                    parentCounter = parentBlock.BlockCounter;
                    
                }
                    
                CompilationBlock currentBlock = new CompilationBlock(
                    condition: GetCondition(sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                    startLine: startLine,
                    blockCounter: blockCounter,
                    startLineEnd: startLineEnd,
                    // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                    endLine: -1,
                    parentCounter: parentCounter);
                  
                openedBlocks.Push(currentBlock);

                // this block would be a child of a root "fake" compilation block that contains the entire source file
                if(parentCounter == -1){
                    orphanBlocksCounterCache.Add(blockCounter);
                }

                blockCounter++;
                    
            }
            else if(new[]{ConditionalBlockTypes.ELIF, ConditionalBlockTypes.ELSE}.Contains(directiveType)){

                // finalize processing previous branch block (a previous elif, if, ifndef, ifdef)
                CompilationBlock siblingBlock = openedBlocks.Pop();
                siblingBlock.EndLine = startLine;
                siblingBlock.Lines = GetLinesOfCodeForBlock(sourceLines.Slice(siblingBlock.StartLineEnd + 1, siblingBlock.EndLine - siblingBlock.StartLineEnd - 1));
                ans.Add(siblingBlock);

                // see if current block has a parent and then link each other
                if(openedBlocks.Count > 0){
                    CompilationBlock parentBlock = openedBlocks.Peek();

                    if(parentBlock.Children == null)
                        parentBlock.Children = new List<int>(){blockCounter};
                    else
                        parentBlock.Children.Add(blockCounter);

                    parentCounter = parentBlock.BlockCounter;
                }

                CompilationBlock currentBlock = new CompilationBlock(
                        GetCondition(sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                        startLine: startLine,
                        blockCounter: blockCounter,
                        startLineEnd: startLineEnd,
                        // end line is incomplete, we will add it when we encounter the next sibling block (else, elif) or the end (endif)
                        endLine: -1,
                        parentCounter: parentCounter
                );
                openedBlocks.Push(currentBlock);

                // this block would be a child of a root "fake" compilation block that contains the entire source file
                if(parentCounter == -1){
                    orphanBlocksCounterCache.Add(blockCounter);
                }

                blockCounter++;
            
            }
            
        }

        return (ans, BuildCBTree(ans));
    }
    public static void Main(string[] args){

        string a = "                 ";
		
		//string[] b = "aaa   bbb c d ee  ".Split();
		//a += b;
		string[] x = a.Split(a).Where(e => e != "").ToArray();
		foreach(string y in x)
			Console.WriteLine(y);
		
		Console.WriteLine(x.Length);
        
        // string noCommentContent = this.RemoveComments(AbsSourcePath);
        // File.WriteAllText("/home/karakitay/Desktop/Unikraft-Scanner/src/UnikraftScanner/UnikraftScanner.Tests/Symbols/inputs/test6-out.c", noCommentContent);
        // string[] lines = this.RemoveComments(AbsSourcePath).Split("\n");

        // NextParser = UnknownParser.GetInstance();
        
        // UnparsedLines = new(lines.Length);
        // foreach(string l in lines)
        //     UnparsedLines.Enqueue(l);
        
        // // the unknown parser does nothing so we must firstly choose the right parser
        // NextParser = NextParser.DecideLookahead();

        // while(NextParser != EOFParser.GetInstance()){
        //     LineParser currentParser = NextParser;

        //     currentParser.ParseLine();
            
        //     NextParser = currentParser.DecideLookahead();

        // }

        // return ParsedBlocks;

    }

    
}
