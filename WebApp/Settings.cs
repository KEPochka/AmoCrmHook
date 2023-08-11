namespace WebApp
{
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// 
    /// </summary>
    public class Settings
    {
        public string Namespace { get; set; }

        public string ModelsPath { get; set; }

        public string AssemblyPath { get; set; }
    }

    #pragma warning restore CS8618
}
