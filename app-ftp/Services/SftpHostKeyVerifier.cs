using Renci.SshNet;
using Renci.SshNet.Common;

namespace app_ftp.Services;

internal static class SftpHostKeyVerifier
{
    public static void Attach(SftpClient client, string configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return;
        }

        var expected = HostKeyExpectation.Parse(configuredValue);
        client.HostKeyReceived += (_, e) => e.CanTrust = expected.Matches(e);
    }

    private sealed class HostKeyExpectation
    {
        public string? Algorithm { get; private init; }
        public int? KeyLength { get; private init; }
        public string? FingerprintMd5 { get; private init; }
        public string? FingerprintSha256 { get; private init; }

        public static HostKeyExpectation Parse(string value)
        {
            var trimmed = value.Trim();
            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? algorithm = null;
            int? keyLength = null;
            string? fingerprintMd5 = null;
            string? fingerprintSha256 = null;

            foreach (var token in tokens)
            {
                if (algorithm is null && token.Contains('-', StringComparison.Ordinal))
                {
                    algorithm = token;
                    continue;
                }

                if (!keyLength.HasValue && int.TryParse(token, out var parsedKeyLength))
                {
                    keyLength = parsedKeyLength;
                    continue;
                }

                if (token.StartsWith("SHA256:", StringComparison.OrdinalIgnoreCase))
                {
                    fingerprintSha256 = token["SHA256:".Length..].Trim();
                    continue;
                }

                if (token.Contains(':', StringComparison.Ordinal))
                {
                    fingerprintMd5 = token.Trim();
                    continue;
                }

                fingerprintSha256 ??= token.Trim();
            }

            if (string.IsNullOrWhiteSpace(fingerprintMd5) && string.IsNullOrWhiteSpace(fingerprintSha256))
            {
                throw new InvalidOperationException("La huella del host SFTP no tiene un formato valido.");
            }

            return new HostKeyExpectation
            {
                Algorithm = NormalizeAlgorithm(algorithm),
                KeyLength = keyLength,
                FingerprintMd5 = NormalizeMd5(fingerprintMd5),
                FingerprintSha256 = NormalizeSha256(fingerprintSha256)
            };
        }

        public bool Matches(HostKeyEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(Algorithm)
                && !string.Equals(NormalizeAlgorithm(args.HostKeyName), Algorithm, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (KeyLength.HasValue && args.KeyLength != KeyLength.Value)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(FingerprintMd5)
                && !string.Equals(NormalizeMd5(args.FingerPrintMD5), FingerprintMd5, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(FingerprintSha256)
                && !string.Equals(NormalizeSha256(args.FingerPrintSHA256), FingerprintSha256, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private static string? NormalizeMd5(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim()
                .Replace("MD5:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }

        private static string? NormalizeSha256(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim()
                .Replace("SHA256:", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimEnd('=');
        }

        private static string? NormalizeAlgorithm(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
