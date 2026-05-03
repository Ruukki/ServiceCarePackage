namespace ServiceCarePackage.Models
{
    public sealed class SavedCommandData
    {
        public string CommandText { get; set; } = string.Empty;
        public bool AdvancedCommand { get; set; }
    }
}
