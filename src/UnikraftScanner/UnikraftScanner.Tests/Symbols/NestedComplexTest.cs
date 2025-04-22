namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;

public class SymbolEngineSimpleTests
{
    [Fact]
    public void FindCompilationBlocks_Nested(){
        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test6.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{
                new CompilationBlock(
                    symbolCondition : "#ifdef A",
                    startLine: 4,
                    startLineEnd: 4,
                    endLine: 5,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#elif B",
                    startLine: 5,
                    startLineEnd: 5,
                    endLine: 20,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 0,
                    children: new List<int>{2, 8, 9}),
                
                new CompilationBlock(
                    symbolCondition : "#ifndef E",
                    startLine: 6,
                    startLineEnd: 6,
                    endLine: 14,
                    blockCounter : 2,
                    parentCounter : 1,
                    lines: 0,
                    children: new List<int>{3, 4, 5}),

                new CompilationBlock(
                    symbolCondition : "#if defined(G)",
                    startLine: 7,
                    startLineEnd: 7,
                    endLine: 8,
                    blockCounter : 3,
                    parentCounter : 2,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#elif defined(H)",
                    startLine: 8,
                    startLineEnd: 8,
                    endLine: 9,
                    blockCounter : 4,
                    parentCounter : 2,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#else",
                    startLine: 9,
                    startLineEnd: 9,
                    endLine: 13,
                    blockCounter : 5,
                    parentCounter : 2,
                    lines: 0,
                    children: new List<int>{6, 7}),

                new CompilationBlock(
                    symbolCondition : "#ifdef I",
                    startLine: 10,
                    startLineEnd: 10,
                    endLine: 11,
                    blockCounter : 6,
                    parentCounter : 5,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#elif defined(J)",
                    startLine: 11,
                    startLineEnd: 11,
                    endLine: 12,
                    blockCounter : 7,
                    parentCounter : 5,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#ifdef A",
                    startLine: 15,
                    startLineEnd: 15,
                    endLine: 16,
                    blockCounter : 8,
                    parentCounter : 1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#ifndef B",
                    startLine: 18,
                    startLineEnd: 18,
                    endLine: 19,
                    blockCounter : 9,
                    parentCounter : 1,
                    lines: 0,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition : "#elif C",
                    startLine: 20,
                    startLineEnd: 20,
                    endLine: 22,
                    blockCounter : 10,
                    parentCounter : -1,
                    lines: 0,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition : "#elif D",
                    startLine: 22,
                    startLineEnd: 22,
                    endLine: 29,
                    blockCounter : 11,
                    parentCounter : -1,
                    lines: 0,
                    children: new List<int>{12, 13}),

                new CompilationBlock(
                    symbolCondition : "#if defined(K)",
                    startLine: 24,
                    startLineEnd: 24,
                    endLine: 25,
                    blockCounter : 12,
                    parentCounter : 11,
                    lines: 0,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition : "#ifdef L",
                    startLine: 27,
                    startLineEnd: 27,
                    endLine: 28,
                    blockCounter : 13,
                    parentCounter : 11,
                    lines: 0,
                    children: null),
            }
        );
    }

    [Fact]
    public void FindCompilationBlocks_Multiline_Directives_And_Code(){


        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "inputs/test7.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{

                new CompilationBlock(
                    symbolCondition : "#if defined(C) && defined(D)",
                    startLine: 2,
                    startLineEnd: 7,
                    endLine: 14,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    symbolCondition : "#ifdef C || D && A",
                    startLine: 20,
                    startLineEnd: 23,
                    endLine: 26,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),
            }
        );
    }

    [Fact]
    public void FindCompilationBlocks_Comments_MultiLine_Directive_In_Code(){
        
        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "inputs/test8.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{

                new CompilationBlock(
                    symbolCondition : "#ifdef B",
                    startLine: 16,
                    startLineEnd: 16,
                    endLine: 21,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),
            }
        );

    }

    
}
