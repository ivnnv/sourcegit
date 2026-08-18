using System.Threading.Tasks;

namespace SourceGit.Commands
{
    // [fork:branch-diff] Resolves the default branch via refs/remotes/<remote>/HEAD
    public class QueryDefaultBranch : Command
    {
        public QueryDefaultBranch(string repo, string remote)
        {
            WorkingDirectory = repo;
            Context = repo;
            Args = $"symbolic-ref --short refs/remotes/{remote}/HEAD";
            RaiseError = false;
        }

        public string GetResult()
        {
            var rs = ReadToEnd();
            return rs.IsSuccess ? rs.StdOut.Trim() : string.Empty;
        }

        public async Task<string> GetResultAsync()
        {
            var rs = await ReadToEndAsync().ConfigureAwait(false);
            return rs.IsSuccess ? rs.StdOut.Trim() : string.Empty;
        }
    }
}
