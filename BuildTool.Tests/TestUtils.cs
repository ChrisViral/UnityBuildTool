using System;

namespace BuildTool.Tests
{
    /// <summary>
    /// Testing utility methods
    /// </summary>
    public static class TestUtils
    {
        #region Constants
        /// <summary>
        /// First non special character
        /// </summary>
        private const int minChar = ' ';
        /// <summary>
        /// Last non special character
        /// </summary>
        private const int maxChar = '~';
        /// <summary>
        /// Random Number Generator
        /// </summary>
        private static readonly Random rng = new Random();
        #endregion

        #region Methods
        /// <summary>
        /// Generates a random string of the given length out of non-special ASCII characters
        /// </summary>
        /// <param name="length">Length of the string to generate, defaults to 10</param>
        /// <returns>The randomly generated string</returns>
        public static string GetRandomString(int length = 10)
        {
            //Create a char buffer
            char[] buffer = new char[length];
            //Get random characters
            for (int i = 0; i < length; i++)
            {
                buffer[i] = (char)rng.Next(minChar, maxChar);
            }
            //Return created string
            return new string(buffer);
        }

        /// <summary>
        /// Generates a random string of the given length out of the given charset
        /// </summary>
        /// <param name="charSet">Characters that can be used to generate the string</param>
        /// <param name="length">Length of the string to generate, defaults to 10</param>
        /// <returns>The randomly generated string</returns>
        public static string GetRandomString(string charSet, int length = 10)
        {
            //Create a char buffer
            char[] buffer = new char[length];
            //Get random characters
            for (int i = 0; i < length; i++)
            {
                buffer[i] = charSet[rng.Next(charSet.Length)];
            }
            //Return created string
            return new string(buffer);
        }

        /// <summary>
        /// Generates a random DateTime between the min and max values of DateTime
        /// </summary>
        /// <returns>The randomly generated DateTime</returns>
        public static DateTime GetRandomDate() => GetRandomDate(DateTime.MinValue, DateTime.MaxValue);

        /// <summary>
        /// Generates a random DateTime between the two given DateTimes
        /// </summary>
        /// <param name="min">Minimum DateTime</param>
        /// <param name="max">Maximum DateTime</param>
        /// <returns>The randomly generated DateTime</returns>
        public static DateTime GetRandomDate(DateTime min, DateTime max) => min + TimeSpan.FromSeconds((long)(rng.NextDouble() * (max - min).TotalSeconds));
        #endregion
    }
}
