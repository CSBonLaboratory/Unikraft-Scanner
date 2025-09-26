namespace UnikraftScanner.Client.Symbols;

using UnikraftScanner.Client.Helpers;


internal class MinBlockIdxComparer : IComparer<int>
{
    public int Compare(int x, int y) => x < y ? -1 : (x > y ? 1 : 0);
}

public partial class SymbolEngine
{

    private IEnumerable<int> IterateFullbloodSibling(List<CompilationBlock> discoveredBlocksOrdered, int elseBlockIdx)
    {
        // yield the fullblood sibling block above him in the source file
        int currentFullbloodIdx = elseBlockIdx;
        int nextFullbloodIdx;

        while ((nextFullbloodIdx = SearchEndLine2Idx(discoveredBlocksOrdered[currentFullbloodIdx].StartLine, discoveredBlocksOrdered)) >= 0)
        {
            currentFullbloodIdx = nextFullbloodIdx;
            yield return nextFullbloodIdx;

        }

        yield break;

    }

    private bool IsAnyFullbloodSiblingTriggered(List<CompilationBlock> discoveredBlocksOrdered, int elseBlockIdx, HashSet<int> triggeredBlockIdxs)
    {
        // since ELSE blocks do not have their trigger state written by the compiler we must deduce ourselves
        // ELSE is triggered when no other direct sibling has been triggered
        // a direct sibling is any block that represents a conditional branch between an IF/IFNDEF/IFDEF and ENDIF (including the first branch too)
        // a block can have multiple such sets of direct siblings
        // a direct sibling is named here as "fullblood"
        // 2 blocks that have same parent block but are not part of fullblood are called halfblood
        foreach (int fullbloodSiblingIdx in IterateFullbloodSibling(discoveredBlocksOrdered, elseBlockIdx))
        {
            if (triggeredBlockIdxs.TryGetValue(fullbloodSiblingIdx, out _))
            {
                return true;
            }
        }

        return false;
    }

    private int BinarySearchStartLine2Idx(int startLine, List<CompilationBlock> discoveredBlocksOrdered)
    {

        int left = 0, right = discoveredBlocksOrdered.Count - 1, mid = left + (right - left) / 2;

        while (left <= right)
        {
            mid = left + (right - left) / 2;
            if (discoveredBlocksOrdered[mid].StartLine == startLine)
                return mid;
            else if (discoveredBlocksOrdered[mid].StartLine < startLine)
                left = mid + 1;
            else
                right = mid - 1;
        }
        
        return -1;
    }

    // we cannot do binary searhc on EndLine, the list of blocks is in ascending order only for StartLine and StartLineEnd
    // TODO: implement a variation of binary search for this too
    private int SearchEndLine2Idx(int endLine, List<CompilationBlock> discoveredBlocksOrdered)
    {

        for (int i = 0; i < discoveredBlocksOrdered.Count; i++)
        {
            if (discoveredBlocksOrdered[i].EndLine == endLine)
                return i;
        }

        return -1;
    }

    private void FindObviousTriggeredBlocks(List<CompilationBlock> discoveredBlocksOrdered, HashSet<int> triggeredBlockIdxs, string[] rawBlocks)
    {

        foreach (string block in rawBlocks)
        {
            // each info line in each block is on a particular line
            string[] blockInfoLines = block.Split("\n");

            CompilationBlockTypes currentType = (CompilationBlockTypes)int.Parse(blockInfoLines[0].Split()[0]);

            // ELSE and ENDIF blocks do not have eval info (if were triggered or not) from plugin
            // for ELSE blocks we find their evaluation status using a custom algo
            if (currentType != CompilationBlockTypes.ELSE && currentType != CompilationBlockTypes.ENDIF)
            {
                if (blockInfoLines[^1].Split()[1].ToLower().Equals("true"))
                {
                    var x = blockInfoLines[0].Split();
                    int searchedStartLine = int.Parse(blockInfoLines[0].Split()[2]);

                    // find triggered block based on start line
                    int idx = BinarySearchStartLine2Idx(searchedStartLine, discoveredBlocksOrdered);

                    triggeredBlockIdxs.Add(idx);
                }
            }
        }
    }

    public ResultUnikraftScanner<int[]> TriggerCompilationBlocks(List<CompilationBlock> discoveredBlocksOrdered, PluginOptions opts, string targetCompilationCommand)
    {
        HashSet<int> triggeredBlockIdxs = new();

        var compilerResult = new CompilerPlugin(targetCompilationCommand, opts).ExecutePlugin();

        if (!compilerResult.IsSuccess)
        {
            return ResultUnikraftScanner<int[]>.Failure(compilerResult.Error);
        }

        string[] rawBlocks = compilerResult.Value;

        // now triggeredBlockIdxs is sorted with indexes of blocks returned by the plugin results
        FindObviousTriggeredBlocks(discoveredBlocksOrdered, triggeredBlockIdxs, rawBlocks);

        foreach (int elseBlockIdx in this.elseBlocksCounterCache!)
        {
            int indexParent = -1;

            // prune ELSE blocks that are inside parent blocks that for sure cannot be compiled

            // if it is primordial ELSE (a compilation block with no parent then for sure it has a chance to be compiled)
            if (discoveredBlocksOrdered[elseBlockIdx].ParentCounter == -1)
                indexParent = Int32.MaxValue;
            // if it does not have a parent that is compiled go to next ELSE block in the cache
            else if (triggeredBlockIdxs.TryGetValue(discoveredBlocksOrdered[elseBlockIdx].ParentCounter, out _) == false)
                continue;
            else
                indexParent = discoveredBlocksOrdered[elseBlockIdx].ParentCounter;

            if (!IsAnyFullbloodSiblingTriggered(discoveredBlocksOrdered, elseBlockIdx, triggeredBlockIdxs) && indexParent >= 0)
            {
                // ELSE block is indeed triggered since no previous branch was
                triggeredBlockIdxs.Add(elseBlockIdx);
            }
        }

        var ans = triggeredBlockIdxs.ToList();
        ans.Sort((x, y) => x.CompareTo(y));

        return (ResultUnikraftScanner<int[]>)ans.ToArray();
    }
}
