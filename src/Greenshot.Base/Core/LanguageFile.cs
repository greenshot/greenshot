using System;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// This class contains the information about a language file
    /// </summary>
    public class LanguageFile : IEquatable<LanguageFile>
    {
        public string Description { get; set; }

        public string Ietf { get; set; }

        public Version Version { get; set; }

        public string LanguageGroup { get; set; }

        public string Filepath { get; set; }

        public string Prefix { get; set; }

        /// <summary>
        /// Overload equals so we can delete a entry from a collection
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(LanguageFile other)
        {
            if (Prefix != null)
            {
                if (other != null && !Prefix.Equals(other.Prefix))
                {
                    return false;
                }
            }
            else if (other?.Prefix != null)
            {
                return false;
            }

            if (Ietf != null)
            {
                if (other != null && !Ietf.Equals(other.Ietf))
                {
                    return false;
                }
            }
            else if (other?.Ietf != null)
            {
                return false;
            }

            if (Version != null)
            {
                if (other != null && !Version.Equals(other.Version))
                {
                    return false;
                }
            }
            else if (other != null && other.Version != null)
            {
                return false;
            }

            if (Filepath != null)
            {
                if (other != null && !Filepath.Equals(other.Filepath))
                {
                    return false;
                }
            }
            else if (other?.Filepath != null)
            {
                return false;
            }

            return true;
        }
    }
}