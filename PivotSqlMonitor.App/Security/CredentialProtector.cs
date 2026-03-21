using System.Security.Cryptography;
using System.Text;

namespace PivotSqlMonitor.App.Security;

public static class CredentialProtector
{
    public static string Protect(string plainText, string entropy)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var entropyBytes = Encoding.UTF8.GetBytes(entropy);
        var protectedBytes = ProtectedData.Protect(bytes, entropyBytes, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string cipherTextBase64, string entropy)
    {
        var protectedBytes = Convert.FromBase64String(cipherTextBase64);
        var entropyBytes = Encoding.UTF8.GetBytes(entropy);
        var bytes = ProtectedData.Unprotect(protectedBytes, entropyBytes, DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(bytes);
    }
}
