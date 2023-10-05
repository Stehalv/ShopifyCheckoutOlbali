using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ShopifyApp
{
    public static class Extensions
    {

        /// <summary>
        ///  Determines if the provided object can be parsed as the provided type. Essentially a condensed TryParse.
        /// </summary>
        /// <typeparam name="T">The type of object to attempt to parse the provided object as.</typeparam>
        /// <param name="objectToBeParsed">The object to attempt to parse.</param>
        /// <returns>Whether or not the object can be parsed as the provided type.</returns>
        public static bool CanBeParsedAs<T>(this object objectToBeParsed)
        {
            try
            {
                var castedObject = Convert.ChangeType(objectToBeParsed, typeof(T));
                return castedObject != null;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// A shortcut call for string.Format(), it formats strings. 
        /// </summary>
        /// <param name="format">The format of the string</param>
        /// <param name="args">The arguments to merge into the provided format</param>
        /// <returns>The formatted, merged string.</returns>
        public static string FormatWith(this string format, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            return string.Format(format, args);
        }

        /// <summary>
        /// A shortcut call for string.Format(), it formats strings. 
        /// </summary>
        /// <param name="provider">The format provider to use</param>
        /// <param name="format">The format of the string</param>
        /// <param name="args">The arguments to merge into the provided format</param>
        /// <returns>The formatted, merged string.</returns>
        public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            return string.Format(provider, format, args);
        }

        /// <summary>
        /// Return either the provided value, or the provided default value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static string Or(this string value, string defaultValue)
        {
            if (!string.IsNullOrEmpty(value)) return value;
            else return defaultValue;
        }

        /// <summary>
        /// Shortcut to determine if a string is empty
        /// </summary>
        /// <returns></returns>
        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Shortcut to determine if a string is null or empty
        /// </summary>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Shortcut to determine if a string is not empty
        /// </summary>
        /// <returns></returns>
        public static bool IsNotEmpty(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Shortcut to determine if a string is not null or empty
        /// </summary>
        /// <returns></returns>
        public static bool IsNotNullOrEmpty(this string value)
        {
            return !string.IsNullOrEmpty(value);
        }
        public static List<KeyValuePair<string, StringValues>> ToKvps(this System.Collections.Specialized.NameValueCollection qs)
        {
            Dictionary<string, string> parameters = qs.Keys.Cast<string>().ToDictionary(key => key, value => qs[value]);
            var kvps = new List<KeyValuePair<string, StringValues>>();

            parameters.ToList().ForEach(x =>
            {
                kvps.Add(new KeyValuePair<string, StringValues>(x.Key, new StringValues(x.Value)));
            });

            return kvps;
        }
        public static string Encrypt(string textToEncrypt)
        {
            try
            {
                string ToReturn = "";
                string publickey = "12345678";
                string secretkey = "87654321";
                byte[] secretkeyByte = { };
                secretkeyByte = System.Text.Encoding.UTF8.GetBytes(secretkey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = System.Text.Encoding.UTF8.GetBytes(textToEncrypt);
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateEncryptor(publickeybyte, secretkeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    ToReturn = Convert.ToBase64String(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }
        public static string Decrypt(string textToDecrypt)
        {
            string ToReturn = "";
            try
            {
                string publickey = "12345678";
                string secretkey = "87654321";
                byte[] privatekeyByte = { };
                privatekeyByte = System.Text.Encoding.UTF8.GetBytes(secretkey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = new byte[textToDecrypt.Replace(" ", "+").Length];
                inputbyteArray = Convert.FromBase64String(textToDecrypt.Replace(" ", "+"));
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateDecryptor(publickeybyte, privatekeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    ToReturn = encoding.GetString(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ae)
            {
                ToReturn = "Failed Decryption";
                return ToReturn;
            }
        }
    }
}