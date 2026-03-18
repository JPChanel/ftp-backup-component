namespace app_ftp.Services;

public static class ByteSizeFormatter
{
    public static string Format(long bytes)
    {
        const long kb = 1024L;
        const long mb = kb * 1024L;
        const long gb = mb * 1024L;
        const long tb = gb * 1024L;

        return bytes switch
        {
            >= tb => $"{bytes / (double)tb:0.##} TB",
            >= gb => $"{bytes / (double)gb:0.##} GB",
            >= mb => $"{bytes / (double)mb:0.##} MB",
            >= kb => $"{bytes / (double)kb:0.##} KB",
            _ => $"{bytes} B"
        };
    }
}
