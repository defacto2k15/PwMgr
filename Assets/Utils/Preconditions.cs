using System;
using UnityEngine;

namespace Assets.Utils
{
    static class Preconditions
    {
        private static bool softFail = false; //todo!
        public static void Assert(bool valueToCheck, string failureMessage)
        {
            if (!valueToCheck)
            {
                Fail(failureMessage);
            }
        }

        public static void Assert(bool valueToCheck, Func<string> failureMessageFunc)
        {
            if (!valueToCheck)
            {
                Fail(failureMessageFunc());
            }
        }

        public static void Fail(string failureMessage)
        {
            if (softFail)
            {
                Debug.Log("SOFT FAIL:" + failureMessage);
            }
            else
            {
                var exception = new ArgumentException(failureMessage);
                Debug.LogError("Precondition failure: " + exception);
                throw exception;
            }
        }
    }
}