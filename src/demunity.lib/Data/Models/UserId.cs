using System;
using Newtonsoft.Json;

namespace demunity.lib.Data.Models
{
    [JsonObject(MemberSerialization.Fields)]
    public struct UserId
    {
        private Guid value;
        public const string Prefix = "user";
        public UserId(Guid value)
        {
            this.value = value;
        }

        public Guid Value => value;

        public static implicit operator Guid(UserId input)
        {
            return input.value;
        }
        public static implicit operator UserId(Guid input)
        {
            return new UserId(input);
        }

        public string ToDbValue()
        {
            return $"{Prefix}|{value}";
        }

        public static UserId FromDbValue(string input)
        {
            var parts = input.Split('|');
            Guid idValue;
            if (!parts[0].Equals(Prefix, StringComparison.OrdinalIgnoreCase) || !Guid.TryParse(parts[1], out idValue))
            {
                throw new ArgumentException($"Value '{input}' is not of expected format.", nameof(input));
            }

            return new UserId(idValue);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            UserId other;
            Guid? nullableGuid;
            if (obj is Guid? && (nullableGuid = (Guid?)obj).HasValue)
            {
                other = nullableGuid.Value;
            }
            else if (obj is Guid)
            {
                other = (Guid)obj;
            }
            else if (!(obj is UserId))
            {
                return false;
            }
            else
            {
                other = (UserId)obj;
            }

            return Equals(other);
        }

        public bool Equals(UserId other)
        {
            return value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(UserId x, UserId y)
        {
            return x.Equals(y);
        }


        public static bool operator !=(UserId x, UserId y)
        {
            return !(x == y);
        }
    }
}