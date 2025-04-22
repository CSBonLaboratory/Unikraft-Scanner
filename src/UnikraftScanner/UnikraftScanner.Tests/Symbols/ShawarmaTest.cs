namespace UnikraftScanner.Tests.Symbols;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;

public class SymbolEngineTests
{
    [Fact]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_OneLiners_NoPadding_NoElse(){

        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "inputs/test1.c"));


        Assert.Equal(
            res, 
            new List<CompilationBlock>{
                new CompilationBlock(
                    symbolCondition : "CONFIG_1",
                    startLine: 7,
                    startLineEnd: 7,
                    endLine: 10,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 2,
                    children: null),
                new CompilationBlock(
                    symbolCondition : "!(CONFIG_1)",
                    startLine: 12,
                    startLineEnd: 12,
                    endLine: 18,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 2, 
                    children: null),
                new CompilationBlock(
                    symbolCondition: "defined(CONFIG_1) && !defined(CONFIG_2)", 
                    startLine: 21,
                    startLineEnd: 21, 
                    endLine: 35,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 10,
                    children: null)
            }
        );
    }

    [Fact]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_OneLiners_NoPadding_WithElse(){

        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test2.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{
                new CompilationBlock(
                    symbolCondition: "!(CONFIG_1)",
                    startLine: 5,
                    startLineEnd: 5,
                    endLine: 12,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 6,
                    children: null
                ),
                new CompilationBlock(
                    symbolCondition: "CONFIG_1 || CONFIG_2",
                    startLine: 12,
                    startLineEnd: 12,
                    endLine: 17,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 4,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "!CONFIG_2",
                    startLine: 17,
                    startLineEnd: 17,
                    endLine: 19,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 1,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "!((CONFIG_1) || (CONFIG_1 || CONFIG_2) || (!CONFIG_2))",
                    startLine: 19,
                    startLineEnd: 19,
                    endLine: 25,
                    blockCounter: 3,
                    parentCounter: -1,
                    lines: 3,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "CONFIG_KVMPLAT",
                    startLine: 27,
                    startLineEnd: 27,
                    endLine: 29,
                    blockCounter: 4,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "!(!CONFIG_RUST)",
                    startLine: 29,
                    startLineEnd: 29,
                    endLine: 31,
                    blockCounter: 5,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "!((CONFIG_KVMPLAT) && !(!CONFIG_RUST))",
                    startLine: 31,
                    startLineEnd: 31,
                    endLine: 33,
                    blockCounter: 6,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "defined(CONFIG_UK)",
                    startLine: 35,
                    startLineEnd: 35,
                    endLine: 38,
                    blockCounter: 7,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "!defined(CONFIG_UK) || defined(CONFIG_RUST)",
                    startLine: 38,
                    startLineEnd: 38,
                    endLine: 40,
                    blockCounter: 8,
                    parentCounter: -1,
                    lines: 1,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "!((defined(CONFIG_UK)) || (!defined(CONFIG_UK) || defined(CONFIG_RUST)))",
                    startLine: 40,
                    startLineEnd: 40,
                    endLine: 44,
                    blockCounter: 9,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "!defined(CONFIG_LIB)",
                    startLine: 46,
                    startLineEnd: 46,
                    endLine: 51,
                    blockCounter: 10,
                    parentCounter: -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "!(!defined(CONFIG_LIB))",
                    startLine: 51,
                    startLineEnd: 51,
                    endLine: 53,
                    blockCounter: 11,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB",
                    startLine: 55,
                    startLineEnd: 55,
                    endLine: 59,
                    blockCounter: 12,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
            }
        );

    }
    [Fact]
    public void FindCompilationBlocks_AllTypes_Multiline_WithWhiteSpaces_NoNested_NoPadding(){
        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test3.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{
                
                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB1 && CONFIG_LIB2 && CONFIG_LIB3",
                    startLine: 1,
                    startLineEnd: 3,
                    endLine: 15,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 10,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB4 && CONFIG_LIB5",
                    startLine: 15,
                    startLineEnd: 17,
                    endLine: 22,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 4,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "!((CONFIG_LIB1 && CONFIG_LIB2 && CONFIG_LIB3) || (CONFIG_LIB4 && CONFIG_LIB5))",
                    startLine: 22,
                    startLineEnd: 3,
                    endLine: 26,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 3,
                    children: null)
            }
        );
    }
    [Fact]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_MultiLiners_MixPadding(){

        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test4.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{
                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB1 || CONFIG_LIB2 && CONFIG_LIB3",
                    startLine: 6,
                    startLineEnd: 8,
                    endLine: 14,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                    new CompilationBlock(
                    symbolCondition: "CONFIG_LIB2",
                    startLine: 14,
                    startLineEnd: 14,
                    endLine: 17,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
            }
        );
    }

    [Fact]
    public void FindCompilationBlocks_AllTypes_WithComments_Nested_Multiline_MixPadding(){
        var res = SymbolEngine.FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "test5.c"));

        Assert.Equal(
            res,
            new List<CompilationBlock>{
                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB1",
                    startLine: 9,
                    startLineEnd: 9,
                    endLine: 17,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB2",
                    startLine: 17,
                    startLineEnd: 17,
                    endLine: 44,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 1,
                    children: new List<int>{2, 3, 6}),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB3",
                    startLine: 20,
                    startLineEnd: 20,
                    endLine: 24,
                    blockCounter: 2,
                    parentCounter: 1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    symbolCondition: "defined(CONFIG_LIB4)",
                    startLine: 24,
                    startLineEnd: 24,
                    endLine: 41,
                    blockCounter: 3,
                    parentCounter: 1,
                    lines: 3,
                    children: new List<int>{4}),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB4 && CONFIG_LIB5",
                    startLine: 27,
                    startLineEnd: 29,
                    endLine: 35,
                    blockCounter: 4,
                    parentCounter: 3,
                    lines: 1,
                    children: new List<int>{5}),

                new CompilationBlock(
                    symbolCondition: "!((CONFIG_LIB3) || (CONFIG_LIB4 && CONFIG_LIB5))",
                    startLine: 41,
                    startLineEnd: 41,
                    endLine: 46,
                    blockCounter: 6,
                    parentCounter: 1,
                    lines: 1,
                    children: new List<int>{7}),

                new CompilationBlock(
                    symbolCondition: "CONFIG_LIB6",
                    startLine: 42,
                    startLineEnd: 42,
                    endLine: 44,
                    blockCounter: 7,
                    parentCounter: 6,
                    lines: 0,
                    children: null),
                
                new CompilationBlock(
                    symbolCondition: "defined(CONFIG_LIB7)",
                    startLine: 47,
                    startLineEnd: 47,
                    endLine: 58,
                    blockCounter: 8,
                    parentCounter: -1,
                    lines: 0,
                    children: new List<int>{9}),

                new CompilationBlock(
                    symbolCondition: "defined(CONFIG_LIB8)",
                    startLine: 48,
                    startLineEnd: 48,
                    endLine: 57,
                    blockCounter: 9,
                    parentCounter: 8,
                    lines: 1,
                    children: null),
            
            }
        );
    }
}
