namespace LicenseServer.Domain.Enums;

public enum LicenseType
{
    OrderLimit = 1,  // E.g. Free trial for first 100 orders
    TimeLimit = 2,   // E.g. Free trial for 30 days
    Lifetime = 3     // Unrestricted paid license
}
