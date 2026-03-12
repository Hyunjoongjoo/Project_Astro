public static class DBStatus
{
    public static bool IsUpdating { get; set; } = false;

    public static void Reset()
    {
        IsUpdating = false;
    }
}
