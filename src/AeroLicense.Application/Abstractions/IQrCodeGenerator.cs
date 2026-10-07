namespace AeroLicense.Application.Abstractions;

public interface IQrCodeGenerator
{
    byte[] GeneratePng(string text);
}
