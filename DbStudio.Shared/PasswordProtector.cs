using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordProtector {
  /// <summary>
  /// Encrypts the plain text password using DPAPI.
  /// </summary>
  /// <param name="password">The password to encrypt.</param>
  /// <returns>A Base64-encoded string representing the encrypted password.</returns>
  public static string EncryptPassword(string password) {
    // Convert the plain text password into a byte array.
    byte[] plainBytes = Encoding.UTF8.GetBytes(password);

    // Encrypt the data using the current user's credentials.
    // Use null for additional entropy; you can supply extra bytes for additional security if needed.
    byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

    // Convert the encrypted bytes to a Base64 string for storage.
    return Convert.ToBase64String(encryptedBytes);
  }

  /// <summary>
  /// Decrypts an encrypted password (in Base64) back to plain text.
  /// </summary>
  /// <param name="encryptedPassword">The Base64-encoded encrypted password.</param>
  /// <returns>The decrypted plain text password.</returns>
  public static string DecryptPassword(string encryptedPassword) {
    // Convert the Base64 string back to a byte array.
    byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);

    // Decrypt the data using the same scope.
    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

    // Convert the decrypted bytes back into a string.
    return Encoding.UTF8.GetString(decryptedBytes);
  }
}
