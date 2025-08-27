namespace UnikraftScanner.Client.Symbols;

using UnikraftScanner.Client.Helpers;
using System.Collections;

public partial class SymbolEngine
{

    public record Gap(int EnclosingBlockIdx, int StartLine, int EndLine);
    public class GapSourceFileCollection : IEnumerable<Gap>
    {
        private readonly List<CompilationBlock> blocksAndFakeRoot;
        private readonly SymbolEngine engine;

        private readonly int sourceLinesNumber;

        public GapSourceFileCollection(List<CompilationBlock> blocksAndFakeRoot, SymbolEngine engine, int sourceLinesNumber)
        {
            this.blocksAndFakeRoot = blocksAndFakeRoot;
            this.engine = engine;
            this.sourceLinesNumber = sourceLinesNumber;
        }

        private Gap GetFirstChildGap(CompilationBlock currentBlock)
        {
            // gap between the current block end of starting statement and the start fo the first child
            // + 1 here is added because we also previously added the fake compilation block for the whole source file
            CompilationBlock firstChild = blocksAndFakeRoot[currentBlock.Children[0] + 1];

            // lines between current block and first child
            return new Gap(currentBlock.BlockCounter, currentBlock.StartLineEnd + 1, firstChild.StartLine - 1);
        }

        private Gap GetLastGap(CompilationBlock currentBlock)
        {
            // gap from last child until the end of the current block
            CompilationBlock lastChild = blocksAndFakeRoot[currentBlock.Children.Last() + 1];

            return new Gap(currentBlock.BlockCounter, lastChild.EndLine + 1, currentBlock.EndLine - 1);
        }
        public IEnumerator<Gap> GetEnumerator()
        {
            // generates the next gap where lines of code of a single block may exist
            // dont panic if the gap is not viable (start line is higher then end line) since consumer in SymbolEngine.cs will check it

            // corner case when we dont have any compilation blocks conditioned by preprocessor directives
            if (engine.orphanBlocksCounterCache.Count == 0)
            {
                // start from 1 not from 0 since we added an artificial line so that numbering starts from 1 as the plugin
                yield return new Gap(-1, 1, sourceLinesNumber - 1);

                yield break;
            }

            foreach (CompilationBlock currentBlock in blocksAndFakeRoot)
            {
                // if block does not have any embeded blocksAndFakeRoot just iterate through start-end
                if (currentBlock.Children == null || currentBlock.Children.Count == 0)
                {
                    yield return new Gap(currentBlock.BlockCounter, currentBlock.StartLineEnd + 1, currentBlock.EndLine - 1);
                }
                else
                {
                    // gap between current block starting line condition and the first child block
                    yield return GetFirstChildGap(currentBlock);

                    // find gaps between children for a current block that has nested blocks
                    for (int i = 0; i < currentBlock.Children.Count - 1; i++)
                    {
                        CompilationBlock childBlock = blocksAndFakeRoot[currentBlock.Children[i] + 1]; // we increment since we added that fake root block that will be later removed
                        CompilationBlock childNextBlock = blocksAndFakeRoot[currentBlock.Children[i + 1] + 1];

                        yield return new Gap(currentBlock.BlockCounter, childBlock.EndLine + 1, childNextBlock.StartLine - 1);
                    }

                    // gap between last child and the ending line of the current block
                    yield return GetLastGap(currentBlock);
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
