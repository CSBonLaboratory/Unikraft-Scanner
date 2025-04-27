namespace UnikraftScanner.Client.Symbols;
using UnikraftScanner.Client.Helpers;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class SymbolEngine
{
    public int GlobalBlockCounter {get; set;}

    public int UniversalLines {get; set;}

    public int CurrentLineIdx {get; set;}

    public List<CompilationBlock> ParsedBlocks {get; set;}

    public Stack<CompilationBlock> NestedOpenBlocks {get; set;}

    private static SymbolEngine? instance = null;

    private SymbolEngine(){
        ParsedBlocks = new();
        NestedOpenBlocks = new();
        CurrentLineIdx = 1;
    }

    public static SymbolEngine GetInstance(InterceptorOptions opts){
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

    private static int GetLinesOfCodeForBlock(List<string> rawCodeContentLines){
        
        int ans = 0;

        foreach(string line in rawCodeContentLines){
            if(line.Split().Where(e => e != "").ToArray().Length != 0)
                ans++;
        }

        return ans;
    }

    private static void ParsePluginBlock(ref int startLine, ref int startLineEnd, ref int endLine, string directiveType, string[] blockInfoLines){
        
        startLine = int.Parse(blockInfoLines[0].Split()[1]);

        if(directiveType.Contains("IF")){
            
            startLineEnd = int.Parse(blockInfoLines[2].Split()[1]);

            // we are not in the state to find the end line of the block, it might be an elif, else or endif
            endLine = -1;
        }
        else if(directiveType.Contains("IFNDEF") || directiveType.Contains("IFDEF") || directiveType.Contains("ELSE")){
            
            startLineEnd = startLine;

            // same as IF
            endLine = -1;
        }
        else if(directiveType.Contains("ELIF")){

            startLineEnd = int.Parse(blockInfoLines[3].Split()[1]);

        }
        // ENDIF
        else{
            startLine = -1;
            startLineEnd = -1;
            endLine = int.Parse(blockInfoLines[0].Split()[1]);
        }

    }
    public static List<CompilationBlock> FindCompilationBlocksAndLines(string sourceFileAbsPath){
        
        try{
            Process generalInterceptor = new Process();
            generalInterceptor.StartInfo.FileName = "clang++-18";

            // trigger plugin execution on tthe source file
            generalInterceptor.StartInfo.ArgumentList.Add(sourceFileAbsPath);

            // compilation flags
            generalInterceptor.StartInfo.ArgumentList.Add("-Wextra -E -fdirectives-only -dD -P");

            // plugin flags
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang -load");
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang /home/karakitay/Desktop/Unikraft-Scanner/src/UnikraftScanner/UnikraftScanner.Client/Symbols/CompilationBlockInterceptor/compile_block_finder.so");
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang -plugin");
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang Order");
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang -plugin-arg-order");
            generalInterceptor.StartInfo.ArgumentList.Add("-Xclang /home/karakitay/Desktop/Unikraft-Scanner/src/UnikraftScanner/UnikraftScanner.Client/Symbols/CompilationBlockInterceptor/qq");
            

            generalInterceptor.StartInfo.CreateNoWindow = true;
            generalInterceptor.StartInfo.RedirectStandardError = true;
            generalInterceptor.StartInfo.RedirectStandardOutput = true;

            generalInterceptor.StartInfo.UseShellExecute = false;
            generalInterceptor.Start();

            string interceptOut = generalInterceptor.StandardOutput.ReadToEnd();
            string interceptErr = generalInterceptor.StandardError.ReadToEnd();

            generalInterceptor.WaitForExit();

        }catch(Exception e){
            Console.WriteLine(e.ToString());
        }

        string pluginResults = File.ReadAllText("/home/karakitay/Desktop/Unikraft-Scanner/src/UnikraftScanner/UnikraftScanner.Client/Symbols/CompilationBlockInterceptor/qq");
        
        string[] rawBlocks = pluginResults.Split("\n\n").Where(e => e != "").ToArray();

        List<string> sourceLines = File.ReadAllText(sourceFileAbsPath).Split("\n").ToList();
        // we just add a blank line so that line indexes start from 1 just as the plugin info when it comes to line numbers
        sourceLines.Insert(0, "");

        Stack<CompilationBlock> openedBlocks = new();

        int startLine = -1, startLineEnd = -1, endLine = -1, parentCounter = -1, blockCounter = -1;

        foreach(string block in rawBlocks){

            string[] blockInfoLines = block.Split("\n");
            
            string directiveType = blockInfoLines[0].Split()[0];

            blockCounter++;

            // reset parent counter
            parentCounter = -1;

            ParsePluginBlock(ref startLine, ref startLineEnd, ref endLine, directiveType, blockInfoLines);

            if(directiveType.Contains("ENDIF")){
                
                CompilationBlock endingBlock = openedBlocks.Pop();

                endingBlock.EndLine = int.Parse(blockInfoLines[0].Split()[1]);
                endingBlock.Lines = GetLinesOfCodeForBlock(sourceLines.Slice(endingBlock.StartLine, endingBlock.EndLine));

                if(endingBlock.StartLine != int.Parse(blockInfoLines[1].Split()[1])){
                    
                }
            }
            else if(directiveType.Contains("IF") || directiveType.Contains("IFNDEF") || directiveType.Contains("IFDEF")){
                    
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
                        endLine: int.Parse(blockInfoLines[2].Split()[1]),
                        parentCounter);
                    
                    openedBlocks.Push(currentBlock);
                    
                }
            else if(directiveType.Contains("ELIF")){

                // finalize processing previous branch block
                CompilationBlock siblingBlock = openedBlocks.Pop();
                siblingBlock.EndLine = startLine;
                siblingBlock.Lines = GetLinesOfCodeForBlock(sourceLines.Slice(siblingBlock.StartLineEnd, siblingBlock.EndLine - siblingBlock.StartLineEnd));

                CompilationBlock parentBlock = openedBlocks.Peek();

                if(parentBlock.Children == null)
                        parentBlock.Children = new List<int>(){blockCounter};
                else
                    parentBlock.Children.Add(blockCounter);

                parentCounter = parentBlock.BlockCounter;

                openedBlocks.Push(
                    new CompilationBlock(
                        GetCondition(sourceLines.Slice(startLine, startLineEnd - startLine + 1)),
                        startLine: startLine,
                        blockCounter: blockCounter,
                        startLineEnd: startLineEnd,
                        endLine: int.Parse(blockInfoLines[3].Split()[1]),
                        parentCounter: parentCounter
                    )
                );


            }
            
        }
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
