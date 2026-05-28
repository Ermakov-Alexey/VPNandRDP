using System;
using System.Globalization;

namespace VaR
{
    public class ModuleVersion : ICloneable, IComparable
    {
        public int Major { get; }

        public int Minor { get; }

        public int Build { get; }

        public int Revision { get; }

        public ModuleVersion()
        {
            Build = -1;
            Revision = -1;
            Major = 0;
            Minor = 0;
        }

        public ModuleVersion(string version)
        {
            Build = -1;
            Revision = -1;
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }
            //разобраться с NPOI 2.1

            char[] chArray1 = ['.'];
            string[] textArray1 = version.Split(chArray1);
            int num1 = textArray1.Length;
            if ((num1 < 2) || (num1 > 4))
            {
                throw new ArgumentException("Arg_VersionString");
            }
            Major = int.Parse(textArray1[0], CultureInfo.InvariantCulture);
            if (Major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), @"ArgumentOutOfRange_Version");
            }
            Minor = int.Parse(textArray1[1], CultureInfo.InvariantCulture);
            if (Minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(version), @"ArgumentOutOfRange_Version");
            }
            num1 -= 2;
            if (num1 > 0)
            {
                Build = int.Parse(textArray1[2], CultureInfo.InvariantCulture);
                if (Build < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(version), @"ArgumentOutOfRange_Version");
                }
                num1--;
                if (num1 > 0)
                {
                    Revision = int.Parse(textArray1[3], CultureInfo.InvariantCulture);
                    if (Revision < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(version), @"ArgumentOutOfRange_Version");
                    }
                }
            }
        }

        public ModuleVersion(int major, int minor)
        {
            Build = -1;
            Revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor), @"ArgumentOutOfRange_Version");
            }
            Major = major;
            Minor = minor;
            Major = major;
        }

        public ModuleVersion(int major, int minor, int build)
        {
            Build = -1;
            Revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor), @"ArgumentOutOfRange_Version");
            }
            if (build < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(build), @"ArgumentOutOfRange_Version");
            }
            Major = major;
            Minor = minor;
            Build = build;
        }

        public ModuleVersion(int major, int minor, int build, int revision)
        {
            Build = -1;
            Revision = -1;
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), @"ArgumentOutOfRange_Version");
            }
            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor), @"ArgumentOutOfRange_Version");
            }
            if (build < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(build), @"ArgumentOutOfRange_Version");
            }
            if (revision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(revision), @"ArgumentOutOfRange_Version");
            }
            Major = major;
            Minor = minor;
            Build = build;
            Revision = revision;
        }
        #region ICloneable Members

        public object Clone()
        {
            ModuleVersion version1 = new ModuleVersion(Major, Minor, Build, Revision);
            return version1;
        }
        #endregion
        #region IComparable Members

        public int CompareTo(object version)
        {
            if (version == null)
            {
                return 1;
            }
            if (!(version is ModuleVersion version1))
            {
                throw new ArgumentException("Arg_MustBeVersion");
            }

            if (Major != version1.Major)
            {
                if (Major > version1.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (Minor != version1.Minor)
            {
                if (Minor > version1.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (Build != version1.Build)
            {
                if (Build > version1.Build)
                {
                    return 1;
                }
                return -1;
            }
            if (Revision == version1.Revision)
            {
                return 0;
            }
            if (Revision > version1.Revision)
            {
                return 1;
            }
            return -1;
        }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is ModuleVersion version1))
            {
                return false;
            }

            if (Major == version1.Major && Minor == version1.Minor && Build == version1.Build && Revision == version1.Revision)
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int num1 = 0;
            num1 |= (Major & 15) << 0x1c;
            num1 |= (Minor & 0xff) << 20;
            num1 |= (Build & 0xff) << 12;
            return num1 | Revision & 0xfff;
        }

        public static bool operator ==(ModuleVersion v1, ModuleVersion v2)
        {
            return v1 != null && v1.Equals(v2);
        }

        public static bool operator >(ModuleVersion v1, ModuleVersion v2)
        {
            return v2 < v1;
        }

        public static bool operator >=(ModuleVersion v1, ModuleVersion v2)
        {
            return v2 <= v1;
        }

        public static bool operator !=(ModuleVersion v1, ModuleVersion v2)
        {
            if (v1 is null) return false;
            if (v2 is null) return false;
            //return (v1 != v2);
            return  !v1.Equals(v2);
        }

        public static bool operator <(ModuleVersion v1, ModuleVersion v2)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException(nameof(v1));
            }
            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(ModuleVersion v1, ModuleVersion v2)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException(nameof(v1));
            }
            return v1.CompareTo(v2) <= 0;
        }

        public static ModuleVersion operator ++(ModuleVersion v1)
        {
            if (v1 == null)
            {
                throw new ArgumentNullException(nameof(v1));
            }
            return new ModuleVersion(v1.Major, v1.Minor, v1.Build, v1.Revision + 1);
        }

        public override string ToString()
        {
            if (Build == -1)
            {
                return ToString(2);
            }
            if (Revision == -1)
            {
                return ToString(3);
            }
            return ToString(4);
        }

        public string ToString(int fieldCount)
        {
            object[] objArray1;
            switch (fieldCount)
            {
                case 0:
                {
                    return string.Empty;
                }
                case 1:
                {
                    return Major.ToString();
                }
                case 2:
                {
                    return $"{Major}.{Minor}";
                }
            }
            if (Build == -1)
            {
                throw new ArgumentException($@"ArgumentOutOfRange_Bounds_Lower_Upper {"0"},{"2"}", nameof(fieldCount));
            }
            if (fieldCount == 3)
            {
                objArray1 = [Major, ".", Minor, ".", Build];
                return string.Concat(objArray1);
            }
            if (Revision == -1)
            {
                throw new ArgumentException($@"ArgumentOutOfRange_Bounds_Lower_Upper {"0"},{"3"}", nameof(fieldCount));
            }
            if (fieldCount == 4)
            {
                objArray1 = [Major, ".", Minor, ".", Build, ".", Revision];
                return string.Concat(objArray1);
            }
            throw new ArgumentException($@"ArgumentOutOfRange_Bounds_Lower_Upper {"0"},{"4"}", nameof(fieldCount));
        }
    }
}
