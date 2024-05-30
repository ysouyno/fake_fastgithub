namespace fake_fastgithub.Configuration
{
    public class FakeFastGithubException : Exception
    {
        public FakeFastGithubException(string message) : base(message) { }

        public FakeFastGithubException(string message, Exception? inner_exception)
            : base(message, inner_exception) { }
    }
}
