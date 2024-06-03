namespace fake_fastgithub
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("fake_fastgithub");
            var options = new WebApplicationOptions
            {
                Args = args,
            };
            CreateWebApplication(options).Run(/*singleton: true*/);
        }

        private static WebApplication CreateWebApplication(WebApplicationOptions options)
        {
            var builder = WebApplication.CreateBuilder(options);

            // TODO
            builder.ConfigureHost();
            builder.ConfigureWebHost();

            var app = builder.Build();
            app.MapGet("/", () => "Hello World!");

            return app;
        }
    }
}