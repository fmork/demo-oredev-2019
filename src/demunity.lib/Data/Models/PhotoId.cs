using System;
using Newtonsoft.Json;

namespace demunity.lib.Data.Models
{
    [JsonObject(MemberSerialization.Fields)]
    public struct PhotoId
    {
        private Guid value;
        public const string Prefix = "photo";
        public PhotoId(Guid value)
        {
            this.value = value;
        }

        public Guid Value => value;

        public static implicit operator Guid(PhotoId input)
        {
            return input.value;
        }
        public static implicit operator PhotoId(Guid input)
        {
            return new PhotoId(input);
        }

        public string ToDbValue()
        {
            return $"{Prefix}|{value}";
        }

        public static PhotoId FromDbValue(string input)
        {
            var parts = input.Split('|');
            Guid idValue;
            if (!parts[0].Equals(Prefix, StringComparison.OrdinalIgnoreCase) || !Guid.TryParse(parts[1], out idValue))
            {
                throw new ArgumentException($"Value '{input}' is not of expected format.", nameof(input));
            }

            return new PhotoId(idValue);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is PhotoId || obj is Guid)
                ? Equals((PhotoId)obj)
                : false;
        }

        public bool Equals(PhotoId other)
        {
            return value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(PhotoId x, PhotoId y)
        {
            return x.Equals(y);
        }


        public static bool operator !=(PhotoId x, PhotoId y)
        {
            return !(x == y);
        }
    }
}