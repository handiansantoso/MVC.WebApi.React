using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;

namespace AdCenter.Extensions
{
    public static class IdentityExtensions
    {
        public static bool IsActive(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("Active");
            // Test for null to avoid issues during local testing
            return (claim != null) ? bool.Parse(claim.Value) : false;
        }
        public static string GetAddress(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("Address");
            // Test for null to avoid issues during local testing
            return (claim != null) ? claim.Value : string.Empty;
        }
        public static string GetPaymentTerms(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("PaymentTerms");
            // Test for null to avoid issues during local testing
            return (claim != null) ? claim.Value : string.Empty;
        }
        public static decimal GetBalance(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("Balance");
            // Test for null to avoid issues during local testing
            return (claim != null) ? decimal.Parse(claim.Value) : 0;
        }
        public static decimal GetCreditLimit(this IIdentity identity)
        {
            var claim = ((ClaimsIdentity)identity).FindFirst("CreditLimit");
            return (claim != null) ? decimal.Parse(claim.Value) : 0;
        }
    }
}