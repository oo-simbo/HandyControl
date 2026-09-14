using System;

namespace HandyControl.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class DecimalRoundAttribute : Attribute
    {
        public int Decimals { get; }
        public DecimalRoundAttribute(int decimals)
        {
            Decimals = decimals;
        }
    }
}
