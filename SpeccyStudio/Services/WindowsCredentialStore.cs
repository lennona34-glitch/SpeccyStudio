using System.Runtime.InteropServices;
using System.Text;

namespace SpeccyStudio.Services;

public static class WindowsCredentialStore
{
    private const string TargetName = "SpeccyStudio/Gemini";
    private const string LegacyTargetName = "SpeccyStudio/OpenAI";
    private const uint CredTypeGeneric = 1;
    private const uint CredPersistLocalMachine = 2;

    public static bool HasApiKey =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")) ||
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_API_KEY")) ||
        ReadApiKey() is not null;

    public static string? ReadApiKey()
    {
        string? environmentKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentKey)) return environmentKey;
        environmentKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (!string.IsNullOrWhiteSpace(environmentKey)) return environmentKey;

        if (TryReadTarget(TargetName, out string? key)) return key;
        if (TryReadTarget(LegacyTargetName, out key)) return key;
        return null;
    }

    private static bool TryReadTarget(string target, out string? key)
    {
        key = null;
        if (!CredRead(target, CredTypeGeneric, 0, out IntPtr credentialPtr)) return false;
        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) return false;
            var bytes = new byte[credential.CredentialBlobSize];
            Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
            key = Encoding.UTF8.GetString(bytes);
            return true;
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public static void SaveApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Trim().Length < 15)
            throw new ArgumentException("That does not look like a complete Google Gemini API key.", nameof(apiKey));

        byte[] blob = Encoding.UTF8.GetBytes(apiKey.Trim());
        IntPtr blobPtr = Marshal.AllocCoTaskMem(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);
            var credential = new NativeCredential
            {
                Type = CredTypeGeneric,
                TargetName = TargetName,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPtr,
                Persist = CredPersistLocalMachine,
                UserName = Environment.UserName
            };
            if (!CredWrite(ref credential, 0))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeCoTaskMem(blobPtr);
            Array.Clear(blob);
        }
    }

    public static void DeleteApiKey()
    {
        CredDelete(TargetName, CredTypeGeneric, 0);
        CredDelete(LegacyTargetName, CredTypeGeneric, 0);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWrite([In] ref NativeCredential userCredential, uint flags);
    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);
    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDelete(string target, uint type, uint flags);
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree([In] IntPtr cred);
}
