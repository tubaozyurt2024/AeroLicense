namespace AeroLicense.Api;

public static class ApiRoutes
{
    // URL segmenti ile versiyonlama: en görünür ve cache/proxy dostu yöntem. v2 gelirse yeni controller'lar
    // eklenir, v1 istemcileri kırılmaz.
    public const string V1 = "api/v1";
}
