using MudBlazor;

namespace PetAdoption.Web.BlazorApp.Helpers;

public static class PetDisplayHelpers
{
    public static Color PetStatusColor(string status) => status switch
    {
        "Available" => Color.Success,
        "Reserved" => Color.Warning,
        "Adopted" => Color.Info,
        _ => Color.Default
    };

    public static Color RequestStatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Approved" => Color.Success,
        "Rejected" => Color.Error,
        "Cancelled" => Color.Default,
        _ => Color.Default
    };

    public static string FormatAge(int months) =>
        months >= 12 ? $"{months / 12}y {months % 12}m" : $"{months}m";
}
