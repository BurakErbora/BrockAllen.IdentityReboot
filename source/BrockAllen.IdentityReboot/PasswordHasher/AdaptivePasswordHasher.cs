﻿using BrockAllen.IdentityReboot.Internal;
using Microsoft.AspNet.Identity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BrockAllen.IdentityReboot
{
    public class AdaptivePasswordHasher : IPasswordHasher
    {
        static readonly TimeSpan DefaultTargetPasswordDuration = TimeSpan.FromMilliseconds(500);
        public const char PasswordHashingIterationCountSeparator = '.';

        public int IterationCount { get; set; }

        public AdaptivePasswordHasher()
        {
        }

        public AdaptivePasswordHasher(int iterations)
        {
            if (iterations <= 0) throw new ArgumentException("Invalid iterations");

            this.IterationCount = iterations;
        }
        
        public AdaptivePasswordHasher(TimeSpan targetDuration)
        {
            if (targetDuration <= TimeSpan.Zero) throw new ArgumentException("Invalid targetDuration");

            this.IterationCount = Estimate(targetDuration);
        }

        private int Estimate(TimeSpan targetDuration)
        {
            int[] guesses = new int[100];
            for (var i = 0; i < guesses.Length; i++)
            {
                guesses[i] = Test(100, targetDuration);
            }
            return (int)guesses.Average();
        }
        private static int Test(int count, TimeSpan targetDuration)
        {
            var sw = new Stopwatch();
            sw.Start();
            Crypto.HashPassword("Test123$%^", count);
            sw.Stop();
            var guess = (targetDuration.TotalMilliseconds / sw.Elapsed.TotalMilliseconds) * count;
            return (int)guess;
        }

        public string HashPassword(string password)
        {
            var count = IterationCount;
            if (count <= 0)
            {
                count = GetIterationsFromYear(GetCurrentYear());
            }
            var result = Crypto.HashPassword(password, count);
            return EncodeIterations(count) + PasswordHashingIterationCountSeparator + result;
        }

        public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            if (!String.IsNullOrWhiteSpace(hashedPassword))
            {
                if (hashedPassword.Contains(PasswordHashingIterationCountSeparator))
                {
                    var parts = hashedPassword.Split(PasswordHashingIterationCountSeparator);
                    if (parts.Length != 2) return PasswordVerificationResult.Failed;

                    int count = DecodeIterations(parts[0]);
                    if (count <= 0) return PasswordVerificationResult.Failed;

                    hashedPassword = parts[1];

                    if (Crypto.VerifyHashedPassword(hashedPassword, providedPassword, count))
                    {
                        return PasswordVerificationResult.Success;
                    }
                }
                else if (Crypto.VerifyHashedPassword(hashedPassword, providedPassword))
                {
                    return PasswordVerificationResult.Success;
                }
            }

            return PasswordVerificationResult.Failed;
        }

        public string EncodeIterations(int count)
        {
            return count.ToString("X");
        }

        public int DecodeIterations(string prefix)
        {
            int val;
            if (Int32.TryParse(prefix, System.Globalization.NumberStyles.HexNumber, null, out val))
            {
                return val;
            }
            return -1;
        }
        
        // from OWASP : https://www.owasp.org/index.php/Password_Storage_Cheat_Sheet
        const int StartYear = 2000;
        const int StartCount = 1000;
        public int GetIterationsFromYear(int year)
        {
            if (year > StartYear)
            {
                var diff = (year - StartYear) / 2;
                var mul = (int)Math.Pow(2, diff);
                int count = StartCount * mul;
                // if we go negative, then we wrapped (expected in year ~2044). 
                // Int32.Max is best we can do at this point
                if (count < 0) count = Int32.MaxValue;
                return count;
            }
            return StartCount;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        internal static bool SlowEqualsInternal(string a, string b)
        {
            if (Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            bool same = true;
            for (var i = 0; i < a.Length; i++)
            {
                same &= (a[i] == b[i]);
            }
            return same;
        }

        public virtual int GetCurrentYear()
        {
            return DateTime.Now.Year;
        }
    }
}
