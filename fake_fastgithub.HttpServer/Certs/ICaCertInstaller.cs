namespace fake_fastgithub.HttpServer.Certs
{
    interface ICaCertInstaller
    {
        bool IsSupported();
        void Install(string certPath);
    }
}
