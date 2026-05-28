using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace VaR;

public class ConnectRdp : ICloneable, IEquatable<ConnectRdp>
{
    public ConnectRdp()
    {
        IPRDP = IPAddress.Loopback;
        DomainRDP = "";
        LoginRDP = "";
        PasswordRDP = "";
        IsAlive = false;
        IsConnected = false;
        CertHash = "";
    }
    public string IP
    {
        get => IPRDP.ToString();
        set => IPRDP = IPAddress.Parse(value);
    }
    public IPAddress IPRDP
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    public string DomainRDP
    {
        get; set;
    }
    public string LoginRDP
    {
        get; set;
    }
    public string PasswordRDP
    {
        get; set;
    }
    private bool isAlive;
    public bool IsAlive
    {
        get => isAlive;
        set
        {
            isAlive = value;
            IsAliveString = value ? " доступен" : " не доступен";
        }
    }
    public string IsAliveString
    {
        get; set;
    }
    private bool isConnected;
    public bool IsConnected
    {
        get => isConnected;
        set
        {
            isConnected = value;
            IsConnectedString = value ? "подключен" : "отключен";
        }
    }
    public string IsConnectedString
    {
        get; set;
    }
    private string certHash = "";
    public string CertHash
    {
        get => certHash;
        set
        {
            certHash = value;
            CertHashObject = HEXStringToByteArray(certHash.Replace(" ", ""));
        }
    }
    public byte[] CertHashObject
    {
        get; set;
    }
    private static byte[] HEXStringToByteArray(string hex)
    {
        return Enumerable.Range(0, hex.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            .ToArray();
    }
    public void ByteArrayToString(byte[] ba)
    {
        certHash = BitConverter.ToString(ba).Replace("-", " ").ToLower();
    }

    #region Cloneable, IEquatable
    public object Clone()
    {
        ConnectRdp c = new ConnectRdp
        {
            IPRDP = IPRDP,
            Name = Name,
            DomainRDP = DomainRDP,
            LoginRDP = LoginRDP,
            PasswordRDP = PasswordRDP,
            IsAlive = IsAlive,
            IsConnected = IsConnected,
            CertHash = CertHash,
            CertHashObject = CertHashObject
        };
        return c;
    }
    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        ConnectRdp other = (ConnectRdp)obj;
        return Equals(other);
    }
    public bool Equals(ConnectRdp other)
    {
        if (other is null) return false;
        if (!Equals(IPRDP, other.IPRDP)) return false;
        if (Name != other.Name) return false;
        if (DomainRDP != other.DomainRDP) return false;
        if (LoginRDP != other.LoginRDP) return false;
        if (PasswordRDP != other.PasswordRDP) return false;
        if (IsAlive != other.IsAlive) return false;
        if (IsConnected != other.IsConnected) return false;
        if (CertHash != other.CertHash) return false;
        if (CertHashObject != other.CertHashObject) return false;

        return true;
    }
    public override int GetHashCode()
    {
        int hashCode = 360553445;
        hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(IPRDP);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DomainRDP);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LoginRDP);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PasswordRDP);
        hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsAlive);
        hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsConnected);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CertHash);
        hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(CertHashObject);

        return hashCode;
    }
    public static bool operator ==(ConnectRdp left, ConnectRdp right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }
    public static bool operator !=(ConnectRdp left, ConnectRdp right)
    {
        return !(left == right);
    }
    #endregion
}
public static class Extensions
{
    public static IList<T> Clone<T>(this IList<T> source) where T : ICloneable
        => source.Select(item => (T)item.Clone()).ToList();
    
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the given value is
    /// null. Otherwise, return the value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="argName">
    /// The name of the argument
    /// </param>
    public static T ThrowIfNull<T>(this T value, string argName)
    {
        if (value == null)
            throw new ArgumentNullException(argName);
        return value;
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the value
    /// is null or an empty string. Otherwise, returns the value.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="argName"></param>
    public static string ThrowIfNullOrEmpty(this string value, string argName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value cannot be null or empty", argName);
        return value;
    }

    /// <summary>
    /// Perform an action for each item in the given collection. The item
    /// is the pass along the processing chain.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        collection = collection.ToList();

        foreach (T item in collection)
            action(item);

        return collection;
    }
}