using System;
using System.Text.RegularExpressions;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Basic Fraction (Rational) numbers with features only needed to represent scale factors.
    /// </summary>
    public readonly struct Fraction : IEquatable<Fraction>, IComparable<Fraction>
    {
        public static Fraction Identity { get; } = new Fraction(1, 1);

        public uint Numerator { get; }
        public uint Denominator { get; }

        public Fraction(uint numerator, uint denominator)
        {
            if (denominator == 0)
            {
                throw new ArgumentException("Can't divide by zero.", nameof(denominator));
            }

            if (numerator == 0)
            {
                throw new ArgumentException("Zero is not supported by this implementation.", nameof(numerator));
            }

            var gcd = GreatestCommonDivisor(numerator, denominator);
            Numerator = numerator / gcd;
            Denominator = denominator / gcd;
        }

        public Fraction Inverse()
            => new Fraction(Denominator, Numerator);

        #region Parse

        private static readonly Regex PARSE_REGEX = new Regex(@"^([1-9][0-9]*)\/([1-9][0-9]*)$", RegexOptions.Compiled);

        public static bool TryParse(string str, out Fraction result)
        {
            var match = PARSE_REGEX.Match(str);
            if (!match.Success)
            {
                result = Identity;
                return false;
            }

            var numerator = uint.Parse(match.Groups[1].Value);
            var denominator = uint.Parse(match.Groups[2].Value);
            result = new Fraction(numerator, denominator);
            return true;
        }

        public static Fraction Parse(string str)
            => TryParse(str, out var result)
                ? result
                : throw new ArgumentException($"Could not parse the input \"{str}\".", nameof(str));

        #endregion

        #region Overrides, interface implementations

        public override string ToString()
            => $"{Numerator}/{Denominator}";

        public override bool Equals(object obj)
            => obj is Fraction fraction && Equals(fraction);

        public bool Equals(Fraction other)
            => Numerator == other.Numerator && Denominator == other.Denominator;

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = -1534900553;
                hashCode = hashCode * -1521134295 + Numerator.GetHashCode();
                hashCode = hashCode * -1521134295 + Denominator.GetHashCode();
                return hashCode;
            }
        }

        public int CompareTo(Fraction other)
            => (int) (Numerator * other.Denominator) - (int) (other.Numerator * Denominator);

        #endregion

        #region Equality operators

        public static bool operator ==(Fraction left, Fraction right)
            => left.Equals(right);

        public static bool operator !=(Fraction left, Fraction right)
            => !(left == right);

        #endregion

        #region Comparison operators

        public static bool operator <(Fraction left, Fraction right)
            => left.CompareTo(right) < 0;

        public static bool operator <=(Fraction left, Fraction right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(Fraction left, Fraction right)
            => left.CompareTo(right) > 0;

        public static bool operator >=(Fraction left, Fraction right)
            => left.CompareTo(right) >= 0;

        #endregion

        #region Scale operators

        public static Fraction operator *(Fraction left, Fraction right)
            => new Fraction(left.Numerator * right.Numerator, left.Denominator * right.Denominator);

        public static Fraction operator *(Fraction left, uint right)
            => new Fraction(left.Numerator * right, left.Denominator);

        public static Fraction operator *(uint left, Fraction right)
            => new Fraction(left * right.Numerator, right.Denominator);

        public static Fraction operator /(Fraction left, Fraction right)
            => new Fraction(left.Numerator * right.Denominator, left.Denominator * right.Numerator);

        public static Fraction operator /(Fraction left, uint right)
            => new Fraction(left.Numerator, left.Denominator * right);

        public static Fraction operator /(uint left, Fraction right)
            => new Fraction(left * right.Denominator, right.Numerator);

        #endregion

        #region Type conversion operators

        public static implicit operator double(Fraction fraction)
            => 1.0 * fraction.Numerator / fraction.Denominator;

        public static implicit operator float(Fraction fraction)
            => 1.0f * fraction.Numerator / fraction.Denominator;

        public static implicit operator Fraction(uint number)
            => new Fraction(number, 1u);

        public static implicit operator Fraction((uint numerator, uint demoninator) tuple)
            => new Fraction(tuple.numerator, tuple.demoninator);

        #endregion

        private static uint GreatestCommonDivisor(uint a, uint b)
            => (b != 0) ? GreatestCommonDivisor(b, a % b) : a;
    }
}